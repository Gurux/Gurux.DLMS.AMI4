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

using System.Text.Json;
using Gurux.DLMS.AMI.Client.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client;

public partial class GXResourceStorage
{
    private long _blockGeneration;
    private bool _blocksRequested;
    private string? _blocksJson;
    private string? _preloadedBlocksJson;
    private long _initialBlockGeneration;

    /// <inheritdoc />
    public event EventHandler<GXBlocksChangedEventArgs>? BlocksChanged;

    /// <inheritdoc />
    public async Task<IEnumerable<GXBlock>> GetBlocksAsync()
    {
        var generation = Interlocked.Read(ref _blockGeneration);
        lock (_changesLock)
        {
            _blocksRequested = true;
        }
        if (_initialCompletion != null)
        {
            await _initialCompletion.Task;
            lock (_changesLock)
            {
                if (generation != Interlocked.Read(ref _blockGeneration))
                {
                    return [];
                }

                if (_initialBlockGeneration == generation && _preloadedBlocksJson != null && _blocksJson == null)
                {
                    _blocksJson = _preloadedBlocksJson;
                    var preloaded = JsonSerializer.Deserialize<ListBlocksResponse>(_preloadedBlocksJson, JsonSerializerOptions.Web)?.Blocks?.ToArray() ?? [];
                    BlocksChanged?.Invoke(this, new GXBlocksChangedEventArgs { Blocks = preloaded });
                }
            }
        }
        using var scope = _serviceProvider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var category = await AppearanceCategoryAsync("block", storage, http);
        bool Current() => generation == Interlocked.Read(ref _blockGeneration);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Valid(string body) => JsonSerializer.Deserialize<ListBlocksResponse>(body, options)?.Blocks != null;
        var body = await _cache.GetConditionalAsync(storage, http, category, "api/Block/Active", Valid, Current, false);
        if (!Current())
        {
            return [];
        }
        var blocks = body == null ? [] : JsonSerializer.Deserialize<ListBlocksResponse>(body, options)?.Blocks?.ToArray() ?? [];
        // Serialize publishing with invalidation so an old response cannot raise
        // an event after an identity or permission change has cleared the display.
        var displayed = body == null ? [] : JsonSerializer.Deserialize<ListBlocksResponse>(body, options)?.Blocks?.ToArray() ?? [];
        lock (_changesLock)
        {
            if (!Current())
            {
                return [];
            }
            var changed = _blocksJson != body;
            _blocksJson = body;
            if (changed)
            {
                // Event subscribers and callers receive separate DTOs.
                BlocksChanged?.Invoke(this, new GXBlocksChangedEventArgs { Blocks = displayed });
            }
        }
        return blocks;
    }

    /// <inheritdoc />
    public Task RefreshBlocksAsync()
    {
        InvalidateBlocks(false);
        return RefreshRequestedBlocksAsync();
    }

    private void InvalidateBlocks(bool clear)
    {
        lock (_changesLock)
        {
            Interlocked.Increment(ref _blockGeneration);
            _preloadedBlocksJson = null;
            _cache.Invalidate("block");
            _cache.Invalidate("block-preload");
            if (clear)
            {
                _blocksJson = null;
                BlocksChanged?.Invoke(this, new GXBlocksChangedEventArgs());
            }
        }
    }

    private async Task RefreshRequestedBlocksAsync()
    {
        lock (_changesLock)
        {
            if (!_blocksRequested)
            {
                return;
            }
        }
        try
        {
            await GetBlocksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot refresh changed blocks.");
        }
    }

    private static bool ValidInitialBlocks(string body) =>
        JsonSerializer.Deserialize<ListBlocksResponse>(body, JsonSerializerOptions.Web)?.Blocks != null;
}
