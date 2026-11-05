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

namespace Gurux.DLMS.AMI.Services;

/// <summary>Module access to the host's allowlisted, credential-free HTTP transport.</summary>
public interface IGXAmiHttpProxy
{
    /// <summary>Downloads public content using the host network policy and optional scoped cache.</summary>
    /// <param name="url">The public content URL to request through the host network policy.</param>
    /// <param name="options">Cache preferences for this request; these do not override host network restrictions.</param>
    /// <param name="referer">Optional Referer header value supplied to the upstream server.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task whose result contains the downloaded body, content type, and source URL.</returns>
    Task<GXAmiHttpProxyContent> GetAsync(string url, GXAmiHttpProxyOptions options, string? referer = null, CancellationToken cancellationToken = default);

    /// <summary>Invalidates cached responses and pending cache writes for the specified scope.</summary>
    /// <param name="cacheScope">The cache namespace to invalidate, matching the request options.</param>
    void ClearCache(string cacheScope);
}

/// <summary>Cache preferences; host allowlists and request limits cannot be overridden.</summary>
public sealed class GXAmiHttpProxyOptions
{
    /// <summary>Stable module-specific cache namespace.</summary>
    public string CacheScope { get; set; } = string.Empty;
    /// <summary>Whether server response caching is enabled for this request.</summary>
    public bool UseCache { get; set; }
    /// <summary>Fallback cache lifetime when the upstream response does not specify freshness.</summary>
    public int CacheTimeoutSeconds { get; set; } = 300;
}

/// <summary>Content downloaded by the AMI server.</summary>
/// <param name="Body">The downloaded response body as bytes.</param>
/// <param name="ContentType">The response media type reported by the proxy.</param>
/// <param name="SourceUrl">The source URL reported by the proxy.</param>
public sealed record GXAmiHttpProxyContent(byte[] Body, string ContentType, string SourceUrl);

/// <summary>A safe public error from an outbound proxy request.</summary>
/// <param name="statusCode">HTTP status code describing the proxy failure.</param>
/// <param name="message">Public error description suitable for the caller.</param>
public sealed class GXAmiHttpProxyException(int statusCode, string message) : Exception(message)
{
    /// <summary>HTTP status describing the failure.</summary>
    public int StatusCode { get; } = statusCode;
}
