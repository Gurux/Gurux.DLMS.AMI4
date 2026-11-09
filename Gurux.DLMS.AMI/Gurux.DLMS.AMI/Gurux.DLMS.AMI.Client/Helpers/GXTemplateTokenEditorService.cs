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

using System.Net.Http.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Gurux.UI.Components.TemplateToken;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Connects the reusable template editor to the AMI token API.</summary>
public sealed class GXTemplateTokenEditorService(HttpClient http) : ITemplateTokenEditorService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateTokenDefinition>> GetDefinitionsAsync(TemplateTokenEditorContext context,
        CancellationToken cancellationToken = default)
    {
        var url = "api/TemplateToken?targetType=" + Uri.EscapeDataString(context.TargetType ?? "") +
            "&targetId=" + Uri.EscapeDataString(context.TargetId ?? "") + "&culture=" + Uri.EscapeDataString(context.Culture.Name) + "&page=" + Uri.EscapeDataString(context.Page ?? "");
        using var response = await http.GetAsync(url, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
        var definitions = await response.Content.ReadFromJsonAsync<AmiTemplateTokenDefinition[]>(cancellationToken: cancellationToken) ?? [];
        return definitions.Select(token => new TemplateTokenDefinition { Name = token.Name, Description = token.Description, ExampleValue = token.ExampleValue }).ToArray();
    }

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string text, TemplateTokenEditorContext context,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/TemplateToken/Resolve", new TemplateTokenRequest
        {
            Text = text,
            TargetType = context.TargetType,
            TargetId = context.TargetId,
            Culture = context.Culture.Name,
            Page = context.Page,
            Values = context.Values == null ? null : new Dictionary<string, string?>(context.Values)
        }, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
        return await response.Content.ReadFromJsonAsync<string>(cancellationToken: cancellationToken);
    }
}
