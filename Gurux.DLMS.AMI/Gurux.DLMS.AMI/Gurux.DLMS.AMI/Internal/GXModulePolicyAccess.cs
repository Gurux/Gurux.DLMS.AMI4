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

using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

internal static class GXModulePolicyAccess
{
    public static async Task<bool> AllowsAsync(IServiceProvider services, System.Security.Claims.ClaimsPrincipal user,
        string moduleId, CancellationToken token = default)
    {
        await using var connection = await services.GetRequiredService<IDbConnectionFactory>().GetHistoryConnectionAsync(token);
        var names = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), moduleId, token);
        return await GXResourcePolicyAccess.AllowsAsync(connection, services.GetRequiredService<IAuthorizationService>(), user, names, token);
    }

    public static async Task<bool> AuthorizeRequestAsync(HttpContext context, string moduleId)
    {
        if (await AllowsAsync(context.RequestServices, context.User, moduleId, context.RequestAborted))
        {
            return true;
        }
        context.Response.StatusCode = context.User.Identity?.IsAuthenticated == true ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
        return false;
    }
}
