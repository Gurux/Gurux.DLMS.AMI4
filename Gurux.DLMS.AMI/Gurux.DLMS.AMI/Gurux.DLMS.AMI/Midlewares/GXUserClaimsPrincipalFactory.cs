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

using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares;

internal sealed class GXUserClaimsPrincipalFactory(UserManager<ApplicationUser> users,
    IOptions<IdentityOptions> options, IDbConnectionFactory dbFactory, GXUserPrincipalCache principalCache) : UserClaimsPrincipalFactory<ApplicationUser>(users, options)
{
    private readonly IDbConnectionFactory _dbFactory = dbFactory;
    private readonly GXUserPrincipalCache _principalCache = principalCache;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var principal = await _principalCache.GetAsync(user.Id, user.SecurityStamp,
            async () => new ClaimsPrincipal(await LoadClaimsAsync(user)));
        return (ClaimsIdentity)principal.Identity!;
    }

    private async Task<ClaimsIdentity> LoadClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        foreach (var claim in identity.FindAll("Permission").ToArray())
        {
            identity.RemoveClaim(claim);
        }

        await using var connection = await _dbFactory.GetConnectionAsync();
        foreach (var claim in identity.FindAll(Options.ClaimsIdentity.RoleClaimType).ToArray())
        {
            identity.RemoveClaim(claim);
        }

        foreach (var role in await UserPermissions.ReadRolesAsync(connection, user.Id))
        {
            identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, role));
        }

        identity.AddClaims(await UserPermissions.ReadRoleClaimsAsync(connection, user.Id, Options.ClaimsIdentity.RoleClaimType));
        foreach (var name in await UserPermissions.ReadEffectiveAsync(connection, user.Id))
        {
            identity.AddClaim(new Claim("Permission", name));
        }

        return identity;
    }
}
