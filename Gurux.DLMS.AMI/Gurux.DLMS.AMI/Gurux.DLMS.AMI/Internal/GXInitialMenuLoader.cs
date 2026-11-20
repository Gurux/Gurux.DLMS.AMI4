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

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Reads startup menus concurrently while isolating scoped repositories and limiting database pressure.</summary>
internal static class GXInitialMenuLoader
{
    /// <summary>Maximum number of simultaneous menu repository operations during one startup request.</summary>
    private const int MaximumConcurrentReads = 4;

    /// <summary>Bounds retained authorized representations across all users and database instances.</summary>
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 * 1024 });

    /// <summary>Partitions one authorized representation by database instance and dependency fingerprint.</summary>
    /// <param name="Partition">Database or service provider owning the menu definitions.</param>
    /// <param name="Name">Exact menu name.</param>
    /// <param name="Version">Fingerprint containing current content and authorization dependencies.</param>
    private sealed record CacheKey(object Partition, string Name, string Version);

    /// <summary>Contains either the authorized menu or the error handled by the startup resource endpoint.</summary>
    /// <param name="Menu">Menu returned after repository authorization checks, or null when reading failed.</param>
    /// <param name="Error">Failure reported for this menu, or null when reading succeeded.</param>
    internal sealed record Result(GXMenu? Menu, Exception? Error);

    /// <summary>Reuses unchanged authorized menus and reads cache misses in independent scopes with bounded concurrency.</summary>
    /// <param name="names">Distinct menu names selected for the initial resource response.</param>
    /// <param name="scopes">Creates independent repository and authorization service instances.</param>
    /// <param name="cancellationToken">Cancels queued and active menu reads when the request ends.</param>
    /// <param name="versions">Fresh batch dependency fingerprints; absent entries are always read.</param>
    /// <param name="partition">Database identity isolating cache entries between service providers.</param>
    /// <param name="verifyVersions">Rechecks dependencies after full reads, preventing concurrent edits from poisoning the cache.</param>
    /// <returns>Authorized menu results and individual failures indexed by menu name.</returns>
    /// <exception cref="OperationCanceledException">The startup request was canceled.</exception>
    internal static async Task<IReadOnlyDictionary<string, Result>> LoadAsync(IEnumerable<string> names,
        IServiceScopeFactory scopes, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? versions = null, object? partition = null,
        Func<Task<IReadOnlyDictionary<string, string>>>? verifyVersions = null)
    {
        var results = new ConcurrentDictionary<string, Result>(StringComparer.Ordinal);
        var pending = new List<string>();
        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (partition != null && versions?.TryGetValue(name, out var version) == true &&
                _cache.TryGetValue(new CacheKey(partition, name, version), out string? body) && body != null)
            {
                // Deserialize a private copy so a consumer cannot mutate another request's authorization result.
                results[name] = new(JsonSerializer.Deserialize<GXMenu>(body, JsonSerializerOptions.Web), null);
            }
            else
            {
                pending.Add(name);
            }
        }
        await Parallel.ForEachAsync(pending, new ParallelOptions
        {
            MaxDegreeOfParallelism = MaximumConcurrentReads,
            CancellationToken = cancellationToken
        }, async (name, token) =>
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMenuRepository>();
                results[name] = new(await repository.ReadAsync(name, null, token), null);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                results[name] = new(null, error);
            }
        });
        if (partition != null && versions != null && pending.Count != 0)
        {
            var verified = verifyVersions == null ? versions : await verifyVersions();
            foreach (var name in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (results[name] is { Menu: not null, Error: null } result &&
                    versions.TryGetValue(name, out var version) && verified.GetValueOrDefault(name) == version)
                {
                    var body = JsonSerializer.Serialize(result.Menu, JsonSerializerOptions.Web);
                    _cache.Set(new CacheKey(partition, name, version), body,
                        new MemoryCacheEntryOptions { Size = Math.Max(1024, (long)body.Length * sizeof(char)), SlidingExpiration = TimeSpan.FromMinutes(5), AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
                }
            }
        }
        return results;
    }
}
