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
internal sealed class GXResourceCache(ILogger logger)
{
    private sealed class Group
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public readonly Dictionary<string, string?> Values = [];
        public DateTimeOffset? Version;
        public DateTimeOffset Checked;
        public long Revision;
        public long CheckedRevision = -1;
        public bool ConditionalLoaded;
        public CachedResponse? ConditionalValue;
    }

    private sealed record CachedResponse(string? ETag, string Body, string? PermissionEpoch = null);

    private readonly ConcurrentDictionary<string, Group> groups = new();

    internal sealed record InitialState(string Category, long Revision, string? Version,
        Dictionary<string, string?> Values, bool Conditional, string? PermissionEpoch, bool UsableOffline);

    private static string GroupKey(HttpClient http, string category) =>
        "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;

    public async Task<InitialState> ReadInitialAsync(IGXLocalStorage storage, HttpClient http, string category,
        IEnumerable<string> keys, bool conditional = false, Func<string, bool>? validate = null)
    {
        var groupKey = GroupKey(http, category);
        var group = groups.GetOrAdd(groupKey, _ => new Group());
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

    public async Task<bool> ApplyInitialAsync(IGXLocalStorage storage, HttpClient http, InitialState state,
        GXInitialResourceGroup result, Func<bool> isCurrent, Func<string, bool>? validate = null)
    {
        var groupKey = GroupKey(http, state.Category);
        var group = groups.GetOrAdd(groupKey, _ => new Group());
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

    public Task InvalidateMenuPermissionsAsync(IGXLocalStorage storage, HttpClient http) =>
        WriteAsync(storage, "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":menu",
            "PermissionEpoch", Guid.NewGuid().ToString("N"));

    public async Task<string?> GetConditionalAsync(IGXLocalStorage storage, HttpClient http, string category,
        string url, Func<string, bool> validate, Func<bool> isCurrent, bool allowOffline)
    {
        var groupKey = "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;
        var group = groups.GetOrAdd(groupKey, _ => new Group());
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

    public void Invalidate(string category)
    {
        foreach (var pair in groups)
        {
            if (pair.Key.EndsWith(":" + category, StringComparison.Ordinal) ||
            pair.Key.Contains(":" + category + ":", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref pair.Value.Revision);
            }
        }
    }

    public async Task<string?> GetAsync(IGXLocalStorage storage, HttpClient http, string category, string key,
        Func<Task<DateTimeOffset>> getVersion, Func<Task<string?>> load, Func<bool>? isCurrent = null)
    {
        // Browser storage can be shared by several AMI hosts under one origin.
        var groupKey = "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + category;
        var group = groups.GetOrAdd(groupKey, _ => new Group());
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

    internal Task<string?> PermissionEpochAsync(IGXLocalStorage storage, HttpClient http)
        => ReadAsync(storage, GroupKey(http, "menu"), "PermissionEpoch");

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
