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

using Gurux.DLMS.AMI.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides same-origin access to configured public feeds, data and raster images.</summary>
[ApiController]
[Authorize]
[Route("api/HttpProxy")]
[EnableRateLimiting(GXHttpProxyRegistration.RateLimitPolicy)]
public sealed class HttpProxyController(GXHttpProxyConfiguration configuration, GXHttpProxyService proxy) : ControllerBase
{
    /// <summary>Retrieves an allowlisted HTTP/HTTPS resource without forwarding user credentials.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string url, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        try
        {
            var result = await proxy.GetAsync(url, await configuration.ReadAsync(cancellationToken),
                Request.Headers.Referer.ToString(), cancellationToken);
            Response.Headers["X-Gurux-Source-Url"] = result.SourceUrl;
            // Keep client caching disabled: changing the allowlist or disabling the proxy takes effect immediately.
            return File(result.Body, result.ContentType);
        }
        catch (GXHttpProxyException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: ex.Message);
        }
    }
}
