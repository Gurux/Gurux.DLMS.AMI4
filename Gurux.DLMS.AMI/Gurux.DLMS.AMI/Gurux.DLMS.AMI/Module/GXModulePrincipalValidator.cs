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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Checks current host account status and limits module grants to current host grants.</summary>
internal interface IGXModulePrincipalValidator
{
    Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

internal sealed class GXModulePrincipalValidator(IDbConnectionFactory database) : IGXModulePrincipalValidator
{
    public async Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!principal.Identities.Any(i => i.IsAuthenticated)) {
            return false;
        }
        var ids = principal.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")
            .Select(c => c.Value).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0])) {
            return false;
        }
        var userId = ids[0];
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        if (await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!,
            u => u.Id == userId && u.Removed == null && u.IsApproved == true), cancellationToken) == null)
            {
            return false;
        }
        GXTokenPermissionValidator.Restrict(principal, await UserPermissions.ReadEffectiveAsync(connection, userId),
            await UserPermissions.ReadRolesAsync(connection, userId));
        if (!principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
            {
            principal.Identities.First(i => i.IsAuthenticated).AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }
}
