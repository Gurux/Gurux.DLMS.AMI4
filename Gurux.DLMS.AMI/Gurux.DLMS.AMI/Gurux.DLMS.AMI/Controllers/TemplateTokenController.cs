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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Discovers and previews template tokens in the authenticated user's context.</summary>
[ApiController, Route("api/TemplateToken"), Authorize]
public sealed class TemplateTokenController(IGXAmiTemplateTokenResolver resolver) : ControllerBase
{
    private AmiTemplateTokenContext Context(string? targetType, string? targetId, string? culture, string? page = null, IReadOnlyDictionary<string, string?>? values = null) => new()
    {
        User = User,
        TargetType = targetType,
        TargetId = targetId,
        Page = page,
        Values = values,
        Culture = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(culture)
    };

    /// <summary>Lists available token names and descriptions.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AmiTemplateTokenDefinition>>> Get(string? targetType,
        string? targetId, string? culture, CancellationToken cancellationToken, string? page = null)
    {
        Response.Headers.CacheControl = "no-store";
        try
        { return Ok(await resolver.GetDefinitionsAsync(Context(targetType, targetId, culture, page), cancellationToken)); }
        catch (CultureNotFoundException) { return BadRequest("Invalid culture."); }
    }

    /// <summary>Resolves template text for a plain-text preview.</summary>
    [HttpPost("Resolve")]
    public async Task<ActionResult<string>> Resolve(TemplateTokenRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (request.Values is { } values && (values.Count > 32 || values.Any(p => p.Key.Length > 128 || p.Value?.Length > 1024)))
            {
            return BadRequest("Template page values are too large.");
        }
        try
        { return new JsonResult(await resolver.ResolveAsync(request.Text, Context(request.TargetType, request.TargetId, request.Culture, request.Page, request.Values), cancellationToken)); }
        catch (CultureNotFoundException) { return BadRequest("Invalid culture."); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
