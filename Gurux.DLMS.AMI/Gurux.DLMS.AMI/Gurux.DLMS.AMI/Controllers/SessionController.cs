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
using Gurux.DLMS.AMI.Client.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Reports the current server session to the interactive client.</summary>
[ApiController]
[Route("api/Session")]
public sealed class SessionController : ControllerBase
{
    /// <summary>Returns the authenticated session, or an anonymous session.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public GXSessionState Get()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new();
        }
        return new GXSessionState
        {
            IsAuthenticated = true,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Name = User.Identity.Name,
            Roles = [.. User.Identities.SelectMany(identity => identity.FindAll(identity.RoleClaimType))
                .Select(claim => claim.Value).Distinct().Order(StringComparer.Ordinal)]
        };
    }
}
