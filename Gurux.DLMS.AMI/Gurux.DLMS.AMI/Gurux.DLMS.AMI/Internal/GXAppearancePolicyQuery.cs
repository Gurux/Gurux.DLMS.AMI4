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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Restricts resource reads before their count and page are calculated.</summary>
internal static class GXAppearancePolicyQuery
{
    /// <summary>An explicit policy grant is independent of the resource's visibility or ownership.</summary>
    public static async Task<bool> AllowsAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXAppearance appearance, IEnumerable<string>? policies, CancellationToken token = default)
    {
        var hasPolicies = policies?.Any(p => !string.IsNullOrWhiteSpace(p)) == true;
        if (hasPolicies && await GXResourcePolicyAccess.AllowsAsync(connection, authorization, user, policies, token))
        {
            return true;
        }
        if (GXAppearanceAccess.IsOwnerOnly(appearance))
        {
            return GXAppearanceAccess.CanReadPrivate(user, appearance);
        }
        return !hasPolicies && GXAppearanceAccess.CanView(user, appearance);
    }

    public static async Task<bool> RestrictAsync(GXDbConnection connection, GXSelectArgs query,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken token = default)
    {
        var allowed = new List<Guid>();
        var candidateQuery = GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.AccessVisibility, a.Creator }, a => a.Removed == null);
        candidateQuery.Joins.AddLeftJoin<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!);
        candidateQuery.Columns.Add<GXUser>(u => u.Id!);
        var candidates = await connection.SelectAsync<GXAppearance>(candidateQuery, token);
        foreach (var candidate in candidates)
        {
            var names = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXAppearance), candidate.Id, token);
            if (await AllowsAsync(connection, authorization, user, candidate, names, token))
            {
                allowed.Add(candidate.Id);
            }
        }
        if (allowed.Count == 0)
        {
            return false;
        }

        query.Where.And<GXAppearance>(a => allowed.Contains(a.Id));
        return true;
    }
}
