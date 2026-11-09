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

using System.Net.Http.Headers;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace Gurux.DLMS.AMI.Server.Services;

public sealed partial class GXHttpProxyService
{
    private sealed record Entry(GXHttpProxyResponse Response, string? ETag, DateTimeOffset? LastModified,
        TimeSpan FreshnessLifetime, CacheControlHeaderValue? CacheControl, DateTimeOffset Date, DateTimeOffset? Expires);

    private GXHttpProxyResponse Store(string key, string scope, byte[] body, string contentType, Uri source,
        HttpResponseMessage upstream, HttpProxySettings settings, Entry? previous, DateTimeOffset received,
        TimeSpan responseDelay, long receivedTimestamp)
    {
        // A 304 updates the stored representation's metadata. Retain fields omitted by validation.
        var control = upstream.Headers.CacheControl ?? previous?.CacheControl;
        var date = upstream.Headers.Date ?? previous?.Date ?? received;
        var expires = upstream.Content.Headers.Expires ?? previous?.Expires;
        var freshness = control?.SharedMaxAge ?? control?.MaxAge ??
            (expires.HasValue ? expires.Value - date : previous?.FreshnessLifetime ?? TimeSpan.FromSeconds(settings.CacheTimeoutSeconds));
        freshness = Clamp(freshness, TimeSpan.FromDays(365));

        // RFC 9111 4.2.3: apparent_age vs Age + response_delay, then add time
        // spent receiving/assembling the body. Use the monotonic clock for elapsed time.
        var apparentAge = received > date ? received - date : TimeSpan.Zero;
        var correctedAge = Clamp(upstream.Headers.Age ?? TimeSpan.Zero, TimeSpan.FromDays(3650)) + responseDelay;
        var age = (apparentAge > correctedAge ? apparentAge : correctedAge) + _time.GetElapsedTime(receivedTimestamp);
        var remaining = freshness > age ? freshness - age : TimeSpan.Zero;
        var cacheable = settings.CacheTimeoutSeconds > 0 && control?.NoStore != true && control?.Private != true &&
            !upstream.Headers.Contains("Set-Cookie") &&
            upstream.Headers.Vary.All(v => v.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase));
        if (control?.NoCache == true) {
            remaining = TimeSpan.Zero;
        }
        var result = new GXHttpProxyResponse(body, contentType, _time.GetUtcNow() + (cacheable ? remaining : TimeSpan.Zero), source.AbsoluteUri);
        lock (_cacheLock)
        {
            if (!_disposed)
            {
                if (cacheable && key.StartsWith(CachePrefix(scope), StringComparison.Ordinal))
                {
                    var entry = new Entry(result, upstream.Headers.ETag?.ToString() ?? previous?.ETag,
                        upstream.Content.Headers.LastModified ?? previous?.LastModified, freshness, control, date, expires);
                    _cache.Set(key, entry, new MemoryCacheEntryOptions
                    {
                        Size = body.Length + 1024,
                        AbsoluteExpirationRelativeToNow = remaining + TimeSpan.FromDays(1)
                    });
                }
                else {
                    _cache.Remove(key);
                }
            }
        }
        return result;
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan maximum)
        => value < TimeSpan.Zero ? TimeSpan.Zero : value > maximum ? maximum : value;
}
