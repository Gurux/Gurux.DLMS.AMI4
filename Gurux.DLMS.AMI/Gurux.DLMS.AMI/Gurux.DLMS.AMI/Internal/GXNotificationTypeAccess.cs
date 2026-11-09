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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

internal static class GXNotificationTypeAccess
{
    public static async Task<bool> AllowsAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, Guid id, CancellationToken token = default)
    {
        var type = await connection.SingleOrDefaultAsync<GXNotificationType>(
            GXSelectArgs.Select<GXNotificationType>(t => new { t.Id, t.Enabled, t.Removed }, t => t.Id == id), token);
        if (type?.Enabled != true || type.Removed != null)
        {
            return false;
        }
        var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXNotificationType), id, token);
        return await GXResourcePolicyAccess.AllowsAsync(connection, authorization, user, policies, token);
    }

    public static async Task<Guid[]> AllowedIdsAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, CancellationToken token = default)
    {
        var types = await connection.SelectAsync<GXNotificationType>(
            GXSelectArgs.Select<GXNotificationType>(t => t.Id, t => t.Enabled == true && t.Removed == null), token);
        var allowed = new List<Guid>();
        foreach (var type in types)
        {
            if (await AllowsAsync(connection, authorization, user, type.Id, token))
            {
                allowed.Add(type.Id);
            }
        }
        return [.. allowed];
    }

    // All callers join GXNotificationType, including offline email and WebPush delivery.
    public static async Task RestrictAsync(GXDbConnection connection, GXSelectArgs query,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken token = default)
    {
        var ids = await AllowedIdsAsync(connection, authorization, user, token);
        query.Where.And<GXNotificationType>(t => ids.Contains(t.Id));
    }
}
