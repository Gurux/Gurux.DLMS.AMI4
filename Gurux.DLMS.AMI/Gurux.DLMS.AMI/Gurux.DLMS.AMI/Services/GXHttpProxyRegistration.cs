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

using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Gurux.DLMS.AMI.Services;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Registers server-only proxy limits and the authenticated endpoint quota.</summary>
public static class GXHttpProxyRegistration
{
    /// <summary>Endpoint policy name for authenticated proxy traffic.</summary>
    public const string RateLimitPolicy = "AmiHttpProxy";

    /// <summary>Binds HttpProxyLimits and registers a per-identity, per-instance request limit.</summary>
    public static IServiceCollection AddAmiHttpProxy(this IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddOptions<GXHttpProxyOptions>().Bind(configuration).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient("HttpProxy", client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(GXHttpProxyTransport.CreateHandler)
            // Default HttpClient logging includes target paths and bypasses our sanitized diagnostics.
            .RemoveAllLoggers();
        services.AddSingleton<GXHttpProxyService>();
        services.AddScoped<GXHttpProxyConfiguration>();
        services.AddScoped<IGXAmiHttpProxy, GXAmiHttpProxy>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retry.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }
                return ValueTask.CompletedTask;
            };
            options.AddPolicy(RateLimitPolicy, context =>
            {
                // Authorization owns anonymous requests and must continue to return 401.
                if (context.User.Identity?.IsAuthenticated != true)
                    {
                    return RateLimitPartition.GetNoLimiter("anonymous");
                }
                var identity = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub") ??
                    context.User.Identity.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var limits = context.RequestServices.GetRequiredService<IOptions<GXHttpProxyOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(identity, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.RequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });
        return services;
    }
}
