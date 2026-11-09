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

using Gurux.DLMS.AMI.Client.Shared;
using System.Security.Claims;
using Gurux.DLMS.AMI.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Applies resource policies in addition to existing automation group access.</summary>
internal static class GXAutomationPolicyAccess
{
    public static async Task EnsureWorkflowReferencesAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXWorkflow workflow, CancellationToken cancellationToken = default)
    {
        if (workflow.TriggerActivity != null)
        {
            var query = GXSelectArgs.SelectAll<GXTriggerActivity>(a => a.Id == workflow.TriggerActivity.Id);
            query.Columns.Add<GXTrigger>();
            query.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
            var activity = await connection.SingleOrDefaultAsync<GXTriggerActivity>(query, cancellationToken);
            if (activity?.Trigger == null || activity.Trigger.Removed != null)
                {
                throw new ArgumentException("The workflow trigger activity does not exist.");
            }
            await authorization.EnsureAnyPolicyAsync(user, GXTriggerPolicies.View);
            await EnsureAsync(connection, authorization, user, typeof(GXTrigger), activity.Trigger.Id, cancellationToken);
            await GXTriggerActivityAccess.EnsureAsync(authorization, user, activity.Trigger, activity, cancellationToken);
        }
        var methods = (workflow.ScriptMethods ?? []).Select(m => m.Id).ToList();
        if (workflow.TriggerMethod != null)
            {
            methods.Add(workflow.TriggerMethod.Id);
        }
        foreach (var id in methods.Distinct())
        {
            var query = GXSelectArgs.SelectAll<GXScriptMethod>(m => m.Id == id);
            query.Columns.Add<GXScript>(s => new { s.Id, s.Removed });
            query.Joins.AddInnerJoin<GXScriptMethod, GXScript>(m => m.Script!, s => s.Id);
            var method = await connection.SingleOrDefaultAsync<GXScriptMethod>(query, cancellationToken);
            if (method?.Script == null || method.Script.Removed != null)
                {
                throw new ArgumentException("The workflow script method does not exist.");
            }
            await authorization.EnsureAnyPolicyAsync(user, GXScriptPolicies.Run);
            await EnsureAsync(connection, authorization, user, typeof(GXScript), method.Script.Id, cancellationToken);
        }
    }

    public static async Task EnsureAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, Type resourceType, Guid id, CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(GXRoles.Admin))
        {
            var userId = ServerHelpers.GetUserId(user);
            var membership = GXRepositoryUsers.Query(resourceType, id, includeRemovedResource: true);
            membership.Where.And<GXUser>(u => u.Id == userId);
            if ((await connection.SelectAsync<GXUser>(membership, cancellationToken)).Count == 0)
                {
                throw new UnauthorizedAccessException();
            }
        }
        if (!await GXResourcePolicyAccess.AllowsAsync(connection, authorization, user,
            await GXResourcePolicyStorage.ReadAsync(connection, resourceType, id, cancellationToken), cancellationToken))
            {
            throw new UnauthorizedAccessException();
        }
    }

    /// <summary>Restricts the query before its count and paging are evaluated.</summary>
    public static async Task ApplyAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, Type resourceType, GXSelectArgs query, CancellationToken cancellationToken)
    {
        var candidates = resourceType == typeof(GXScript) ? GXSelectArgs.Select<GXScript>(s => s.Id) :
            resourceType == typeof(GXTrigger) ? GXSelectArgs.Select<GXTrigger>(s => s.Id) : GXSelectArgs.Select<GXWorkflow>(s => s.Id);
        candidates.Joins.Append(query.Joins);
        candidates.Where.Append(query.Where);
        candidates.Distinct = true;
        var allowed = new List<Guid>();
        if (resourceType == typeof(GXScript))
        {
            foreach (var item in await connection.SelectAsync<GXScript>(candidates, cancellationToken))
                {
                if (await AllowsAsync(item.Id)) {
                    allowed.Add(item.Id);
                }
            }
            query.Where.And<GXScript>(s => allowed.Contains(s.Id));
        }
        else if (resourceType == typeof(GXTrigger))
        {
            foreach (var item in await connection.SelectAsync<GXTrigger>(candidates, cancellationToken))
                {
                if (await AllowsAsync(item.Id)) {
                    allowed.Add(item.Id);
                }
            }
            query.Where.And<GXTrigger>(s => allowed.Contains(s.Id));
        }
        else if (resourceType == typeof(GXWorkflow))
        {
            foreach (var item in await connection.SelectAsync<GXWorkflow>(candidates, cancellationToken))
                {
                if (await AllowsAsync(item.Id)) {
                    allowed.Add(item.Id);
                }
            }
            query.Where.And<GXWorkflow>(s => allowed.Contains(s.Id));
        }
        else {
            throw new ArgumentException("Unsupported automation resource.", nameof(resourceType));
        }

        async Task<bool> AllowsAsync(Guid id)
        {
            try
            {
                await EnsureAsync(connection, authorization, user, resourceType, id, cancellationToken);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
