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
using System.Net;
using System.Net.Http.Json;
using Gurux.UI.Components;
using Gurux.UI.Components.Help;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Loads Gurux COSEM articles without website navigation through the authenticated AMI server.</summary>
public sealed class GXCosemHelpProvider(HttpClient http) : IGXHelpProvider
{
    /// <inheritdoc />
    public async Task<GXHelpContent?> GetHelpAsync(GXHelpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Topic.StartsWith("cosem.", StringComparison.Ordinal))
        {
            return null;
        }
        var culture = request.Culture ?? CultureInfo.CurrentUICulture.Name;
        var address = "api/Help/Cosem?topic=" + Uri.EscapeDataString(request.Topic) + "&culture=" + Uri.EscapeDataString(culture);
        using var response = await http.GetAsync(address, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GXHelpContent>(cancellationToken);
    }
}
