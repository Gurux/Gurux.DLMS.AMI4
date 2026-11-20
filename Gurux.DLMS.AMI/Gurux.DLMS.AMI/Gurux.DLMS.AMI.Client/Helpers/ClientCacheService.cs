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

namespace Gurux.DLMS.AMI.Client.Helpers
{
    /// <summary>Maintains client-side cache entries and expires them when they are read.</summary>
    public sealed class ClientCacheService
    {
        /// <summary>Stores cached values and their absolute expiration times by key.</summary>
        private readonly Dictionary<string, CacheItem<object>> _storage = [];

        /// <summary>Adds a cache entry with an expiration relative to the current UTC time.</summary>
        /// <typeparam name="T">The type of value being cached.</typeparam>
        /// <param name="key">The unique key for the cache entry.</param>
        /// <param name="value">The value retained until expiration or removal.</param>
        /// <param name="lifetime">The duration for which the entry remains valid.</param>
        /// <returns>A completed task after the entry has been added.</returns>
        /// <exception cref="ArgumentException">The key already exists in the cache.</exception>
        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan lifetime)
        {
            _storage.Add(key, new CacheItem<object>
            {
                Value = value,
                ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
            });
            return Task.CompletedTask;
        }

        /// <summary>Returns an unexpired cached value and removes entries that have expired.</summary>
        /// <typeparam name="T">The expected cached value type.</typeparam>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The cached value, or the default value when absent or expired.</returns>
        /// <exception cref="InvalidCastException">The cached value is not compatible with the requested type.</exception>
        public async Task<T?> GetAsync<T>(string key)
        {
            if (_storage.TryGetValue(key, out CacheItem<object>? cacheItem))
            {
                if (cacheItem.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    await RemoveAsync(key);
                    return default;
                }
                return (T?)cacheItem.Value;
            }
            return default;
        }

        /// <summary>Removes the selected entry if it exists.</summary>
        /// <param name="key">The key of the entry to remove.</param>
        /// <returns>A completed task after removal.</returns>
        public Task RemoveAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>Removes all cached entries.</summary>
        /// <returns>A completed task after all entries have been removed.</returns>
        public Task ClearAsync()
        {
            _storage.Clear();
            return Task.CompletedTask;
        }
    }
}
