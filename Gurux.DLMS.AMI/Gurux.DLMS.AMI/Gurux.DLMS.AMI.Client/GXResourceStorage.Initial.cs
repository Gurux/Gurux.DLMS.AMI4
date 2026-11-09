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
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client;

public partial class GXResourceStorage
{
    private TaskCompletionSource? _initialCompletion;
    private TaskCompletionSource? _initialWarm;
    private CancellationTokenSource? _initialCancellation;
    private long _initialGeneration;
    private long _initialResourceGeneration;
    private long _initialResourceSnapshotGeneration;
    private string? _initialLanguage;
    private readonly Dictionary<(string Category, string Key), string?> _initialValues = [];
    private readonly SemaphoreSlim _languageGate = new(1, 1);

    public Task InitializeAsync(IEnumerable<string>? menuNames = null, IEnumerable<string>? texts = null)
    {
        lock (_changesLock)
        {
            if (_initialCompletion != null)
            {
                return _initialCompletion.Task;
            }

            _initialGeneration = Interlocked.Read(ref _menuGeneration);
            _initialBlockGeneration = Interlocked.Read(ref _blockGeneration);
            _initialResourceSnapshotGeneration = Interlocked.Read(ref _initialResourceGeneration);
            _initialCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _initialWarm = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _initialCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            _ = InitializeCoreAsync([.. (menuNames ?? []).Distinct().Take(8)],
                [.. (texts ?? []).Distinct().Take(64)]);
            _ = PublishInitialAsync();
            return _initialCompletion.Task;
        }
    }

    private bool InitialCurrent => _initialGeneration == Interlocked.Read(ref _menuGeneration) &&
        _initialResourceSnapshotGeneration == Interlocked.Read(ref _initialResourceGeneration);

    private async Task<(bool Found, string? Value)> InitialValueAsync(string category, string key)
    {
        if (_initialCompletion == null || !InitialCurrent)
        {
            return (false, null);
        }

        // Menus contain user-specific authorization decisions. Wait for the
        // server to validate the preload before displaying a cached menu.
        if (category.StartsWith("menu:", StringComparison.Ordinal))
        {
            await _initialCompletion.Task;
            return (false, null);
        }

        await _initialWarm!.Task;
        if (!InitialCurrent)
        {
            return (false, null);
        }

        lock (_changesLock)
        {
            if (!_initialCompletion.Task.IsCompleted && _initialValues.TryGetValue((category, key), out var value))
            {
                return (true, value);
            }
        }

        await _initialCompletion.Task;
        return (false, null);
    }

    private async Task InitialCultureAsync()
    {
        if (_initialCompletion == null || !InitialCurrent)
        {
            return;
        }

        await _initialWarm!.Task;
        if (_initialLanguage == null && InitialCurrent)
        {
            await _initialCompletion.Task;
        }
    }

    private async Task InitializeCoreAsync(string[] menuNames, string[] texts)
    {
        var states = new Dictionary<string, GXResourceCache.InitialState>(StringComparer.Ordinal);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            Task<AuthenticationState>? authentication;
            Task reset;
            lock (_changesLock)
            {
                _menuAuthentication ??= _menuAuthenticationProvider?.GetAuthenticationStateAsync();
                authentication = _menuAuthentication;
                reset = _menuPermissionReset;
            }
            await reset;
            var user = authentication == null ? new ClaimsPrincipal(new ClaimsIdentity()) : (await authentication).User;
            var userKey = GXClientAuthorizationCache.UserKey(user);
            var permissionEpoch = await _cache.PermissionEpochAsync(storage, http) ?? "initial";
            var manifestGroup = "ami-initial:v1:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":" + userKey + ":" + permissionEpoch;
            var savedLanguage = await storage.GetValueAsync("user", "language");
            var savedCulture = await storage.GetValueAsync("user", "culture");
            string? language = null;
            try
            {
                if (!string.IsNullOrEmpty(savedLanguage))
                {
                    language = CultureInfo.GetCultureInfo(savedLanguage).Name;
                }
            }
            catch (CultureNotFoundException)
            {
            }
            // Cached language is only an offline hint. Re-resolve the profile/browser on the server.
            var request = new GXInitialResources { MenuNames = menuNames, Texts = texts, IncludeDefaults = true };
            var images = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(texts, StringComparer.Ordinal);
            async Task Read(string key, string category, IEnumerable<string> keys, bool conditional = false)
            {
                if (states.TryGetValue(key, out var prior))
                {
                    keys = keys.Concat(prior.Values.Keys).Distinct(StringComparer.Ordinal);
                }

                var state = await _cache.ReadInitialAsync(storage, http, category, keys, conditional,
                    key == "block:preload" ? ValidInitialBlocks : null);
                if ((key == "theme" || key == "iconpack") && !ValidInitial(key,
                    new GXInitialResourceGroup { Version = state.Version ?? "missing", Values = state.Values }, options))
                {
                    state.Values.Remove("current");
                }

                states[key] = state;
                if (state.Version != null)
                {
                    request.Versions[key] = state.Version;
                }

                request.Keys[key] = [.. state.Values.Keys];
                if (state.UsableOffline)
                {
                    lock (_changesLock)
                    {
                        foreach (var value in state.Values)
                        {
                            _initialValues[(category, value.Key)] = value.Value;
                        }
                    }
                }
            }
            async Task<(string? Category, bool Conditional)> Category(string key)
            {
                if (key.StartsWith("menu:", StringComparison.Ordinal))
                {
                    return ("menu:" + userKey + ":" + Uri.EscapeDataString(key[5..]), true);
                }

                if (key == "block:preload")
                {
                    return (await AppearanceCategoryAsync("block-preload", storage, http), true);
                }

                if (key == "appearance:iconpack")
                {
                    return (await AppearanceCategoryAsync("iconpack", storage, http), false);
                }

                if (key is "image" or "theme" or "iconpack")
                {
                    return (await AppearanceCategoryAsync(key, storage, http), false);
                }

                if (key.StartsWith("text:", StringComparison.Ordinal))
                {
                    try
                    {
                        if (CultureInfo.GetCultureInfo(key[5..]).TwoLetterISOLanguageName == key[5..])
                        {
                            return (key, false);
                        }
                    }
                    catch (CultureNotFoundException)
                    {
                    }
                }
                return (null, false);
            }
            try
            {
                var saved = await storage.GetValueAsync(manifestGroup, "catalog");
                var manifest = saved == null ? null : JsonSerializer.Deserialize<Dictionary<string, string[]>>(saved);
                foreach (var entry in (manifest ?? []).Take(128))
                {
                    var partition = await Category(entry.Key);
                    if (partition.Category != null)
                    {
                        await Read(entry.Key, partition.Category, entry.Value.Take(256), partition.Conditional);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cannot read cached preload catalog.");
            }
            foreach (var name in menuNames)
            {
                await Read("menu:" + name, "menu:" + userKey + ":" + Uri.EscapeDataString(name), ["current"], true);
                if (states["menu:" + name].Values.GetValueOrDefault("current") is string body)
                {
                    var menu = JsonSerializer.Deserialize<GetMenuResponse>(body, options)?.Item;
                    foreach (var link in menu?.Links ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(link.Icon))
                        {
                            images.Add(link.Icon);
                        }

                        if (!string.IsNullOrWhiteSpace(link.Name))
                        {
                            names.Add(link.Name);
                        }
                    }
                }
            }
            await Read("theme", await AppearanceCategoryAsync("theme", storage, http), ["current"]);
            await Read("iconpack", await AppearanceCategoryAsync("iconpack", storage, http), ["current"]);
            if (images.Count != 0)
            {
                images.Add("Unknown");
            }

            await Read("image", await AppearanceCategoryAsync("image", storage, http), images);
            if (language != null)
            {
                var languageId = CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName;
                await Read("text:" + languageId, "text:" + languageId, names.Select(ClientHelpers.GetHashCode));
            }
            if (!InitialCurrent)
            {
                return;
            }

            _initialLanguage = language;
            if (language != null)
            {
                try
                {
                    SetInitialCulture(language, savedCulture);
                }
                catch (CultureNotFoundException)
                {
                    SetInitialCulture(language);
                }
            }

            _initialWarm!.TrySetResult();
            using var result = await http.PostAsJsonAsync("api/Resource/Initial", request, _initialCancellation!.Token);
            result.EnsureSuccessStatusCode();
            var response = await result.Content.ReadFromJsonAsync<GXInitialResourcesResponse>(options, _initialCancellation.Token)
                ?? throw new JsonException("Missing initial resources.");
            if (!InitialCurrent)
            {
                return;
            }

            var culture = CultureInfo.GetCultureInfo(response.Language);
            lock (_changesLock)
            {
                foreach (var omitted in states.Where(entry => !response.Resources.ContainsKey(entry.Key)))
                {
                    foreach (var key in _initialValues.Keys.Where(key => key.Category == omitted.Value.Category).ToArray())
                    {
                        _initialValues.Remove(key);
                    }
                }
            }

            var selectedCatalog = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var pair in response.Resources)
            {
                if (!states.TryGetValue(pair.Key, out var state))
                {
                    var partition = await Category(pair.Key);
                    if (partition.Category == null)
                    {
                        continue;
                    }

                    state = await _cache.ReadInitialAsync(storage, http, partition.Category, [], partition.Conditional,
                        pair.Key == "block:preload" ? ValidInitialBlocks : null);
                }
                if (!ValidInitial(pair.Key, pair.Value, options))
                {
                    continue;
                }

                var applied = await _cache.ApplyInitialAsync(storage, http, state, pair.Value,
                    () => InitialCurrent && (pair.Key != "block:preload" || _initialBlockGeneration == Interlocked.Read(ref _blockGeneration)),
                    pair.Key == "block:preload" ? ValidInitialBlocks : null);
                if (!applied)
                {
                    continue;
                }

                if (pair.Key == "block:preload")
                {
                    var body = pair.Value.Values.GetValueOrDefault("current") ??
                        (pair.Value.Version == state.Version ? state.Values.GetValueOrDefault("current") : null);
                    lock (_changesLock)
                    {
                        if (InitialCurrent && _initialBlockGeneration == Interlocked.Read(ref _blockGeneration))
                        {
                            _preloadedBlocksJson = body;
                        }
                    }
                }
                selectedCatalog[pair.Key] = pair.Value.Values.Count == 0 ? [.. state.Values.Keys] : [.. pair.Value.Values.Keys];
            }
            if (!InitialCurrent)
            {
                return;
            }

            try
            {
                await storage.SetValueAsync(manifestGroup, "catalog", JsonSerializer.Serialize(selectedCatalog));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cannot persist preload catalog.");
            }
            await _languageGate.WaitAsync();
            try
            {
                if (!InitialCurrent)
                {
                    return;
                }

                _initialLanguage = culture.Name;
                await storage.SetValueAsync("user", "language", _initialLanguage);
                await storage.SetValueAsync("user", "culture", response.Culture ?? _initialLanguage);
                if (InitialCurrent)
                {
                    SetInitialCulture(_initialLanguage, response.Culture);
                    OnLanguageChanged?.Invoke(this, _initialLanguage);
                }
            }
            finally
            {
                _languageGate.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial resources unavailable; using cached resources and lazy loading.");
        }
        finally
        {
            _initialWarm!.TrySetResult();
            _initialCompletion!.TrySetResult();
        }
    }

    private static bool ValidInitial(string category, GXInitialResourceGroup group, JsonSerializerOptions options)
    {
        if (group.Values == null || string.IsNullOrEmpty(group.Version))
        {
            return false;
        }

        try
        {
            if (category == "block:preload" && group.Values.TryGetValue("current", out var blocks))
            {
                return blocks != null && ValidInitialBlocks(blocks);
            }
            if (category.StartsWith("menu:", StringComparison.Ordinal) && group.Values.TryGetValue("current", out var menu))
            {
                return menu != null && JsonSerializer.Deserialize<GetMenuResponse>(menu, options)?.Item != null;
            }

            if (category == "theme" && group.Values.TryGetValue("current", out var theme))
            {
                if (theme == null)
                {
                    return false;
                }

                JsonSerializer.Deserialize<GXThemeInfo>(theme, options);
            }
            if (category == "iconpack" && group.Values.TryGetValue("current", out var packs))
            {
                return packs != null && JsonSerializer.Deserialize<string[]>(packs, options) != null;
            }

            if (category == "appearance:iconpack")
            {
                return group.Values.All(value => value.Key != "current" && value.Value != null &&
                JsonSerializer.Deserialize<Gurux.DLMS.AMI.Shared.DTOs.GXAppearance>(value.Value, options) != null);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task UpdateLocalizationAsync()
    {
        var generation = Interlocked.Increment(ref _initialResourceGeneration);
        await _languageGate.WaitAsync();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            using var result = await http.PostAsJsonAsync("api/Resource/Initial", new GXInitialResources { IncludeDefaults = false });
            result.EnsureSuccessStatusCode();
            var response = await result.Content.ReadFromJsonAsync<GXInitialResourcesResponse>()
                ?? throw new JsonException("Missing localization settings.");
            if (generation != Interlocked.Read(ref _initialResourceGeneration))
            {
                return;
            }
            SetInitialCulture(response.Language, response.Culture);
            var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            _initialLanguage = _resourceCulture!.Name;
            await storage.SetValueAsync("user", "language", _initialLanguage);
            await storage.SetValueAsync("user", "culture", _formatCulture!.Name);
            OnLanguageChanged?.Invoke(this, _initialLanguage);
        }
        finally
        {
            _languageGate.Release();
        }
    }

    private void SetInitialCulture(string language, string? formattingCulture = null)
    {
        _resourceCulture = CultureInfo.GetCultureInfo(language);
        var format = CultureInfo.GetCultureInfo(formattingCulture ?? language);
        _formatCulture = format.IsNeutralCulture ? CultureInfo.CreateSpecificCulture(format.Name) : format;
        CultureInfo.DefaultThreadCurrentCulture = _formatCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _resourceCulture;
        CultureInfo.CurrentCulture = _formatCulture;
        CultureInfo.CurrentUICulture = _resourceCulture;
    }

    private async Task PublishInitialAsync()
    {
        await _initialCompletion!.Task;
        if (!InitialCurrent)
        {
            return;
        }

        await RefreshMenusAsync();
        await RefreshChangedResourcesAsync(true, true);
    }
}
