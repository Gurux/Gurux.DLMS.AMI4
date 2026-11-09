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

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares;

/// <summary>Keeps dynamically loaded permission claims out of the persistent authentication cookie.</summary>
internal class GXCompactCookieEvents : CookieAuthenticationEvents
{
    private static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api") ||
        context.Request.Path.StartsWithSegments("/guruxami");

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!IsApi(context.HttpContext))
        {
            return base.RedirectToLogin(context);
        }
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { status = 401, title = "Unauthorized", detail = "Sign in to continue." });
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!IsApi(context.HttpContext))
        {
            return base.RedirectToAccessDenied(context);
        }
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { status = 403, title = "Forbidden", detail = "You do not have permission to access this resource." });
    }

    public override Task SigningIn(CookieSigningInContext context)
    {
        // Unlike IssuedUtc, this encrypted ticket property survives sliding renewal.
        // RefreshSignInAsync also retains the original AuthenticationProperties.
        context.Properties.Items.TryAdd("Gurux.SessionStartedUtc",
            DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        if (context.Principal != null)
        {
            var compact = new ClaimsPrincipal(context.Principal.Identities.Select(i => i.Clone()));
            foreach (var identity in compact.Identities)
            {
                foreach (var claim in identity.FindAll("Permission").ToArray())
                {
                    identity.RemoveClaim(claim);
                }
            }

            context.Principal = compact;
        }
        return base.SigningIn(context);
    }
}
