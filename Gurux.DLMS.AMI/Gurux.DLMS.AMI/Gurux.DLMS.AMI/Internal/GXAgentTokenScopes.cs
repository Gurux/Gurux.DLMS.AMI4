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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Resolves the least-privilege scopes available for an agent PAT.</summary>
internal static class GXAgentTokenScopes
{
    /// <summary>Reads configured agent scopes limited to the authenticated caller.</summary>
    internal static async Task<string[]> ReadAsync(IDbConnectionFactory database, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        var query = GXSelectArgs.Select<GXPermission>(permission => permission.Name!);
        query.Joins.AddInnerJoin<GXPermission, GXRolePermission>(permission => permission.Id, membership => membership.PermissionId!);
        query.Joins.AddInnerJoin<GXRolePermission, GXRole>(membership => membership.RoleId!, role => role.Id);
        query.Where.And<GXRole>(role => role.NormalizedName == "AGENT" && role.Removed == null);
        var scopes = await connection.SelectAsync<string>(query, cancellationToken);
        var userId = ServerHelpers.GetUserId(user);
        var effective = await UserPermissions.ReadEffectiveAsync(connection, userId);
        var roles = await UserPermissions.ReadRolesAsync(connection, userId);
        if (!user.IsInRole(GXRoles.Admin) || !roles.Contains(GXRoles.Admin, StringComparer.Ordinal))
        {
            effective = [.. effective.Intersect(user.FindAll("Permission").Select(claim => claim.Value), StringComparer.Ordinal)];
        }
        return [.. scopes.Intersect(effective, StringComparer.Ordinal).OrderBy(scope => scope, StringComparer.Ordinal)];
    }
}
