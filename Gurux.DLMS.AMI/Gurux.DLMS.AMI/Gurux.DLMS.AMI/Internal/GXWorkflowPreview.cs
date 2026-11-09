// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Read-only matching preview. Deliberately has no dependency on a script runner.</summary>
internal static class GXWorkflowPreview
{
    internal static async Task<PreviewWorkflowResponse> PreviewAsync(GXDbConnection connection,
        IAuthorizationService authorization, ClaimsPrincipal caller, PreviewWorkflow request, CancellationToken token = default)
    {
        var workflow = request.Workflow ?? throw new ArgumentException("A workflow is required.");
        Gurux.DLMS.AMI.Server.Services.GXWorkflowService.ValidateExecutionSettings(workflow);
        await authorization.EnsureAnyPolicyAsync(caller, GXWorkflowPolicies.View);
        await authorization.EnsureAnyPolicyAsync(caller, workflow.Id == Guid.Empty ? GXWorkflowPolicies.Add : GXWorkflowPolicies.Edit);
        if (workflow.Id != Guid.Empty)
        {
            var existing = await connection.SingleOrDefaultAsync<GXWorkflow>(GXSelectArgs.Select<GXWorkflow>(w => new { w.Id, w.Removed }, w => w.Id == workflow.Id), token);
            if (existing == null || existing.Removed != null) {
                throw new ArgumentException("The workflow is unavailable.");
            }
            await GXAutomationPolicyAccess.EnsureAsync(connection, authorization, caller, typeof(GXWorkflow), workflow.Id, token);
        }
        await GXWorkflowIdentityAccess.ValidateAsync(connection, caller, workflow, token);
        await GXResourcePolicyAccess.ValidateAsync(connection, workflow.Policies, token);
        await GXWorkflowEventFilter.ValidateAsync(connection, authorization, caller, workflow, token);
        await GXAutomationPolicyAccess.EnsureWorkflowReferencesAsync(connection, authorization, caller, workflow, token);
        if (workflow.Phase != TriggerPhase.After) {
            throw new ArgumentException("Only the After workflow phase is currently executable.");
        }
        if (workflow.TriggerActivity == null) {
            throw new ArgumentException("A trigger activity is required.");
        }
        var query = GXSelectArgs.SelectAll<GXTriggerActivity>(a => a.Id == workflow.TriggerActivity.Id);
        query.Columns.Add<GXTrigger>();
        query.Columns.Exclude<GXTrigger>(t => t.Activities!);
        query.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
        var activity = await connection.SingleOrDefaultAsync<GXTriggerActivity>(query, token);
        if (activity?.Trigger == null || !activity.SupportedPhases.HasFlag(workflow.Phase))
            {
            throw new ArgumentException("The trigger activity does not support the selected phase.");
        }
        if (!GXTriggerEventCatalog.TryResolveEvent(request.EventCode, out var eventType, out var eventName))
            {
            throw new ArgumentException("Choose a registered event code.");
        }
        GXTriggerEventCatalog.TryGetPermission(eventType.FullName, eventName, out var permission);
        await authorization.EnsureAnyPolicyAsync(caller, permission);
        var context = CreateContext(request, permission);
        if (permission.StartsWith("agent.", StringComparison.Ordinal) || permission.StartsWith("schedule.", StringComparison.Ordinal))
        {
            if (!Guid.TryParse(request.TargetId, out var targetId) || targetId == Guid.Empty)
                {
                throw new ArgumentException("Select an existing sample event target.");
            }
            bool exists = permission.StartsWith("agent.", StringComparison.Ordinal)
                ? (await connection.SelectAsync<Gurux.DLMS.AMI.Shared.DTOs.Agent.GXAgent>(
                    GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Agent.GXAgent>(a => a.Id, a => a.Id == targetId && a.Removed == null), token)).Any()
                : (await connection.SelectAsync<Gurux.DLMS.AMI.Shared.DTOs.Schedule.GXSchedule>(
                    GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Schedule.GXSchedule>(s => s.Id, s => s.Id == targetId && s.Removed == null), token)).Any();
            if (!exists) {
                throw new UnauthorizedAccessException("The sample event target is unavailable.");
            }
        }
        // Deletion previews use a still-existing sample device, never a client-supplied deletion snapshot.
        var accessContext = CreateContext(request, permission == "device.delete" ? "device.read" : permission);
        await GXWorkflowEventFilter.EnsureEventAccessAsync(connection, authorization, caller, accessContext, token);
        await GXWorkflowEventAccess.EnsureAsync(connection, authorization, caller, context, token);
        var execution = await GXWorkflowIdentityAccess.ResolveAsync(connection, workflow, GXWorkflowIdentityAccess.ActorId(caller), false, token);
        await authorization.EnsureAnyPolicyAsync(execution, GXWorkflowPolicies.Run);
        if (!await GXResourcePolicyAccess.AllowsAsync(connection, authorization, execution, workflow.Policies, token))
            {
            throw new UnauthorizedAccessException("The execution account cannot access the draft workflow policies.");
        }
        if (workflow.Id != Guid.Empty)
            {
            await GXAutomationPolicyAccess.EnsureAsync(connection, authorization, execution, typeof(GXWorkflow), workflow.Id, token);
        }
        await GXAutomationPolicyAccess.EnsureWorkflowReferencesAsync(connection, authorization, execution, workflow, token);
        await GXWorkflowEventFilter.ValidateAsync(connection, authorization, execution, workflow, token);
        await GXWorkflowEventFilter.EnsureEventAccessAsync(connection, authorization, execution, accessContext, token);
        await GXWorkflowEventAccess.EnsureAsync(connection, authorization, execution, context, token);
        return new PreviewWorkflowResponse
        {
            TriggerMatches = activity.Trigger.ClassName == eventType.FullName && activity.Name == eventName,
            FiltersMatch = await GXWorkflowEventFilter.MatchesAsync(connection, workflow, context, token),
            Enabled = workflow.Enabled != false,
            ExecutionUserId = GXWorkflowIdentityAccess.ActorId(execution)!,
            ExecutionUserName = execution.Identity?.Name ?? ""
        };
    }

    internal static GXWorkflowEventContext CreateContext(PreviewWorkflow request, string code)
    {
        bool deviceEvent = code.StartsWith("device.", StringComparison.Ordinal) || code.StartsWith("value.", StringComparison.Ordinal);
        if (deviceEvent && (request.DeviceId == null || request.DeviceId == Guid.Empty))
            {
            throw new ArgumentException("Select an existing sample device for a device or value event.");
        }
        return new GXWorkflowEventContext { EventCode = code, DeviceId = deviceEvent ? request.DeviceId : null, TargetId = request.TargetId };
    }
}
