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

using System.Net;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Midlewares;

/// <summary>Returns maintenance responses while preserving administrator and sign-in access.</summary>
internal sealed class GXMaintenanceMidleware(RequestDelegate next, IConfigurationRepository configurationRepository)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfigurationRepository _configurationRepository = configurationRepository;

    public async Task Invoke(HttpContext context, Gurux.DLMS.AMI.Module.IGXAmiTemplateTokenResolver tokens)
    {
        var path = context.Request.Path;
        if (context.User.IsInRole(GXRoles.Admin) || path.Equals(new PathString("/api/Configuration/Maintenance")) ||
            path.StartsWithSegments("/Account"))
        {
            await _next(context);
            return;
        }
        // Read the persisted configuration so updates and restarts cannot leave a stale maintenance state.
        var configurations = await _configurationRepository.ListAsync(context.User,
            new ListConfiguration { Filter = new GXConfiguration { Name = GXConfigurations.Maintenance } },
            null, context.RequestAborted);
        var configuration = configurations.FirstOrDefault();
        var settings = string.IsNullOrEmpty(configuration?.Settings) ? null :
            JsonSerializer.Deserialize<MaintenanceSettings>(configuration.Settings);
        var now = DateTimeOffset.UtcNow;
        if (settings?.IsActive(now) != true)
        {
            await _next(context);
            return;
        }
        context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
        if (settings.EndTime is DateTimeOffset endTime)
        {
            context.Response.Headers.RetryAfter = Math.Ceiling((endTime - now).TotalSeconds)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        bool html = (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
            !(path.StartsWithSegments("/api") || path.StartsWithSegments("/guruxami") || path.StartsWithSegments("/addin"));
        context.Response.ContentType = html ? "text/html; charset=utf-8" : "application/json; charset=utf-8";
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }
        const string title = "Site is on maintenance mode.";
        var detail = string.IsNullOrWhiteSpace(settings.Message) ? title :
            settings.Message.Length > 65536 ? settings.Message :
            await tokens.ResolveAsync(settings.Message, new Gurux.DLMS.AMI.Module.AmiTemplateTokenContext
            { User = context.User, Culture = System.Globalization.CultureInfo.CurrentCulture,
                Page = "maintenance", Values = settings.GetTokenValues() }, context.RequestAborted);
        if (html)
        {
            var loginUrl = WebUtility.HtmlEncode(context.Request.PathBase.Add("/Account/Login").Value);
            var endNotice = settings.EndTime is DateTimeOffset end
                ? "<p>Expected end: " + WebUtility.HtmlEncode(end.UtcDateTime.ToString("u", System.Globalization.CultureInfo.InvariantCulture)) + "</p>"
                : string.Empty;
            await context.Response.WriteAsync($"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>503 Service Unavailable</title>
                </head>
                <body>
                    <main>
                        <h1>{title}</h1>
                        <p style="white-space: pre-wrap">{WebUtility.HtmlEncode(detail)}</p>
                        {endNotice}
                        <a href="{loginUrl}">Administrator login</a>
                    </main>
                </body>
                </html>
                """, context.RequestAborted);
            return;
        }
        await context.Response.WriteAsJsonAsync(new
        {
            type = "maintenance",
            title,
            status = (int)HttpStatusCode.ServiceUnavailable,
            detail,
            startTime = settings.StartTime,
            endTime = settings.EndTime
        }, context.RequestAborted);
    }
}
