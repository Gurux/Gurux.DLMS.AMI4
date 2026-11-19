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
namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Reads proxy response bodies in bounded chunks while enforcing the configured byte limit.</summary>
internal static class GXHttpProxyBuffer
{
    /// <summary>Maximum size of one response-body read buffer in bytes.</summary>
    internal const int ChunkSize = 64 * 1024;

    // Fixed chunks avoid MemoryStream capacity growth and keep assembly below
    // two body copies plus one chunk. The public response copy is reserved too.
    /// <summary>Reads a response body without permitting its buffered length to exceed the configured limit.</summary>
    /// <param name="stream">The upstream response body to read.</param>
    /// <param name="maximum">The maximum permitted body size in bytes.</param>
    /// <param name="token">Cancels reading the response stream.</param>
    /// <returns>The complete response bytes within the permitted limit.</returns>
    /// <exception cref="GXHttpException">The response body exceeds the configured limit.</exception>
    internal static async Task<byte[]> ReadAsync(Stream stream, int maximum, CancellationToken token)
    {
        var chunks = new List<(byte[] Bytes, int Count)>();
        var length = 0;
        while (true)
        {
            var chunk = new byte[Math.Min(ChunkSize, maximum - length + 1)];
            var used = 0;
            while (used < chunk.Length)
            {
                var count = await stream.ReadAsync(chunk.AsMemory(used), token);
                if (count == 0) {
                    break;
                }
                used += count;
                if ((long)length + used > maximum)
                    {
                    throw new GXHttpException(Properties.Resources.HttpProxyResponseTooLarge, System.Net.HttpStatusCode.RequestEntityTooLarge);
                }
            }
            if (used == 0) {
                break;
            }
            chunks.Add((chunk, used));
            length += used;
            if (used < chunk.Length) {
                break;
            }
        }
        var result = GC.AllocateUninitializedArray<byte>(length);
        var offset = 0;
        foreach (var (bytes, count) in chunks)
        {
            bytes.AsSpan(0, count).CopyTo(result.AsSpan(offset));
            offset += count;
        }
        return result;
    }
}
