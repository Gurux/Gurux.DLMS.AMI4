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
using System.Net.Http.Json;
using Gurux.UI.Components;
using Gurux.UI.Components.Help;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Loads help from an active module's own authorized provider endpoint.</summary>
/// <param name="http">Client transport that carries the current AMI authentication state.</param>
public sealed class GXModuleHelpProvider(HttpClient http) : IGXHelpProvider
{
    /// <summary>Resolves a module overview, configuration or view guide without loading its server assembly.</summary>
    /// <param name="request">Topic in the form module:ModuleId, optionally followed by :configuration or :view.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The module's documentation, or null for an unrelated or unavailable topic.</returns>
    public async Task<GXHelpContent?> GetHelpAsync(GXHelpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var parts = request.Topic.Split(':');
        if (parts.Length is < 2 or > 3 || parts[0] != "module" || string.IsNullOrWhiteSpace(parts[1]) ||
            (parts.Length == 3 && parts[2] is not ("configuration" or "view")))
        {
            return null;
        }
        var address = "api/modules/" + Uri.EscapeDataString(parts[1]) + "/help?topic=" + Uri.EscapeDataString(request.Topic);
        if (request.Version != null)
        {
            address += "&version=" + Uri.EscapeDataString(request.Version);
        }
        if (request.Culture != null)
        {
            address += "&culture=" + Uri.EscapeDataString(request.Culture);
        }
        using var response = await http.GetAsync(address, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable or HttpStatusCode.Conflict)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GXHelpContent>(cancellationToken);
    }
}
