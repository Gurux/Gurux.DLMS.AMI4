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
using Gurux.UI.Components;
using Microsoft.JSInterop;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Keeps server resources in IndexedDB and small user preferences in local storage.</summary>
public sealed class GXBrowserStorage(IJSRuntime js, ILogger<GXBrowserStorage> logger,
    GXLocalStorage preferences) : IGXLocalStorage, IAsyncDisposable
{
    private sealed record Entry(string? Value);
    private readonly ConcurrentDictionary<string, Entry> _memory = new();
    private readonly ConcurrentDictionary<string, byte> _clearedGroups = new();
    private Task<IJSObjectReference>? _module;
    private static string Key(string group, string key) => string.IsNullOrEmpty(group) ? key : group + ":" + key;
    private static bool IsResource(string key) => key.StartsWith("ami-resource:", StringComparison.Ordinal) || key.StartsWith("ami-initial:", StringComparison.Ordinal);
    private Task<IJSObjectReference> Module() => _module ??= js.InvokeAsync<IJSObjectReference>("import", "./resource-cache.js").AsTask();

    /// <inheritdoc />
    public Task SetValueAsync(string key, string? value) => SetValueAsync("", key, value);
    /// <inheritdoc />
    public async Task SetValueAsync(string group, string key, string? value)
    {
        var fullKey = Key(group, key);
        if (!IsResource(fullKey))
        {
            await preferences.SetValueAsync(group, key, value);
            return;
        }
        _memory[fullKey] = new(value);
        try
        {
            await (await Module()).InvokeVoidAsync("set", fullKey, value);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Resource persistence unavailable; resources remain in memory.");
        }
    }

    /// <inheritdoc />
    public Task<string?> GetValueAsync(string key) => GetValueAsync("", key);
    /// <inheritdoc />
    public async Task<string?> GetValueAsync(string group, string key)
    {
        var fullKey = Key(group, key);
        if (!IsResource(fullKey))
        {
            return await preferences.GetValueAsync(group, key);
        }
        if (_memory.TryGetValue(fullKey, out var value))
        {
            return value.Value;
        }
        if (_clearedGroups.Keys.Any(g => g.Length == 0 || fullKey.StartsWith(g + ":", StringComparison.Ordinal)))
        {
            return null;
        }
        try
        {
            return await (await Module()).InvokeAsync<string?>("get", fullKey);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Cannot read persisted resources; resources will be retrieved from the server.");
            return null;
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key) => RemoveAsync("", key);
    /// <inheritdoc />
    public Task RemoveAsync(string group, string key) => SetValueAsync(group, key, null);
    /// <inheritdoc />
    public Task ClearAsync() => ClearAsync("");
    /// <inheritdoc />
    public async Task ClearAsync(string group)
    {
        if (!IsResource(group) && !string.IsNullOrEmpty(group))
        {
            await preferences.ClearAsync(group);
            return;
        }
        if (string.IsNullOrEmpty(group))
        {
            await preferences.ClearAsync();
        }
        foreach (var key in _memory.Keys.Where(k => string.IsNullOrEmpty(group) || k.StartsWith(group + ":", StringComparison.Ordinal)))
        {
            _memory.TryRemove(key, out _);
        }
        _clearedGroups[group] = 0;
        try
        {
            await (await Module()).InvokeVoidAsync("clear", group);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Cannot clear persisted resource group {Group}.", group);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>?> GetGroupValuesAsync(string group)
    {
        if (!IsResource(group))
        {
            return await preferences.GetGroupValuesAsync(group);
        }
        var keys = _memory.Where(k => k.Value.Value != null && k.Key.StartsWith(group + ":", StringComparison.Ordinal)).Select(k => k.Key).ToArray();
        try
        {
            var stored = await (await Module()).InvokeAsync<string[]>("keys", group);
            return keys.Concat(stored.Where(k => !_memory.ContainsKey(k) && !_clearedGroups.Keys.Any(g => g.Length == 0 || k.StartsWith(g + ":", StringComparison.Ordinal)))).Distinct().ToArray();
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Cannot enumerate persisted resources.");
            return keys.ToArray();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module?.IsCompletedSuccessfully == true)
        {
            await _module.Result.DisposeAsync();
        }
    }
}
