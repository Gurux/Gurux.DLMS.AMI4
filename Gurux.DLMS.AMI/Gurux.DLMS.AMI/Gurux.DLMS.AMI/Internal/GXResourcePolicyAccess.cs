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
using Gurux.DLMS.AMI.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

internal static class GXResourcePolicyAccess
{
    public static async Task<bool> AllowsAsync(GXDbConnection connection, IAuthorizationService authorization, ClaimsPrincipal? user, IEnumerable<string>? policies,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] names;
        try
        {
            names = GXPolicyNames.Normalize(policies);
        }
        catch (ArgumentException)
        {
            return false;
        }
        if (names.Length == 0)
        {
            return true;
        }

        foreach (var policyName in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedName = policyName.ToUpperInvariant();
            var active = await connection.SingleOrDefaultAsync<GXPolicy>(GXSelectArgs.Select<GXPolicy>(p => new { p.Id, p.Name },
                p => p.NormalizedName == normalizedName && p.Removed == null), cancellationToken);
            if (active != null && (await authorization.AuthorizeAsync(
                user ?? new ClaimsPrincipal(new ClaimsIdentity()), null, active.Name)).Succeeded)
            {
                return true;
            }
        }
        return false;
    }

    public static async Task ValidateAsync(GXDbConnection connection, IEnumerable<string>? policies, CancellationToken cancellationToken = default)
    {
        foreach (var policyName in GXPolicyNames.Normalize(policies))
        {
            var normalizedName = policyName.ToUpperInvariant();
            if (await connection.SingleOrDefaultAsync<GXPolicy>(GXSelectArgs.Select<GXPolicy>(p => p.Id,
                p => p.NormalizedName == normalizedName && p.Removed == null), cancellationToken) == null)
            {
                throw new ArgumentException($"Policy '{policyName}' is unknown or removed.");
            }
        }
    }
}
