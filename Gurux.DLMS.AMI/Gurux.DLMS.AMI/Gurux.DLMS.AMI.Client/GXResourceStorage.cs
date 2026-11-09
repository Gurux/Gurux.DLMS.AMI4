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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Internal;
using Gurux.DLMS.AMI.Http;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// Resource service is used to get resource from the cache or server if it's not loaded yet.
    /// </summary>
    public partial class GXResourceStorage : IGXResourceStorage, IAmiMenuStorage, IAmiNavigationMenuStorage, IAmiBlockStorage, IAmiViewStorage, IDisposable
    {
        private bool? _utcTime;
        private CultureInfo? _formatCulture;
        private CultureInfo? _resourceCulture;
        private readonly IServiceProvider _serviceProvider;
        /// <inheritdoc/>
        public EventCallback OnEnumTypesChanged { get; set; }

        /// <inheritdoc/>
        public event EventHandler<GXTextChangedEventArgs>? OnTextChanged;

        /// <inheritdoc/>
        public event EventHandler<GXThemeInfo>? OnThemeChanged;
        public event EventHandler<IEnumerable<string>>? OnIconPacksChanged;
        private string? _iconPackJson;
        private bool _iconPacksRequested;
        /// <inheritdoc/>
        public event EventHandler<GXImageChangedArgs>? OnImageChanged;
        public event EventHandler<string?>? OnLanguageChanged;

        /// <summary>
        /// Cached enum types.
        /// </summary>
        private readonly Dictionary<string, Dictionary<int, string>> _enumTypes = [];

        /// <summary>
        /// Cached values.
        /// </summary>
        private readonly GXResourceCache _cache;
        private readonly IGXNotifier _notifier;
        private readonly ILogger _logger;
        private readonly Lock _changesLock = new();
        private readonly Dictionary<string, string?> _images = [];
        private readonly Dictionary<(string Language, string Name), string> _texts = [];
        private bool _themeRequested;
        private string? _themeJson;
        private readonly Dictionary<string, string?> _menus = [];
        private readonly AuthenticationStateProvider? _menuAuthenticationProvider;
        private Task<AuthenticationState>? _menuAuthentication;
        private long _menuGeneration;
        private Task _menuPermissionReset = Task.CompletedTask;
        public event EventHandler<GXMenuChangedEventArgs>? MenuChanged;
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXResourceStorage(IServiceProvider serviceProvider,
            IGXNotifier notifier)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger(nameof(GXResourceStorage));

            _serviceProvider = serviceProvider;
            _cache = new GXResourceCache(_logger);
            _notifier = notifier;
            _menuAuthenticationProvider = serviceProvider.GetService<AuthenticationStateProvider>();
            if (_menuAuthenticationProvider != null)
            {
                _menuAuthenticationProvider.AuthenticationStateChanged += MenuAuthenticationChanged;
            }

            notifier.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), InvalidateResources);
        }

        private void InvalidateResources(IEnumerable<GXTarget> items)
        {
            bool appearance = false, text = false, menu = false, permissions = false, block = false, blockPermissions = false, view = false, viewPermissions = false;
            foreach (var item in items)
            {
                if (item.Type == GXViewPolicies.Add || item.Type == GXViewPolicies.Edit || item.Type == GXViewPolicies.Delete)
                {
                    view = true;
                }
                if (item.Type == GXViewGroupPolicies.Add || item.Type == GXViewGroupPolicies.Edit || item.Type == GXViewGroupPolicies.Delete)
                {
                    viewPermissions = true;
                }
                if (item.Type == GXBlockPolicies.Add || item.Type == GXBlockPolicies.Edit ||
                    item.Type == GXBlockPolicies.Delete || item.Type == GXBlockPolicies.Close ||
                    item.Type == GXBlockPolicies.Regenerate ||
                    item.Type == GXComponentViewPolicies.Add || item.Type == GXComponentViewPolicies.Edit ||
                    item.Type == GXComponentViewPolicies.Delete)
                {
                    block = true;
                }
                if (item.Type == GXBlockGroupPolicies.Add || item.Type == GXBlockGroupPolicies.Edit ||
                    item.Type == GXBlockGroupPolicies.Delete)
                {
                    blockPermissions = true;
                }
                if (item.Type == GXAppearancePolicies.Add || item.Type == GXAppearancePolicies.Edit || item.Type == GXAppearancePolicies.Delete)
                {
                    appearance = true;
                    _cache.Invalidate("image");
                    _cache.Invalidate("theme");
                    _cache.Invalidate("iconpack");
                }
                else if (item.Type == GXLocalizedResourcePolicies.Add || item.Type == GXLocalizedResourcePolicies.Edit || item.Type == GXLocalizedResourcePolicies.Delete)
                {
                    text = true;
                    _cache.Invalidate("text");
                }
                if (item.Type == GXMenuPolicies.Add || item.Type == GXMenuPolicies.Edit || item.Type == GXMenuPolicies.Delete ||
                    item.Type == GXConfigurationPolicies.Edit ||
                    item.Type == GXMenuGroupPolicies.Add || item.Type == GXMenuGroupPolicies.Edit || item.Type == GXMenuGroupPolicies.Delete)
                {
                    menu = true;
                }

                if (item.Type == GXMenuGroupPolicies.Add || item.Type == GXMenuGroupPolicies.Edit || item.Type == GXMenuGroupPolicies.Delete)
                {
                    permissions = true;
                }

                if (GXClientAuthorizationCache.IsPermissionChange(item.Type))
                {
                    permissions = true;
                }
            }
            if (appearance || text || menu || permissions)
            {
                Interlocked.Increment(ref _initialResourceGeneration);
            }

            if (appearance || text)
            {
                _ = RefreshChangedResourcesAsync(appearance, text);
            }

            if (menu || permissions)
            {
                InvalidateNavigationMenus();
                if (permissions)
                {
                    Interlocked.Increment(ref _menuGeneration);
                    lock (_changesLock)
                    {
                        _menuPermissionReset = ResetMenuPermissionsAsync();
                    }

                    ClearDisplayedAppearances();
                    _ = RefreshChangedResourcesAsync(true, false);
                }
                _cache.Invalidate("menu");
                if (permissions)
                {
                    ClearDisplayedMenus();
                }

                _ = RefreshMenusAsync();
            }
            if (block || blockPermissions || permissions)
            {
                InvalidateBlocks(blockPermissions || permissions);
                _ = RefreshRequestedBlocksAsync();
            }
            if (view || viewPermissions || permissions)
            {
                InvalidateViews(viewPermissions || permissions);
                _ = RefreshRequestedViewsAsync();
            }
        }

        private async Task RefreshChangedResourcesAsync(bool appearance, bool text)
        {
            try
            {
                string[] images;
                (string Language, string Name)[] texts;
                bool theme;
                bool iconpacks;
                lock (_changesLock)
                {
                    images = [.. _images.Keys];
                    texts = [.. _texts.Keys];
                    theme = _themeRequested;
                    iconpacks = _iconPacksRequested;
                }
                if (appearance)
                {
                    if (theme)
                    {
                        await GetCurrentThemeAsync();
                    }

                    if (iconpacks)
                    {
                        await GetCurrentIconPacksAsync();
                    }

                    await GetImagesAsync(images);
                }
                if (text)
                {
                    foreach (var (Language, Name) in texts)
                    {
                        await GetLocalizedTextAsync(CultureInfo.GetCultureInfo(Language), Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot refresh changed resources.");
            }
        }

        private void MenuAuthenticationChanged(Task<AuthenticationState> state)
        {
            Interlocked.Increment(ref _menuGeneration);
            lock (_changesLock)
            {
                _menuAuthentication = state;
            }
            InvalidateNavigationMenus();

            _cache.Invalidate("menu");
            ClearDisplayedMenus();
            InvalidateViews(true);
            _ = RefreshRequestedViewsAsync();
            InvalidateBlocks(true);
            _ = RefreshRequestedBlocksAsync();
            ClearDisplayedAppearances();
            _ = RefreshChangedResourcesAsync(true, false);
            _ = RefreshMenusAsync();
        }

        private async Task<string> AppearanceCategoryAsync(string category, IGXLocalStorage storage, HttpClient http)
        {
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
            var epoch = await _cache.PermissionEpochAsync(storage, http) ?? "initial";
            return category + ":" + GXClientAuthorizationCache.UserKey(user) + ":" + epoch;
        }

        private void ClearDisplayedAppearances()
        {
            string[] names;
            lock (_changesLock)
            {
                names = [.. _images.Keys];
                foreach (var name in names)
                {
                    _images[name] = null;
                }

                _themeJson = null;
                _iconPackJson = null;
                _initialValues.Clear();
            }
            _cache.Invalidate("image");
            _cache.Invalidate("theme");
            _cache.Invalidate("iconpack");
            OnThemeChanged?.Invoke(this, new GXThemeInfo());
            OnIconPacksChanged?.Invoke(this, []);
            foreach (var name in names)
            {
                OnImageChanged?.Invoke(this, new GXImageChangedArgs { Id = name, Value = null });
            }
        }

        private async Task ResetMenuPermissionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            await _cache.InvalidateMenuPermissionsAsync(storage, http);
        }

        private void ClearDisplayedMenus()
        {
            string[] names;
            lock (_changesLock)
            {
                names = [.. _menus.Keys];
                foreach (var name in names)
                {
                    _menus[name] = null;
                }
            }
            foreach (var name in names)
            {
                MenuChanged?.Invoke(this, new GXMenuChangedEventArgs { Name = name });
            }
        }

        private async Task RefreshMenusAsync()
        {
            try
            {
                string[] names;
                lock (_changesLock)
                {
                    names = [.. _menus.Keys];
                }

                foreach (var name in names)
                {
                    await GetMenuAsync(name);
                }
                await RefreshNavigationMenusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot refresh changed menus.");
            }
        }

        public async Task<GXMenu?> GetMenuAsync(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var generation = Interlocked.Read(ref _menuGeneration);
            Task<AuthenticationState>? authentication;
            Task permissionReset;
            lock (_changesLock)
            {
                _menuAuthentication ??= _menuAuthenticationProvider?.GetAuthenticationStateAsync();
                authentication = _menuAuthentication;
                permissionReset = _menuPermissionReset;
                _menus.TryAdd(name, null);
            }
            await permissionReset;
            var user = authentication == null ? new ClaimsPrincipal(new ClaimsIdentity()) : (await authentication).User;
            using var scope = _serviceProvider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            bool Current() => generation == Interlocked.Read(ref _menuGeneration);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            bool Valid(string body) => JsonSerializer.Deserialize<GetMenuResponse>(body, options)?.Item != null;
            var category = "menu:" + GXClientAuthorizationCache.UserKey(user) + ":" + Uri.EscapeDataString(name);
            var (Found, Value) = await InitialValueAsync(category, "current");
            var body = Found ? Value : await _cache.GetConditionalAsync(storage, http,
                category, "api/Menu/name?name=" + Uri.EscapeDataString(name), Valid, Current, generation == 0);
            if (!Current())
            {
                return null;
            }

            var menu = body == null ? null : JsonSerializer.Deserialize<GetMenuResponse>(body, options)?.Item;
            bool changed;
            lock (_changesLock)
            {
                if (!Current())
                {
                    return null;
                }

                changed = _menus[name] != body;
                _menus[name] = body;
            }
            if (changed)
            {
                MenuChanged?.Invoke(this, new GXMenuChangedEventArgs { Name = name, Menu = menu });
            }
            // Every caller gets a fresh DTO; component changes must not mutate the shared cache.
            return menu;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            lock (_changesLock)
            {
                _navigationMenusDisposed = true;
                ++_navigationMenusGeneration;
                _navigationMenusLoad = null;
                Interlocked.Increment(ref _blockGeneration);
                _viewsDisposed = true;
                Interlocked.Increment(ref _viewGeneration);
            }
            Interlocked.Increment(ref _menuGeneration);
            _initialCancellation?.Cancel();
            if (_menuAuthenticationProvider != null)
            {
                _menuAuthenticationProvider.AuthenticationStateChanged -= MenuAuthenticationChanged;
            }

            _notifier.RemoveListener(this);
        }

        /// <summary>
        /// Get image from the local storage or server.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<string?> GetImageAsync(string name)
        {
            return (await GetImagesAsync([name]))?.SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<List<string?>?> GetImagesAsync(IEnumerable<string> list)
        {
            var values = new List<string?>();
            using var scope = _serviceProvider.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var generation = Interlocked.Read(ref _menuGeneration);
            var resourceGeneration = Interlocked.Read(ref _initialResourceGeneration);
            bool Current() => generation == Interlocked.Read(ref _menuGeneration) && resourceGeneration == Interlocked.Read(ref _initialResourceGeneration);
            var category = await AppearanceCategoryAsync("image", ls, http);
            foreach (var name in list)
            {
                string? value = null;
                try
                {
                    var (Found, Value) = await InitialValueAsync(category, name);
                    value = Found ? Value : await _cache.GetAsync(ls, http, category, name,
                        () => http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Image),
                        async () =>
                        {
                            var response = await http.GetAsJsonAsync<GetAppearanceResponse>("api/Appearance/ByName?type=" + (int)ResourceType.Image + "&name=" + Uri.EscapeDataString(name));
                            var image = response.Item?.Value;
                            return image;
                        }, Current);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot load image {Image}.", name);
                }
                if (!Current())
                {
                    values.Add(null);
                    continue;
                }
                if (value == null && name != "Unknown")
                {
                    value = await GetImageAsync("Unknown");
                }

                bool changed;
                lock (_changesLock)
                {
                    if (!Current())
                    {
                        values.Add(null);
                        continue;
                    }
                    changed = !_images.TryGetValue(name, out var previous) || previous != value;
                    _images[name] = value;
                }
                if (changed && Current())
                {
                    OnImageChanged?.Invoke(this, new GXImageChangedArgs { Id = name, Value = value });
                }

                values.Add(value);
            }
            return values;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<KeyValuePair<int, string>>?> RefreshEnumTypesAsync(string type)
        {
            ListEnumTypes req = new()
            {
                Filter = new GXEnumType()
                {
                    Type = type
                }
            };
            List<KeyValuePair<int, string>> list = [];
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var ret = await http.PostAsJson<ListEnumTypesResponse>("api/EnumType/List", req);
                if (ret.Types != null)
                {
                    foreach (var it in ret.Types)
                    {
                        if (it.Name != null)
                        {
                            int id = GXEnumTypeKeys.GetKey(type, it);
                            list.Add(new KeyValuePair<int, string>(id, GXEnumTypeKeys.GetDisplayName(type, it.Name)!));
                        }
                    }
                }
            }
            UpdateEnumTypes(type, list);
            return list;
        }

        /// <inheritdoc/>
        public async Task<string?> GetEnumTypeAsync(string type, int id)
        {
            return (await GetEnumTypesAsync(type, [id]))?.SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<List<string?>?> GetEnumTypesAsync(string type, IEnumerable<int> ids)
        {
            List<string?> values = [];
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ListEnumTypes req = new()
                {
                    Filter = new GXEnumType()
                    {
                        Type = type
                    }
                };
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var cacheGroup = "ami-resource:v2:" + Uri.EscapeDataString(http.BaseAddress?.AbsoluteUri ?? "") + ":enum:" + type;
                foreach (var id in ids)
                {
                    string? value;
                    value = await ls.GetValueAsync(cacheGroup, id.ToString());
                    if (value == null)
                    {
                        try
                        {
                            req.Filter.Id = id;
                            var ret = await http.PostAsJson<ListEnumTypesResponse>("api/EnumType/List", req);
                            value = ret.Types?.FirstOrDefault()?.Name;
                            if (value != null)
                            {
                                //Save value to local storage.
                                await ls.SetValueAsync(cacheGroup, id.ToString(), value);
                            }
                        }
                        catch (Exception)
                        {
                            value = null;
                        }
                    }
                    //Null values are also added to the list.
                    values.Add(GXEnumTypeKeys.GetDisplayName(type, value));
                }
            }
            if (OnEnumTypesChanged.HasDelegate)
            {
                await OnEnumTypesChanged.InvokeAsync();
            }
            return values;
        }

        /// <inheritdoc/>
        public string? GetEnumType(string type, int? id)
        {
            string? ret = null;

            _ = type + id;
            lock (_enumTypes)
            {
                var tp = _enumTypes.GetValueOrDefault(type);
                if (tp != null && id != null)
                {
                    ret = tp.GetValueOrDefault(id.Value);
                }
            }
            ret ??= "Unknown";
            return GXEnumTypeKeys.GetDisplayName(type, ret);
        }

        /// <inheritdoc/>
        public string? GetEnumType(string type, Enum value)
        {
            return GetEnumType(type, Convert.ToInt32(value));
        }


        /// <inheritdoc/>
        public void UpdateEnumTypes(string type, IEnumerable<KeyValuePair<int, string>> values)
        {
            lock (_enumTypes)
            {
                _enumTypes.Remove(type);
                var tp = _enumTypes.GetValueOrDefault(type);
                tp ??= _enumTypes[type] = [];
                foreach (var it in values)
                {
                    tp[it.Key] = it.Value;
                }
            }
            if (OnEnumTypesChanged.HasDelegate)
            {
                OnEnumTypesChanged.InvokeAsync();
            }
        }

        /// <inheritdoc/>
        public void ClearEnumTypes(string type)
        {
            lock (_enumTypes)
            {
                _enumTypes.Remove(type);
            }
            if (OnEnumTypesChanged.HasDelegate)
            {
                OnEnumTypesChanged.InvokeAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetLocalizedTextAsync(string name)
        {
            await InitialCultureAsync();
            return (await GetLocalizedTextsAsync(_resourceCulture ?? CultureInfo.CurrentUICulture, [name]))[0];
        }

        /// <inheritdoc/>
        public async Task<string> GetLocalizedTextAsync(CultureInfo cultureInfo, string name)
        {
            return (await GetLocalizedTextsAsync(cultureInfo, [name]))[0];
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetLocalizedTextsAsync(IEnumerable<string> names)
        {
            await InitialCultureAsync();
            return await GetLocalizedTextsAsync(_resourceCulture ?? CultureInfo.CurrentUICulture, names);
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetLocalizedTextsAsync(CultureInfo cultureInfo, IEnumerable<string> names)
        {
            var values = new List<string>();
            using var scope = _serviceProvider.CreateScope();
            var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            string lang = cultureInfo.TwoLetterISOLanguageName;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    values.Add("");
                    continue;
                }
                string? value = null;
                try
                {
                    string hash = ClientHelpers.GetHashCode(name);
                    var (Found, Value) = await InitialValueAsync("text:" + lang, hash);
                    value = Found ? Value : await _cache.GetAsync(ls, http, "text:" + lang, hash,
                        () => http.PostAsJson<DateTimeOffset>("api/LocalizedResource/LastChanged", null),
                        async () =>
                        {
                            var response = await http.GetAsJsonAsync<GetLocalizedResourceResponse>("api/LocalizedResource?lang=" + Uri.EscapeDataString(lang) + "&hash=" + Uri.EscapeDataString(hash) + "&text=" + Uri.EscapeDataString(name));
                            return response.Item?.Value ?? name;
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot load localized resource {Language}.", lang);
                }
                value ??= name;
                values.Add(value);
                string previous;
                lock (_changesLock)
                {
                    previous = _texts.GetValueOrDefault((lang, name)) ?? name;
                    _texts[(lang, name)] = value;
                }
                if (value != previous)
                {
                    OnTextChanged?.Invoke(this, new GXTextChangedEventArgs { PreviousText = previous, CurrentText = value });
                }
            }
            return values;
        }

        /// <inheritdoc/>
        public async Task<GXThemeInfo?> GetCurrentThemeAsync()
        {
            lock (_changesLock)
            {
                _themeRequested = true;
            }

            using var scope = _serviceProvider.CreateScope();
            var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            try
            {
                var generation = Interlocked.Read(ref _menuGeneration);
                var resourceGeneration = Interlocked.Read(ref _initialResourceGeneration);
                bool Current() => generation == Interlocked.Read(ref _menuGeneration) && resourceGeneration == Interlocked.Read(ref _initialResourceGeneration);
                var category = await AppearanceCategoryAsync("theme", ls, http);
                var (Found, Value) = await InitialValueAsync(category, "current");
                var value = Found ? Value : await _cache.GetAsync(ls, http, category, "current",
                    () => http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Theme),
                    async () =>
                    {
                        var response = await http.PostAsJson<ListAppearancesResponse>("api/Appearance/List", new ListAppearances
                        {
                            Filter = new GXAppearance { ResourceType = (byte)ResourceType.Theme, Enabled = true }
                        });
                        var theme = response.Appearances?.OrderByDescending(a => a.Priority).ThenBy(a => a.Name, StringComparer.Ordinal).FirstOrDefault()?.Value;
                        // Reject malformed payloads before persisting them.
                        if (theme != null)
                        {
                            JsonSerializer.Deserialize<GXThemeInfo>(theme);
                        }

                        return theme;
                    }, Current);
                if (!Current())
                {
                    return null;
                }

                var theme = value == null ? null : JsonSerializer.Deserialize<GXThemeInfo>(value);
                bool changed;
                lock (_changesLock)
                {
                    if (!Current())
                    {
                        return null;
                    }

                    changed = _themeRequested && _themeJson != value;
                    _themeRequested = true;
                    _themeJson = value;
                }
                if (changed && Current())
                {
                    OnThemeChanged?.Invoke(this, theme ?? new GXThemeInfo());
                }

                return theme;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot load current theme.");
                return null;
            }
        }

        public async Task<IEnumerable<string>?> GetCurrentIconPacksAsync()
        {
            lock (_changesLock)
            {
                _iconPacksRequested = true;
            }

            using var scope = _serviceProvider.CreateScope();
            var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            try
            {
                var generation = Interlocked.Read(ref _menuGeneration);
                var resourceGeneration = Interlocked.Read(ref _initialResourceGeneration);
                bool Current() => generation == Interlocked.Read(ref _menuGeneration) && resourceGeneration == Interlocked.Read(ref _initialResourceGeneration);
                var category = await AppearanceCategoryAsync("iconpack", ls, http);
                var (Found, Value) = await InitialValueAsync(category, "current");
                var value = Found ? Value : await _cache.GetAsync(ls, http, category, "current",
                    () => http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Iconpack),
                    async () =>
                    {
                        var response = await http.PostAsJson<ListAppearancesResponse>("api/Appearance/List", new ListAppearances
                        {
                            Filter = new GXAppearance { ResourceType = (byte)ResourceType.Iconpack, Enabled = true }
                        });
                        return JsonSerializer.Serialize(response.Appearances?.Select(a => a.Value).Where(v => !string.IsNullOrEmpty(v)) ?? []);
                    }, Current);
                if (!Current())
                {
                    return null;
                }

                var packs = value == null ? [] : JsonSerializer.Deserialize<string[]>(value) ?? [];
                bool changed;
                lock (_changesLock)
                {
                    if (!Current())
                    {
                        return null;
                    }

                    changed = _iconPackJson != value;
                    _iconPackJson = value;
                }
                if (changed && Current())
                {
                    OnIconPacksChanged?.Invoke(this, packs);
                }

                return packs;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot load current icon packs.");
                return null;
            }
        }

        /// <summary>
        /// Determines whether the current instance uses local or UTC time.
        /// </summary>
        /// <returns><see langword="true"/> if the current instance contains a local time; otherwise, UTC time is used.</returns>
        /// <seealso cref="DateTimeOffsetToString"/>
        /// <seealso cref="SetLocalDateTimeAsync"/>
        public async Task<bool> HasLocalDateTimeAsync()
        {
            if (_utcTime == null)
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                var value = await ls.GetValueAsync("user", "UtcTime");
                _utcTime = Convert.ToBoolean(value);
            }
            return _utcTime != true;
        }

        /// <inheritdoc/>
        public async Task SetLocalDateTimeAsync(bool value)
        {
            _utcTime = value;
            using IServiceScope scope = _serviceProvider.CreateScope();
            IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            await ls.SetValueAsync("user", "UtcTime", value.ToString());
        }
        /// <inheritdoc/>
        public string? DateTimeOffsetToString(DateTimeOffset? value)
        {
            if (_utcTime == true)
            {
                return value?.ToString(_formatCulture ?? CultureInfo.CurrentCulture);
            }
            return value?.LocalDateTime.ToString(_formatCulture ?? CultureInfo.CurrentCulture);
        }

        public async Task<string?> GetUserLanguageAsync(CancellationToken cancellationToken = default)
        {
            await InitialCultureAsync();
            using var scope = _serviceProvider.CreateScope();
            var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
            var value = await ls.GetValueAsync("user", "language");
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            try
            {
                var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                using var response = await http.GetAsync("api/Localization", cancellationToken);
                await response.EnsureSuccessAsync(cancellationToken);
                value = await response.Content.ReadAsStringAsync(cancellationToken);
                await SetUserLanguageAsync(value, cancellationToken);
                return value;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot load user language.");
                return "en";
            }
        }

        public async Task SetUserLanguageAsync(string? value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(value))
            {
                CultureInfo.GetCultureInfo(value);
            }

            Interlocked.Increment(ref _initialResourceGeneration);
            await _languageGate.WaitAsync(cancellationToken);
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                var previous = await ls.GetValueAsync("user", "language");
                if (value == null)
                {
                    await ls.RemoveAsync("user", "language");
                }
                else
                {
                    await ls.SetValueAsync("user", "language", value);
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    await ls.SetValueAsync("user", "culture", CultureInfo.GetCultureInfo(value).Name);
                    SetInitialCulture(value);
                }
                if (previous != value)
                {
                    OnLanguageChanged?.Invoke(this, value);
                }
            }
            finally
            {
                _languageGate.Release();
            }
        }
    }
}
