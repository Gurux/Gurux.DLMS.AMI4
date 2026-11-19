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

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Rejects HTTP work after permanent authentication failure and shares newly received rejections.</summary>
/// <param name="inner">Original failure or inner HTTP transport retained by the wrapper.</param>
/// <param name="authentication">Shared authentication rejection state for outgoing requests.</param>
internal sealed class GXAuthenticationHandler(HttpMessageHandler inner, GXAuthenticationState authentication) : DelegatingHandler(inner)
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        authentication.ThrowIfRejected();
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            authentication.Reject();
            authentication.ThrowIfRejected();
        }
        return response;
    }
}
