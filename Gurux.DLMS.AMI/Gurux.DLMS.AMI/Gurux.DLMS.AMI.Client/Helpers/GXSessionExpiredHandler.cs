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
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Handles expired API sessions consistently for all client pages.</summary>
public sealed class GXSessionExpiredHandler(NavigationManager navigation) : DelegatingHandler
{
    private int redirecting;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        var baseUri = new Uri(navigation.BaseUri);
        if (response.StatusCode != HttpStatusCode.Unauthorized || request.RequestUri == null || !baseUri.IsBaseOf(request.RequestUri))
        {
            return response;
        }

        var path = navigation.ToBaseRelativePath(request.RequestUri.AbsoluteUri).Split('?', '#')[0];
        var current = navigation.ToBaseRelativePath(navigation.Uri);
        if (!path.StartsWith("api/", StringComparison.OrdinalIgnoreCase) || path.Equals("api/Session", StringComparison.OrdinalIgnoreCase) || current.StartsWith("Account/", StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }

        if (Interlocked.Exchange(ref redirecting, 1) == 0)
        {
            navigation.NavigateTo("Account/Login?returnUrl=" + Uri.EscapeDataString(current), forceLoad: true);
        }
        return response;
    }
}
