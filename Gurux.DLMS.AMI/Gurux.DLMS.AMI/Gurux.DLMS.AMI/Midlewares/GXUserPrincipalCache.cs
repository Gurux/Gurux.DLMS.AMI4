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

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Gurux.DLMS.AMI.Server.Midlewares;

/// <summary>Caches immutable snapshots of the current user grants independently of authentication cookies.</summary>
internal sealed class GXUserPrincipalCache(IMemoryCache cache)
{
    static long generation;
    readonly Lock gate = new();

    /// <summary>Invalidate grant snapshots after user or role changes have been saved.</summary>
    public static void InvalidateAll() => Interlocked.Increment(ref generation);

    public async Task<ClaimsPrincipal> GetAsync(string userId, string? stamp, Func<Task<ClaimsPrincipal>> load)
    {
        while (true)
        {
            var version = Interlocked.Read(ref generation);
            var key = ("user-grants", userId, stamp, version);
            Lazy<Task<byte[]>> pending;
            lock (gate)
            {
                if (!cache.TryGetValue(key, out pending!))
                {
                    pending = new Lazy<Task<byte[]>>(async () => TicketSerializer.Default.Serialize(
                        new AuthenticationTicket(await load(), "grants")), LazyThreadSafetyMode.ExecutionAndPublication);
                    cache.Set(key, pending, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(30),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    });
                }
            }
            byte[] snapshot;
            try
            {
                snapshot = await pending.Value;
            }
            catch
            {
                lock (gate)
                {
                    if (cache.TryGetValue(key, out Lazy<Task<byte[]>>? current) && ReferenceEquals(current, pending))
                    {
                        cache.Remove(key);
                    }
                }
            
                throw;
            }
            // Never return grants loaded concurrently with a committed role/user change.
            if (version != Interlocked.Read(ref generation))
            {
                continue;
            }
            return TicketSerializer.Default.Deserialize(snapshot)!.Principal;
        }
    }
}
