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
/// <param name="navigation">Navigation service used to observe page transitions or redirect to login.</param>
public sealed class GXSessionExpiredHandler(NavigationManager navigation) : DelegatingHandler
{
    /// <summary>Ensures that simultaneous unauthorized API responses trigger only one login redirect.</summary>
    private int _redirecting;

    /// <summary>Sends an HTTP request and redirects expired same-origin API sessions to login while preserving the return path.</summary>
    /// <param name="request">HTTP request forwarded to the inner handler.</param>
    /// <param name="cancellationToken">Token that cancels request transmission or response processing.</param>
    /// <returns>The original HTTP response, including unauthorized responses that initiated a redirect.</returns>
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

        if (Interlocked.Exchange(ref _redirecting, 1) == 0)
        {
            navigation.NavigateTo("Account/Login?returnUrl=" + Uri.EscapeDataString(current), forceLoad: true);
        }
        return response;
    }
}
