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

/// <summary>Shares rejection of a PAT across API requests and SignalR negotiation.</summary>
internal sealed class GXAuthenticationState(Action rejected)
{
    private int failed;
    internal bool IsRejected => Volatile.Read(ref failed) != 0;

    internal void Reject()
    {
        if (Interlocked.Exchange(ref failed, 1) == 0)
        {
            rejected();
        }
    }

    internal void ThrowIfRejected()
    {
        if (IsRejected)
        {
            throw new HttpRequestException("AMI authentication failed (HTTP 401).", null, HttpStatusCode.Unauthorized);
        }
    }
}

internal sealed class GXAuthenticationHandler(HttpMessageHandler inner, GXAuthenticationState authentication) : DelegatingHandler(inner)
{
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

/// <summary>Forwards through a caller-owned client without taking ownership of it.</summary>
internal sealed class GXHostHttpHandler(HttpClient client) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // HttpClient rejects sending the same request twice. The content remains owned by the original request.
        using var forwarded = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
            Content = request.Content
        };
        foreach (var header in request.Headers)
        {
            forwarded.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var option in request.Options)
        {
            forwarded.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }
        try
        {
            var response = await client.SendAsync(forwarded, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.RequestMessage = request;
            return response;
        }
        finally
        {
            forwarded.Content = null;
        }
    }
}
