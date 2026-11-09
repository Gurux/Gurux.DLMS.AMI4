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

using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Server.Token;

/// <summary>Provides public site details and values belonging to the current user.</summary>
public sealed class GXSiteTemplateTokenProvider(IConfiguration configuration) : IGXAmiTemplateTokenProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var definitions = new List<AmiTemplateTokenDefinition>
        {
            new() { Name = "site:name", Description = "Site name", ExampleValue = "Gurux DLMS AMI" },
            new() { Name = "site:url", Description = "Site address", ExampleValue = "https://example.com" },
            new() { Name = "date:now", Description = "Current date and time (UTC)", ExampleValue = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).ToString("g", context.Culture) + " UTC" }
        };
        if (context.User.Identity?.IsAuthenticated == true)
            {
            definitions.Add(new() { Name = "user:name", Description = "Current user's name", ExampleValue = "User" });
        }
        return Task.FromResult<IReadOnlyList<AmiTemplateTokenDefinition>>(definitions);
    }

    /// <inheritdoc />
    public Task<string?> ResolveAsync(string name, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? value = name switch
        {
            "site:name" => configuration["TemplateTokens:SiteName"] ?? "Gurux DLMS AMI",
            "site:url" => ServerSettings.ServerAddress,
            "date:now" => DateTimeOffset.UtcNow.ToString("g", context.Culture) + " UTC",
            "user:name" when context.User.Identity?.IsAuthenticated == true => context.User.Identity.Name,
            _ => null
        };
        return Task.FromResult(value);
    }
}
