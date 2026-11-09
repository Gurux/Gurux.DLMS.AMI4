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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Resolves notification sources from a user's direct and inherited permissions.</summary>
internal static class GXNotificationPermissions
{
    public static async Task<GXPermission[]> ReadAsync(GXDbConnection connection, string userId, CancellationToken cancellationToken = default)
    {
        var names = await UserPermissions.ReadEffectiveAsync(connection, userId);
        var identity = new ClaimsIdentity("NotificationSources");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        foreach (var role in await UserPermissions.ReadRolesAsync(connection, userId))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        foreach (var name in names)
        {
            identity.AddClaim(new Claim("Permission", name));
        }

        identity.AddClaims(await UserPermissions.ReadRoleClaimsAsync(connection, userId, ClaimTypes.Role));
        foreach (var claim in await connection.SelectAsync<GXUserClaim>(GXSelectArgs.SelectAll<GXUserClaim>(c => c.UserId == userId), cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(claim.ClaimType) && claim.ClaimValue != null && claim.ClaimType != "Permission" && claim.ClaimType != ClaimTypes.Role)
            {
                identity.AddClaim(new Claim(claim.ClaimType, claim.ClaimValue));
            }
        }
        var user = new ClaimsPrincipal(identity);
        var policiesQuery = GXSelectArgs.SelectAll<GXPolicy>(p => p.Removed == null);
        policiesQuery.Columns.Add<GXPolicyRequirement>();
        policiesQuery.Joins.AddLeftJoin<GXPolicy, GXPolicyRequirement>(p => p.Id, r => r.Policy!);
        var allowed = new Dictionary<string, GXPermission>(StringComparer.Ordinal);
        var handler = new GXAuthorizationRequirementHandler();
        foreach (var policy in await connection.SelectAsync<GXPolicy>(policiesQuery, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                continue;
            }
            var requirements = policy.Requirements?.ToList() ?? [];
            if (policy.RequireAuthenticatedUser)
            {
                requirements.Add(new GXPolicyRequirement { Type = PolicyRequirementType.AuthenticatedUser });
            }
            var requirement = new GXAuthorizationRequirement(policy.RequirementOperator, requirements);
            var context = new AuthorizationHandlerContext([requirement], user, null);
            await handler.HandleAsync(context);
            if (context.HasSucceeded)
            {
                allowed[policy.Name] = new GXPermission(policy.Name)
                {
                    Description = policy.Description,
                    SystemDefined = policy.SystemDefined
                };
            }
        }
        names.AddRange(allowed.Keys);
        if (names.Count == 0)
        {
            return[];
        }
        var query = GXSelectArgs.SelectAll<GXPermission>(p => names.Contains(p.Name!));
        query.OrderBy.Add<GXPermission>(p => p.Name!);
        foreach (var permission in await connection.SelectAsync<GXPermission>(query, cancellationToken))
        {
            allowed[permission.Name!] = permission;
        }

        var sources = await connection.SelectAsync<GXNotificationSource>(
            GXSelectArgs.SelectAll<GXNotificationSource>(), cancellationToken);
        var registered = sources.Select(s => s.Source).ToHashSet(StringComparer.Ordinal);
        return [.. allowed.Values.Where(p => registered.Contains(p.Name!) || registered.Contains(p.Name!.Split('.')[0]))
            .OrderBy(p => p.Name, StringComparer.Ordinal)];
    }
}
