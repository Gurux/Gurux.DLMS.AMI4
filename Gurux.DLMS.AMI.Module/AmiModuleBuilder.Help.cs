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

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module;

public sealed partial class AmiModuleBuilder
{
    /// <summary>Registers one module's provider and its policy-protected help endpoint.</summary>
    /// <typeparam name="TProvider">Module-owned service that loads help content.</typeparam>
    /// <typeparam name="TContent">Reference type serialized as the help response.</typeparam>
    /// <param name="policy">Policy required to read the module's documentation.</param>
    /// <param name="getHelp">Callback that loads content or returns null for an unknown topic. It receives the module service, request metadata and cancellation token.</param>
    /// <remarks>The provider is a singleton. The host exposes GET /api/extensions/{moduleId}/help with topic, version and culture query parameters.</remarks>
    /// <exception cref="ArgumentException">The access policy is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">The help callback is null.</exception>
    public void AddHelp<TProvider, TContent>(string policy,
        Func<TProvider, AmiModuleHelpRequest, CancellationToken, Task<TContent?>> getHelp)
        where TProvider : class
        where TContent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        ArgumentNullException.ThrowIfNull(getHelp);
        Services.AddSingleton<TProvider>();
        MapGet("help", async (request, token) =>
        {
            var query = request.HttpContext.Request.Query;
            var provider = request.Module.Services.GetRequiredService<TProvider>();
            var content = await getHelp(provider, new AmiModuleHelpRequest(query["topic"].ToString(),
                query.ContainsKey("version") ? query["version"].ToString() : null,
                query.ContainsKey("culture") ? query["culture"].ToString() : null), token);
            if (content == null)
            {
                request.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            request.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            await request.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(content, request.Module.JsonOptions), token);
        }).RequirePolicy(policy);
    }
}
