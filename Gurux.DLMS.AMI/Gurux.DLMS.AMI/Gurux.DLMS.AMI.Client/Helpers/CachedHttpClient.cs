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

using Gurux.DLMS.AMI.Http;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    /// <summary>Reads JSON resources over HTTP and retains successful responses in the client cache.</summary>
    /// <param name="http">The HTTP client used when a cached response is unavailable.</param>
    /// <param name="cache">The cache storing responses by URL.</param>
    /// <param name="logger">Receives cache and request diagnostics.</param>
    public class CachedHttpClient(
        HttpClient http,
        ClientCacheService cache,
        ILogger<CachedHttpClient> logger)
    {
        /// <summary>Retrieves responses that are missing from the cache.</summary>
        private readonly HttpClient _http = http;
        /// <summary>Stores responses by their request URL.</summary>
        private readonly ClientCacheService _cache = cache;
        /// <summary>Receives HTTP and cache-operation diagnostics.</summary>
        private readonly ILogger<CachedHttpClient> _logger = logger;

        /// <summary>Returns a cached response or retrieves and caches a non-null HTTP response.</summary>
        /// <typeparam name="T">The response model deserialized from JSON.</typeparam>
        /// <param name="url">The request URL and cache key.</param>
        /// <param name="cacheTime">The response lifetime; the default value uses sixty minutes.</param>
        /// <returns>The cached or retrieved response, or null when the response has no model.</returns>
        public async Task<T?> GetAsync<T>(
            string url,
            TimeSpan cacheTime = default)
        {
            if (cacheTime == default)
            {
                cacheTime = TimeSpan.FromMinutes(60);
            }
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Getting data from {Url} with cache time {CacheTime}", url, cacheTime);
            }

            var cached = await _cache.GetAsync<T>(url);
            if (cached != null)
            {
                return cached;
            }
            var result = await _http.GetAsJsonAsync<T>(url);

            if (result != null)
            {
                await _cache.SetAsync(
                    url,
                    result,
                    cacheTime);
            }
            return result;
        }

        /// <summary>
        /// Remove cache for the given url.
        /// </summary>
        /// <param name="url">Removed url.</param>
        /// <returns>A completed task after requesting removal of the cache entry.</returns>
        public Task RemoveAsync(string url)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                _logger.LogInformation("Removing cache for {Url}", url);
            }
            _cache.RemoveAsync(url);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear cache.
        /// </summary>
        /// <returns>A completed task after requesting removal of all cache entries.</returns>
        public Task ClearAsync()
        {
            _logger.LogInformation("Clearing all cache");
            _cache.ClearAsync();
            return Task.CompletedTask;
        }
    }
}
