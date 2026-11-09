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
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Authorizes registered events against their authoritative action permission.</summary>
internal static class GXTriggerActivityAccess
{
    internal static async Task<bool> AllowsAsync(IAuthorizationService authorization, ClaimsPrincipal user,
        string? triggerClass, GXTriggerActivity activity, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!GXTriggerEventCatalog.TryGetPermission(triggerClass, activity.Name, out var permission)) {
            return false;
        }
        return (await authorization.AuthorizeAsync(user, null, permission)).Succeeded;
    }

    internal static async Task<List<GXTriggerActivity>> FilterAsync(IAuthorizationService authorization,
        ClaimsPrincipal user, string? triggerClass, IEnumerable<GXTriggerActivity>? activities, CancellationToken token = default)
    {
        var allowed = new List<GXTriggerActivity>();
        foreach (var activity in activities ?? [])
        {
            if (await AllowsAsync(authorization, user, triggerClass, activity, token))
            {
                GXTriggerEventCatalog.TryGetPermission(triggerClass, activity.Name, out var permission);
                activity.Permission = permission;
                allowed.Add(activity);
            }
        }
        return allowed;
    }

    internal static async Task ApplyAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXSelectArgs query, CancellationToken token)
    {
        var activityQuery = GXSelectArgs.SelectAll<GXTriggerActivity>();
        activityQuery.Columns.Add<GXTrigger>(t => new { t.Id, t.ClassName });
        activityQuery.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
        var allowed = new HashSet<Guid>();
        foreach (var activity in await connection.SelectAsync<GXTriggerActivity>(activityQuery, token))
            {
            if (activity.Trigger != null && await AllowsAsync(authorization, user, activity.Trigger.ClassName, activity, token))
                {
                allowed.Add(activity.Trigger.Id);
            }
        }
        var ids = allowed.ToArray();
        query.Where.And<GXTrigger>(t => ids.Contains(t.Id));
    }

    internal static async Task EnsureAsync(IAuthorizationService authorization, ClaimsPrincipal user,
        GXTrigger trigger, GXTriggerActivity activity, CancellationToken token = default)
    {
        if (!await AllowsAsync(authorization, user, trigger.ClassName, activity, token))
            {
            throw new UnauthorizedAccessException("The trigger event is unavailable or its action permission is denied.");
        }
    }
}