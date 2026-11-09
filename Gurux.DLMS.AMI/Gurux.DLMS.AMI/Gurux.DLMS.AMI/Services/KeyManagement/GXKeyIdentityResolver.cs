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

using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using System.Security.Claims;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Reloads certificate-mapped AMI identities without synthesizing HTTP contexts or caching rights.</summary>
public sealed class GXKeyIdentityResolver(UserManager<ApplicationUser> users, IDbConnectionFactory factory) : IKeyIdentityResolver
{
    /// <inheritdoc/>
    public async Task<ClaimsPrincipal> ResolveAsync(string userId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("Mapped AMI user does not exist.");
        if (await users.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException("Mapped AMI user is locked out.");
        }
        var identity = new ClaimsIdentity("KMIP certificate");
        identity.AddClaim(new(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new(ClaimTypes.Name, user.UserName ?? user.Id));
        await using var db = await factory.GetConnectionAsync(token);
        if (await db.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == user.Id && u.Removed == null), token) == null)
        {
            throw new UnauthorizedAccessException("Mapped AMI user has been removed.");
        }
        foreach (var role in await UserPermissions.ReadRolesAsync(db, user.Id))
        {
            identity.AddClaim(new(ClaimTypes.Role, role));
        }

        foreach (var permission in await UserPermissions.ReadEffectiveAsync(db, user.Id))
        {
            identity.AddClaim(new("Permission", permission));
        }

        return new(identity);
    }
}
