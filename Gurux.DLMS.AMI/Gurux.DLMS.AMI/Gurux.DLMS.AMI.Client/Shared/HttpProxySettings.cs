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

using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Settings persisted in the http-proxy GXConfiguration.</summary>
public sealed class HttpProxySettings
{
    /// <summary>Whether the proxy accepts requests.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Exact allowed origins, including scheme and optional port; no wildcards or paths.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Fallback cache lifetime in seconds; zero disables caching. Upstream cache directives take priority.</summary>
    [Range(0, 604800)]
    public int CacheTimeoutSeconds { get; set; } = 300;

    /// <summary>Timeout covering redirects, headers and response body, in seconds.</summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum decompressed response size in bytes.</summary>
    [Range(1, 52428800)]
    public int MaxResponseBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Validates persisted settings before saving or using them.</summary>
    public void Validate()
    {
        Validator.ValidateObject(this, new ValidationContext(this), true);
        if (AllowedOrigins is null || AllowedOrigins.Length > 100)
        {
            throw new ValidationException("Provide at most 100 allowed origins.");
        }
        foreach (var value in AllowedOrigins)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var origin) ||
                (origin.Scheme != "http" && origin.Scheme != "https") ||
                origin.IdnHost.Contains('*') ||
                origin.HostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6) ||
                !string.IsNullOrEmpty(origin.UserInfo) || origin.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
            {
                throw new ValidationException("Allowed origins must contain only a scheme, host and optional port, for example https://gurux.fi.");
            }
        }
    }
}
