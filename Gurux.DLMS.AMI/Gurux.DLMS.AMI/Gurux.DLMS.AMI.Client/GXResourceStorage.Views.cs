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
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client;

public partial class GXResourceStorage
{
    private readonly Dictionary<string, string?> _views = new(StringComparer.Ordinal);
    private long _viewGeneration;
    private bool _viewsDisposed;

    /// <inheritdoc />
    public event EventHandler<GXViewChangedEventArgs>? ViewChanged;

    /// <inheritdoc />
    public async Task<GXView?> GetViewAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        long generation;
        lock (_changesLock)
        {
            if (_viewsDisposed)
            {
                return null;
            }
            generation = Interlocked.Read(ref _viewGeneration);
            _views.TryAdd(name, null);
        }
        bool Current() => !Volatile.Read(ref _viewsDisposed) && generation == Interlocked.Read(ref _viewGeneration);
        using var scope = _serviceProvider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var category = await AppearanceCategoryAsync("view", storage, http) + ":" + Uri.EscapeDataString(name);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        bool Valid(string body) => JsonSerializer.Deserialize<GetViewResponse>(body, options)?.Item != null;
        // An empty-ID representation is a valid cached absence of a default view.
        GXView? Read(string? body)
        {
            var value = body == null ? null : JsonSerializer.Deserialize<GetViewResponse>(body, options)?.Item;
            return value?.Id == Guid.Empty ? null : value;
        }
        var body = await _cache.GetConditionalAsync(storage, http, category,
            "api/View/name?name=" + Uri.EscapeDataString(name), Valid, Current, false);
        if (!Current())
        {
            return null;
        }
        var view = Read(body);
        lock (_changesLock)
        {
            if (!Current())
            {
                return null;
            }
            var changed = _views[name] != body;
            _views[name] = body;
            if (changed)
            {
                // Publishing and invalidation share a lock so stale requests cannot restore cleared views.
                // Every subscriber also receives its own DTO, separate from the caller's instance.
                foreach (EventHandler<GXViewChangedEventArgs> handler in (ViewChanged?.GetInvocationList() ?? []).Cast<EventHandler<GXViewChangedEventArgs>>())
                {
                    if (!Current())
                    {
                        return null;
                    }
                    handler(this, new GXViewChangedEventArgs { Name = name, View = Read(body) });
                }
            }
        }
        return Current() ? view : null;
    }

    /// <inheritdoc />
    public Task RefreshViewsAsync()
    {
        InvalidateViews(false);
        return RefreshRequestedViewsAsync();
    }

    private void InvalidateViews(bool clear)
    {
        lock (_changesLock)
        {
            if (_viewsDisposed)
            {
                return;
            }
            Interlocked.Increment(ref _viewGeneration);
            _cache.Invalidate("view");
            if (clear)
            {
                foreach (var name in _views.Keys.ToArray())
                {
                    _views[name] = null;
                    ViewChanged?.Invoke(this, new GXViewChangedEventArgs { Name = name });
                }
            }
        }
    }

    private async Task RefreshRequestedViewsAsync()
    {
        string[] names;
        lock (_changesLock)
        {
            if (_viewsDisposed)
            {
                return;
            }
            names = [.. _views.Keys];
        }
        foreach (var name in names)
        {
            try
            {
                await GetViewAsync(name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot refresh changed view {Name}.", name);
            }
        }
    }
}
