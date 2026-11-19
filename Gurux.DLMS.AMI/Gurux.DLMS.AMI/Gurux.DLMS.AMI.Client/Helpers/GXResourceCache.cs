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

using Gurux.DLMS.AMI.Client.Rest;
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Gurux.UI.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Versioned resource groups shared by memory and browser storage.</summary>
/// <param name="logger">Logger used to report recoverable client failures.</param>
internal sealed class GXResourceCache(ILogger logger)
{
    /// <summary>In-memory values and validation state for one host-specific resource category.</summary>
    private sealed class Group
    {
        /// <summary>Serializes reads and writes within the resource category.</summary>
        public readonly SemaphoreSlim Gate = new(1, 1);
        /// <summary>Resource bodies indexed by their version-qualified key.</summary>
        public readonly Dictionary<string, string?> Values = [];
        /// <summary>Last known server change timestamp for the category.</summary>
        public DateTimeOffset? Version;
        /// <summary>Time at which server freshness was most recently checked.</summary>
        public DateTimeOffset Checked;
        /// <summary>Invalidation counter incremented when notifications require a fresh read.</summary>
        public long Revision;
        /// <summary>Invalidation counter covered by the most recent server freshness check.</summary>
        public long CheckedRevision = -1;
        /// <summary>Whether a persisted conditional response has already been considered.</summary>
        public bool ConditionalLoaded;
        /// <summary>Validated conditional response available for reuse or permitted offline use.</summary>
        public CachedResponse? ConditionalValue;
    }

    /// <summary>Persisted response body with its HTTP entity tag and permission epoch.</summary>
    /// <param name="ETag">HTTP entity tag associated with the saved response body.</param>
    /// <param name="Body">Validated serialized resource response body.</param>
    /// <param name="PermissionEpoch">Permission epoch under which the conditional response was validated.</param>
    private sealed record CachedResponse(string? ETag, string Body, string? PermissionEpoch = null);

    /// <summary>Independent caches partitioned by API host and resource category.</summary>
    private readonly ConcurrentDictionary<string, Group> _groups = new();

    /// <summary>Snapshot of cache values and validation metadata captured before the initial resource request.</summary>
    /// <param name="Category">Resource category represented by the initial snapshot.</param>
    /// <param name="Revision">Invalidation revision captured before the request started.</param>
    /// <param name="Version">Server version or entity tag associated with the captured values.</param>
    /// <param name="Values">Independent snapshot of resource bodies keyed by resource name.</param>
    /// <param name="Conditional">Whether the snapshot uses conditional entity-tag validation.</param>
    /// <param name="PermissionEpoch">Permission epoch under which the conditional response was validated.</param>
    /// <param name="UsableOffline">Whether the saved response was validated under the current permission epoch.</param>
    internal sealed record InitialState(string Category, long Revision, string? Version,
        Dictionary<string, string?> Values, bool Conditional, string? PermissionEpoch, bool UsableOffline);

    /// <summary>Partitions browser resource storage by the API base address and resource category.</summary>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="category">Resource category whose cache partition is accessed or invalidated.</param>
    /// <returns>An escaped host-specific storage group identifier.</returns>
    private static string GroupKey(HttpClient http, string category) =>
        "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;

    /// <summary>Reads a snapshot of previously stored values for an initial batched resource request.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="category">Resource category whose cache partition is accessed or invalidated.</param>
    /// <param name="keys">Resource keys to include in the initial cache snapshot.</param>
    /// <param name="conditional">Whether the category uses entity-tag based conditional response caching.</param>
    /// <param name="validate">Optional callback that accepts valid cached or received resource representations.</param>
    /// <returns>Cached values, their version and revision, and permission metadata used to validate the response.</returns>
    public async Task<InitialState> ReadInitialAsync(IGXLocalStorage storage, HttpClient http, string category,
        IEnumerable<string> keys, bool conditional = false, Func<string, bool>? validate = null)
    {
        var groupKey = GroupKey(http, category);
        var group = _groups.GetOrAdd(groupKey, _ => new Group());
        await group.Gate.WaitAsync();
        try
        {
            var values = new Dictionary<string, string?>();
            if (conditional)
            {
                var epoch = await ReadAsync(storage, GroupKey(http, "menu"), "PermissionEpoch");
                if (!group.ConditionalLoaded)
                {
                    try
                    {
                        var saved = await ReadAsync(storage, groupKey, "current");
                        var cached = saved == null ? null : JsonSerializer.Deserialize<CachedResponse>(saved);
                        if (cached?.Body != null && (validate?.Invoke(cached.Body) ??
                            JsonSerializer.Deserialize<GetMenuResponse>(cached.Body, JsonSerializerOptions.Web)?.Item != null))
                        {
                            group.ConditionalValue = cached;
                        }
                    }
                    catch (JsonException)
                    {
                    }
                    group.ConditionalLoaded = true;
                }
                if (group.ConditionalValue != null)
                {
                    values["current"] = group.ConditionalValue.Body;
                }

                return new(category, Interlocked.Read(ref group.Revision), group.ConditionalValue?.ETag, values, true, epoch,
                                    group.ConditionalValue?.PermissionEpoch == epoch);
            }
            if (group.Version == null && DateTimeOffset.TryParse(await ReadAsync(storage, groupKey, "LastChanged"),
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var version))
            {
                group.Version = version;
            }

            foreach (var key in keys.Distinct(StringComparer.Ordinal))
            {
                var valueKey = (group.Version?.ToString("O", CultureInfo.InvariantCulture) ?? "unverified") + ":" + key;
                if (group.Values.TryGetValue(valueKey, out var cached))
                {
                    values[key] = cached;
                }
                else if (await ReadAsync(storage, groupKey, valueKey) is string saved)
                {
                    values[key] = saved;
                    group.Values[valueKey] = saved;
                }
            }
            return new(category, Interlocked.Read(ref group.Revision), group.Version?.ToString("O", CultureInfo.InvariantCulture),
                values, false, null, true);
        }
        finally
        {
            group.Gate.Release();
        }
    }

    /// <summary>Applies an initial server response only while its request and cache revision remain current.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="state">Cache snapshot captured before the initial resource request.</param>
    /// <param name="result">Initial resource-group response received from the server.</param>
    /// <param name="isCurrent">Callback that reports whether the caller still represents the active view or session.</param>
    /// <param name="validate">Optional callback that accepts valid cached or received resource representations.</param>
    /// <returns>True when the response was validated and applied to the cache.</returns>
    public async Task<bool> ApplyInitialAsync(IGXLocalStorage storage, HttpClient http, InitialState state,
        GXInitialResourceGroup result, Func<bool> isCurrent, Func<string, bool>? validate = null)
    {
        var groupKey = GroupKey(http, state.Category);
        var group = _groups.GetOrAdd(groupKey, _ => new Group());
        await group.Gate.WaitAsync();
        try
        {
            if (!isCurrent() || group.Revision != state.Revision)
            {
                return false;
            }

            if (state.Conditional)
            {
                var epoch = await ReadAsync(storage, GroupKey(http, "menu"), "PermissionEpoch");
                if (epoch != state.PermissionEpoch)
                {
                    return false;
                }

                var body = result.Values.GetValueOrDefault("current") ?? (result.Version == state.Version ? state.Values.GetValueOrDefault("current") : null);
                if (body == null || !(validate?.Invoke(body) ??
                    JsonSerializer.Deserialize<GetMenuResponse>(body, JsonSerializerOptions.Web)?.Item != null))
                {
                    return false;
                }

                group.ConditionalValue = new CachedResponse(result.Version, body, epoch);
                group.ConditionalLoaded = true;
                await WriteAsync(storage, groupKey, "current", JsonSerializer.Serialize(group.ConditionalValue));
            }
            else
            {
                if (!DateTimeOffset.TryParse(result.Version, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var version))
                {
                    return false;
                }

                if (group.Version != version)
                {
                    group.Values.Clear();
                    try
                    {
                        await storage.ClearAsync(groupKey);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Cannot clear initial resource group.");
                    }
                }
                group.Version = version;
                await WriteAsync(storage, groupKey, "LastChanged", version.ToString("O", CultureInfo.InvariantCulture));
                var values = result.Version == state.Version ? new Dictionary<string, string?>(state.Values) : [];
                foreach (var value in result.Values)
                {
                    values[value.Key] = value.Value;
                }

                foreach (var value in values)
                {
                    var key = version.ToString("O", CultureInfo.InvariantCulture) + ":" + value.Key;
                    group.Values[key] = value.Value;
                    if (value.Value != null)
                    {
                        await WriteAsync(storage, groupKey, key, value.Value);
                    }
                }
            }
            if (!isCurrent() || group.Revision != state.Revision)
            {
                return false;
            }

            group.CheckedRevision = state.Revision;
            group.Checked = DateTimeOffset.UtcNow;
            return true;
        }
        finally
        {
            group.Gate.Release();
        }
    }

    /// <summary>Changes the persisted menu permission epoch so old conditional bodies require revalidation.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <returns>A task that completes after the permission epoch write has been attempted.</returns>
    public Task InvalidateMenuPermissionsAsync(IGXLocalStorage storage, HttpClient http) =>
        WriteAsync(storage, "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":menu",
            "PermissionEpoch", Guid.NewGuid().ToString("N"));

    /// <summary>Revalidates a cached response with its entity tag and applies the caller&apos;s offline and permission rules.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="category">Resource category whose cache partition is accessed or invalidated.</param>
    /// <param name="url">API endpoint from which to load the resource representation.</param>
    /// <param name="validate">Optional callback that accepts valid cached or received resource representations.</param>
    /// <param name="isCurrent">Callback that reports whether the caller still represents the active view or session.</param>
    /// <param name="allowOffline">Whether a cached response may be used when server revalidation fails.</param>
    /// <returns>The validated or permitted cached body, or null when it cannot safely be used.</returns>
    public async Task<string?> GetConditionalAsync(IGXLocalStorage storage, HttpClient http, string category,
        string url, Func<string, bool> validate, Func<bool> isCurrent, bool allowOffline)
    {
        var groupKey = "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;
        var group = _groups.GetOrAdd(groupKey, _ => new Group());
        await group.Gate.WaitAsync();
        try
        {
            if (!isCurrent())
            {
                return null;
            }

            var epoch = await ReadAsync(storage, "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":menu", "PermissionEpoch");
            if (!group.ConditionalLoaded)
            {
                var saved = await ReadAsync(storage, groupKey, "current");
                try
                {
                    var value = saved == null ? null : JsonSerializer.Deserialize<CachedResponse>(saved);
                    if (value?.Body != null && validate(value.Body))
                    {
                        group.ConditionalValue = value;
                    }
                }
                catch (JsonException)
                {
                }
                group.ConditionalLoaded = true;
            }
            var revision = Interlocked.Read(ref group.Revision);
            bool permissionChanged = group.ConditionalValue?.PermissionEpoch != epoch;
            if (permissionChanged || group.CheckedRevision != revision || DateTimeOffset.UtcNow - group.Checked >= TimeSpan.FromMinutes(1))
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (EntityTagHeaderValue.TryParse(group.ConditionalValue?.ETag, out var etag))
                    {
                        request.Headers.IfNoneMatch.Add(etag);
                    }

                    using var response = await http.SendAsync(request);
                    if (!isCurrent())
                    {
                        return null;
                    }

                    if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
                    {
                        group.ConditionalValue = null;
                        try
                        {
                            await storage.RemoveAsync(groupKey, "current");
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Cannot remove denied menu.");
                        }
                        return null;
                    }
                    if (response.StatusCode == HttpStatusCode.NotModified)
                    {
                        if (group.ConditionalValue == null)
                        {
                            return null;
                        }

                        if (permissionChanged)
                        {
                            group.ConditionalValue = group.ConditionalValue with
                            {
                                PermissionEpoch = epoch
                            };
                            await WriteAsync(storage, groupKey, "current", JsonSerializer.Serialize(group.ConditionalValue));
                        }
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        if (!validate(body))
                        {
                            throw new JsonException("Invalid resource representation.");
                        }

                        if (!isCurrent())
                        {
                            return null;
                        }

                        group.ConditionalValue = new CachedResponse(response.Headers.ETag?.ToString(), body, epoch);
                        await WriteAsync(storage, groupKey, "current", JsonSerializer.Serialize(group.ConditionalValue));
                    }
                    group.CheckedRevision = revision;
                    group.Checked = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cannot validate resource {Group}.", category);
                    if (permissionChanged || !allowOffline && group.CheckedRevision != revision)
                    {
                        return null;
                    }
                }
            }
            return isCurrent() ? group.ConditionalValue?.Body : null;
        }
        finally
        {
            group.Gate.Release();
        }
    }

    /// <summary>Marks all host partitions of the specified resource category for revalidation.</summary>
    /// <param name="category">Resource category whose cache partition is accessed or invalidated.</param>
    public void Invalidate(string category)
    {
        foreach (var pair in _groups)
        {
            if (pair.Key.EndsWith(":" + category, StringComparison.Ordinal) ||
            pair.Key.Contains(":" + category + ":", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref pair.Value.Revision);
            }
        }
    }

    /// <summary>Loads a versioned resource through memory, browser storage and the supplied server callbacks.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="category">Resource category whose cache partition is accessed or invalidated.</param>
    /// <param name="key">Resource key within the selected cache or browser storage group.</param>
    /// <param name="getVersion">Callback that loads the server&apos;s current resource version.</param>
    /// <param name="load">Callback that loads a resource body when no cached value is available.</param>
    /// <param name="isCurrent">Callback that reports whether the caller still represents the active view or session.</param>
    /// <returns>The current resource body, or null when unavailable or the caller becomes stale.</returns>
    public async Task<string?> GetAsync(IGXLocalStorage storage, HttpClient http, string category, string key,
        Func<Task<DateTimeOffset>> getVersion, Func<Task<string?>> load, Func<bool>? isCurrent = null)
    {
        // Browser storage can be shared by several AMI hosts under one origin.
        var groupKey = "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;
        var group = _groups.GetOrAdd(groupKey, _ => new Group());
        await group.Gate.WaitAsync();
        try
        {
            if (isCurrent?.Invoke() == false)
            {
                return null;
            }

            var revision = Interlocked.Read(ref group.Revision);
            var now = DateTimeOffset.UtcNow;
            if (group.CheckedRevision != revision || now - group.Checked >= TimeSpan.FromMinutes(1))
            {
                if (group.Version == null)
                {
                    var saved = await ReadAsync(storage, groupKey, "LastChanged");
                    if (DateTimeOffset.TryParse(saved, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var version))
                    {
                        group.Version = version;
                    }
                }
                try
                {
                    var version = await getVersion();
                    if (version != group.Version)
                    {
                        group.Values.Clear();
                        // Clear only this resource group; leave user settings and other resources intact.
                        try
                        {
                            await storage.ClearAsync(groupKey);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Cannot clear resource storage {Group}.", category);
                        }
                        group.Version = version;
                        await WriteAsync(storage, groupKey, "LastChanged", version.ToString("O", CultureInfo.InvariantCulture));
                    }
                    group.CheckedRevision = revision;
                }
                catch (Exception ex)
                {
                    // Keep the previous version and its values while the server is unavailable.
                    logger.LogWarning(ex, "Cannot check resource version {Group}.", category);
                    group.CheckedRevision = revision;
                }
                group.Checked = now;
            }

            // Include the version in each key, so a failed clear cannot expose old values.
            var valueKey = (group.Version?.ToString("O", CultureInfo.InvariantCulture) ?? "unverified") + ":" + key;
            if (isCurrent?.Invoke() == false)
            {
                return null;
            }

            if (group.Values.TryGetValue(valueKey, out var cached))
            {
                return cached;
            }

            var value = await ReadAsync(storage, groupKey, valueKey);
            if (value == null)
            {
                value = await load();
                if (isCurrent?.Invoke() == false || revision != Interlocked.Read(ref group.Revision))
                {
                    return null;
                }

                if (value != null)
                {
                    await WriteAsync(storage, groupKey, valueKey, value);
                }
            }
            if (value != null)
            {
                group.Values[valueKey] = value;
            }

            return value;
        }
        finally
        {
            group.Gate.Release();
        }
    }

    /// <summary>Reads the persisted epoch used to invalidate conditional menu permissions.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <returns>The stored permission epoch, or null when unavailable.</returns>
    internal Task<string?> PermissionEpochAsync(IGXLocalStorage storage, HttpClient http)
        => ReadAsync(storage, GroupKey(http, "menu"), "PermissionEpoch");

    /// <summary>Reads browser storage and logs storage failures without preventing resource loading.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="group">Host-specific browser storage partition.</param>
    /// <param name="key">Resource key within the selected cache or browser storage group.</param>
    /// <returns>The stored value, or null when absent or unreadable.</returns>
    private async Task<string?> ReadAsync(IGXLocalStorage storage, string group, string key)
    {
        try
        {
            return await storage.GetValueAsync(group, key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot read resource storage.");
            return null;
        }
    }

    /// <summary>Writes browser storage and logs failures without interrupting the caller&apos;s resource operation.</summary>
    /// <param name="storage">Browser storage service used to persist resource snapshots.</param>
    /// <param name="group">Host-specific browser storage partition.</param>
    /// <param name="key">Resource key within the selected cache or browser storage group.</param>
    /// <param name="value">Serialized resource value to persist in browser storage.</param>
    /// <returns>A task that completes after the storage write has been attempted.</returns>
    private async Task WriteAsync(IGXLocalStorage storage, string group, string key, string value)
    {
        try
        {
            await storage.SetValueAsync(group, key, value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot write resource storage.");
        }
    }
}
