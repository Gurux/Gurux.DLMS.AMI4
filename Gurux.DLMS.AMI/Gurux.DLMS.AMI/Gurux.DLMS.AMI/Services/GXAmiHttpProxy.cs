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

using Gurux.DLMS.AMI.Services;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Applies host network policy to HTTP requests from runtime modules.</summary>
public sealed class GXAmiHttpProxy(GXHttpProxyConfiguration configuration, GXHttpProxyService proxy) : IGXAmiHttpProxy
{
    /// <inheritdoc />
    public async Task<GXAmiHttpProxyContent> GetAsync(string url, GXAmiHttpProxyOptions options, string? referer = null, CancellationToken cancellationToken = default)
    {
        var settings = await configuration.ReadAsync(cancellationToken);
        settings.CacheTimeoutSeconds = options.UseCache && settings.CacheTimeoutSeconds > 0 ? options.CacheTimeoutSeconds : 0;
        try
        {
            var content = await proxy.GetAsync(url, settings, referer, cancellationToken, options.CacheScope);
            return new(content.Body, content.ContentType, content.SourceUrl);
        }
        catch (GXHttpProxyException ex)
        {
            throw new GXAmiHttpProxyException(ex.StatusCode, ex.Message);
        }
    }

    /// <inheritdoc />
    public void ClearCache(string cacheScope) => proxy.ClearCache(cacheScope);
}
