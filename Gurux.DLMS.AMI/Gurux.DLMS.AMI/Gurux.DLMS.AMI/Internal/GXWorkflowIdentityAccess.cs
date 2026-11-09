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

using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Client.Shared;
using System.Security.Claims;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Validates workflow impersonation settings and rebuilds current execution principals.</summary>
internal static class GXWorkflowIdentityAccess
{
    internal static string? ActorId(object? source) => source switch
    {
        ClaimsPrincipal principal => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"),
        ApplicationUser user => user.Id,
        GXUser user => user.Id,
        _ => null
    };

    internal static void EnsureActive(GXUser? user)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.Id) || user.Removed != null || user.IsApproved != true
            || user.LockoutEnabled == true && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
            throw new UnauthorizedAccessException("The workflow execution account is unavailable, unapproved or locked.");
        }
    }

    internal static async Task<GXUser> ReadActiveAsync(GXDbConnection connection, string? id, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            {
            throw new UnauthorizedAccessException("A workflow requires an explicit execution account.");
        }
        var user = await connection.SingleOrDefaultAsync<GXUser>(
            GXSelectArgs.Select<GXUser>(u => new { u.Id, u.UserName, u.Removed, u.IsApproved, u.LockoutEnabled, u.LockoutEnd },
                u => u.Id == id), token);
        EnsureActive(user);
        return user!;
    }

    internal static async Task ValidateAsync(GXDbConnection connection, ClaimsPrincipal caller,
        GXWorkflow workflow, CancellationToken token = default)
    {
        if (!Enum.IsDefined(workflow.ExecutionIdentity))
            {
            throw new ArgumentException("Unknown workflow execution identity.");
        }
        var callerId = ActorId(caller);
        bool admin = caller.IsInRole(GXRoles.Admin);
        // A user must not edit a privileged service workflow and thereby replace its actions.
        if (workflow.Id != Guid.Empty && !admin)
        {
            var previous = await connection.SingleOrDefaultAsync<GXWorkflow>(
                GXSelectArgs.Select<GXWorkflow>(w => new { w.Id, w.ExecutionIdentity, w.ExecutionUserId }, w => w.Id == workflow.Id), token);
            if (previous?.ExecutionIdentity == GXWorkflowExecutionIdentity.EventActor)
                {
                throw new UnauthorizedAccessException("Only an administrator may edit event-actor automation.");
            }
            if (previous?.ExecutionIdentity == GXWorkflowExecutionIdentity.ServiceAccount
                && !string.IsNullOrEmpty(previous.ExecutionUserId) && previous.ExecutionUserId != callerId)
                {
                throw new UnauthorizedAccessException("Only an administrator or the execution account may edit this service workflow.");
            }
        }
        if (workflow.Phase == TriggerPhase.Before && workflow.ExecutionIdentity != GXWorkflowExecutionIdentity.EventActor)
            {
            throw new ArgumentException("Before workflows must run as the event actor.");
        }
        if (workflow.ExecutionIdentity == GXWorkflowExecutionIdentity.EventActor)
        {
            if (!admin) {
                throw new UnauthorizedAccessException("Only an administrator may configure event-actor automation.");
            }
            if (!string.IsNullOrWhiteSpace(workflow.ExecutionUserId))
                {
                throw new ArgumentException("An event-actor workflow cannot specify a service account.");
            }
            return;
        }
        if (string.IsNullOrWhiteSpace(workflow.ExecutionUserId) && workflow.Enabled == false)
            {
            return;
        } // An explicitly disabled draft may be incomplete.
        var executionUser = await ReadActiveAsync(connection, workflow.ExecutionUserId, token);
        if (!admin && executionUser.Id != callerId)
            {
            throw new UnauthorizedAccessException("Only an administrator may assign another user as a workflow service account.");
        }
    }

    internal static async Task ValidateScriptMutationAsync(GXDbConnection connection, ClaimsPrincipal caller,
        Guid scriptId, CancellationToken token = default)
    {
        if (scriptId == Guid.Empty || caller.IsInRole(GXRoles.Admin)) {
            return;
        }
        var script = new GXScript { Id = scriptId };
        var actions = ProtectedWorkflows();
        actions.Joins.AddInnerJoin<GXWorkflow, GXWorkflowScriptMethod>(w => w.Id, m => m.WorkflowId);
        actions.Joins.AddInnerJoin<GXWorkflowScriptMethod, GXScriptMethod>(m => m.MethodId, s => s.Id);
        actions.Where.And<GXScriptMethod>(m => m.Script == script);
        var conditions = ProtectedWorkflows();
        conditions.Joins.AddInnerJoin<GXWorkflow, GXScriptMethod>(w => w.TriggerMethod!, m => m.Id);
        conditions.Where.And<GXScriptMethod>(m => m.Script == script);
        var references = (await connection.SelectAsync<GXWorkflow>(actions, token))
            .Concat(await connection.SelectAsync<GXWorkflow>(conditions, token));
        EnsureCanMutateReferences(caller, references);
    }

    internal static async Task ValidateTriggerMutationAsync(GXDbConnection connection, ClaimsPrincipal caller,
        Guid triggerId, CancellationToken token = default)
    {
        if (triggerId == Guid.Empty || caller.IsInRole(GXRoles.Admin)) {
            return;
        }
        var trigger = new GXTrigger { Id = triggerId };
        var workflows = ProtectedWorkflows();
        workflows.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(w => w.TriggerActivity!, a => a.Id);
        workflows.Where.And<GXTriggerActivity>(a => a.Trigger == trigger);
        EnsureCanMutateReferences(caller, await connection.SelectAsync<GXWorkflow>(workflows, token));
    }

    private static GXSelectArgs ProtectedWorkflows() => GXSelectArgs.Select<GXWorkflow>(
        w => new { w.Id, w.ExecutionIdentity, w.ExecutionUserId },
        w => w.Removed == null);

    private static void EnsureCanMutateReferences(ClaimsPrincipal caller, IEnumerable<GXWorkflow> references)
    {
        var callerId = ActorId(caller);
        if (references.Any(w => w.ExecutionIdentity == GXWorkflowExecutionIdentity.EventActor
            || !string.IsNullOrWhiteSpace(w.ExecutionUserId) && w.ExecutionUserId != callerId))
            {
            throw new UnauthorizedAccessException("This resource is used by protected automation. Only an administrator or its explicit service execution account may change it.");
        }
    }
    internal static string ResolveUserId(GXWorkflow workflow, string? actorId, bool manual)
    {
        if (!Enum.IsDefined(workflow.ExecutionIdentity))
            {
            throw new UnauthorizedAccessException("Unknown workflow execution identity.");
        }
        string? id = manual || workflow.Phase == TriggerPhase.Before
            || workflow.ExecutionIdentity == GXWorkflowExecutionIdentity.EventActor ? actorId : workflow.ExecutionUserId;
        return !string.IsNullOrWhiteSpace(id) ? id
            : throw new UnauthorizedAccessException("The selected workflow execution identity is unavailable.");
    }

    internal static async Task<ClaimsPrincipal> ResolveAsync(GXDbConnection connection, GXWorkflow workflow,
        string? actorId, bool manual, CancellationToken token = default)
    {
        var user = await ReadActiveAsync(connection, ResolveUserId(workflow, actorId, manual), token);
        user.Roles = await UserPermissions.ReadRolesAsync(connection, user.Id!);
        var principal = ServerHelpers.CreateClaimsPrincipalFromUser(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName ?? user.Id!));
        identity.AddClaims((await UserPermissions.ReadRoleClaimsAsync(connection, user.Id!, ClaimTypes.Role))
            .Where(c => c.Type != ClaimTypes.NameIdentifier && c.Type != "sub" && c.Type != ClaimTypes.Name));
        var directClaims = await connection.SelectAsync<GXUserClaim>(
            GXSelectArgs.SelectAll<GXUserClaim>(c => c.UserId == user.Id), token);
        foreach (var claim in directClaims.Where(c => !string.IsNullOrEmpty(c.ClaimType) && c.ClaimValue != null
            && c.ClaimType != "Permission" && c.ClaimType != ClaimTypes.Role && c.ClaimType != ClaimTypes.NameIdentifier
            && c.ClaimType != "sub" && c.ClaimType != ClaimTypes.Name))
            {
            identity.AddClaim(new Claim(claim.ClaimType!, claim.ClaimValue!));
        }
        foreach (var permission in await UserPermissions.ReadEffectiveAsync(connection, user.Id!))
            {
            identity.AddClaim(new Claim("Permission", permission));
        }
        return principal;
    }
}