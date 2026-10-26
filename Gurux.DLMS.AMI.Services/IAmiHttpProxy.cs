//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

namespace Gurux.DLMS.AMI.Services;

/// <summary>Module access to the host's allowlisted, credential-free HTTP transport.</summary>
public interface IAmiHttpProxy
{
    /// <summary>Downloads public content using the host network policy and optional scoped cache.</summary>
    Task<AmiHttpProxyContent> GetAsync(string url, AmiHttpProxyOptions options, string? referer = null, CancellationToken cancellationToken = default);

    /// <summary>Invalidates cached responses and pending cache writes for the specified scope.</summary>
    void ClearCache(string cacheScope);
}

/// <summary>Cache preferences; host allowlists and request limits cannot be overridden.</summary>
public sealed class AmiHttpProxyOptions
{
    /// <summary>Stable module-specific cache namespace.</summary>
    public string CacheScope { get; set; } = string.Empty;
    /// <summary>Whether server response caching is enabled for this request.</summary>
    public bool UseCache { get; set; }
    /// <summary>Fallback cache lifetime when the upstream response does not specify freshness.</summary>
    public int CacheTimeoutSeconds { get; set; } = 300;
}

/// <summary>Content downloaded by the AMI server.</summary>
public sealed record AmiHttpProxyContent(byte[] Body, string ContentType, string SourceUrl);

/// <summary>A safe public error from an outbound proxy request.</summary>
public sealed class AmiHttpProxyException(int statusCode, string message) : Exception(message)
{
    /// <summary>HTTP status describing the failure.</summary>
    public int StatusCode { get; } = statusCode;
}
