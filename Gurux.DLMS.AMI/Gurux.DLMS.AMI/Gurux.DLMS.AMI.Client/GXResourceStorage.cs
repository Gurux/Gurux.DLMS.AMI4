//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Pages.Config;
using Gurux.DLMS.AMI.Client.Properties;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// Resource service is used to get resource from the cache or server if it's not loaded yet.
    /// </summary>
    public class GXResourceStorage : IGXResourceStorage
    {
        private bool? _utcTime;
        private readonly IServiceProvider _serviceProvider;
        private DateTimeOffset? _appereanceLastChanged;
        private DateTimeOffset? _resourceLastChanged;
        private DateTimeOffset? _iconpacksLastChanged;
        /// <inheritdoc/>
        public EventCallback OnEnumTypesChanged { get; set; }

        /// <inheritdoc/>
        public event EventHandler<GXTextChangedEventArgs> OnTextChanged;

        /// <inheritdoc/>
        public event EventHandler<GXThemeInfo> OnThemeChanged;
        /// <inheritdoc/>
        public event EventHandler<GXImageChangedArgs>? OnImageChanged;

        /// <summary>
        /// Cached enum types.
        /// </summary>
        private readonly Dictionary<string, Dictionary<int, string>> _enumTypes = new Dictionary<string, Dictionary<int, string>>();

        /// <summary>
        /// Cached values.
        /// </summary>
        private readonly Dictionary<string, string?> _cached = new Dictionary<string, string?>();
        private readonly ILogger _logger;
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXResourceStorage(IServiceProvider serviceProvider,
            IGXNotifier notifier)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger(nameof(GXResourceStorage));

            _serviceProvider = serviceProvider;
            notifier.On<IEnumerable<GXAppearance>>(this, nameof(IGXHubEvents.AppearanceUpdate), async (appearance) =>
            {
                foreach (var it in appearance)
                {
                    if (it.ResourceType == (byte)ResourceType.Theme && it.Active == true)
                    {
                        _logger.LogInformation("Theme changed.");
                        //Read the new theme on change.
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                            await ls.RemoveAsync("theme:LastChanged");
                            await ls.RemoveAsync("theme");
                        }
                        var theme = await GetCurrentThemeAsync();
                        if (theme != null)
                        {
                            OnThemeChanged?.Invoke(null, theme);
                        }
                    }
                    if (it.ResourceType == (byte)ResourceType.Image)
                    {
                        _logger.LogInformation("Image changed {0}.", it.Id);
                        _cached.Remove("image:" + it.Id);
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                            await ls.RemoveAsync("image:" + it.Id);
                        }
                        if (it.Value == null)
                        {
                            it.Value = await GetImageAsync(it.Id);
                        }
                        //Notify that the image has changed.
                        OnImageChanged?.Invoke(null, new GXImageChangedArgs()
                        {
                            Id = it.Id,
                            Value = it.Value
                        });
                    }
                }
                lock (_enumTypes)
                {
                    _enumTypes.Clear();
                }
            });
            notifier.On<IEnumerable<GXAppearance>>(this, nameof(IGXHubEvents.AppearanceDelete), (appearance) =>
            {
                foreach (var it in appearance)
                {
                    _logger.LogInformation("Appearance delete {0}.", it.Id);
                }
                lock (_enumTypes)
                {
                    foreach (var it in appearance)
                    {
                        _cached.Remove(it.Id);
                    }
                    _enumTypes.Clear();
                }
            });
            notifier.On<IEnumerable<GXLocalizedResource>>(this, nameof(IGXHubEvents.LocalizedResourceUpdate), async (resources) =>
            {
                //Notify that the text changed.
                if (OnTextChanged != null)
                {
                    string lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                        foreach (var resource in resources)
                        {
                            if (resource.Hash != null)
                            {
                                if (_cached.TryGetValue(lang + resource.Hash, out string? value))
                                {
                                    _logger.LogInformation("{0} Text changed from {1} to {2}.", lang, value, resource.Value);
                                    _cached[lang + resource.Hash] = resource.Value;
                                    //Update new value to the local storage.
                                    await ls.SetValueAsync(lang, resource.Hash, resource.Value);
                                    OnTextChanged.Invoke(null, new GXTextChangedEventArgs()
                                    {
                                        PreviousText = value,
                                        CurrentText = resource.Value,
                                    });
                                }
                            }
                        }
                    }
                }
            });
            notifier.On<IEnumerable<GXLocalizedResource>>(this, nameof(IGXHubEvents.LocalizedResourceDelete), async (resources) =>
            {
                await RemoveResources(resources);
            });
            _ = HasLocalDateTimeAsync();
        }

        private async Task RemoveResources(IEnumerable<GXLocalizedResource> resources)
        {
            try
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                    if (resources == null)
                    {
                        _logger.LogWarning("Clear All resources.");
                        await ls.ClearAsync();
                    }
                    else
                    {
                        foreach (var resource in resources)
                        {
                            if (string.IsNullOrEmpty(resource.Language?.Id))
                            {
                                //Clear all resources.
                                _logger.LogWarning("Clear All resources.");
                                await ls.ClearAsync();
                                break;
                            }
                            else
                            {
                                _logger.LogWarning("Remove resource.", resource.Language.Id);
                                await ls.RemoveAsync(resource.Language.Id, resource.Hash);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Remove resources failed {0}.", ex);
            }
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
            List<string?> values = new List<string?>();
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                var ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                try
                {
                    //Check are the appearances changed.
                    if (_appereanceLastChanged == null)
                    {
                        _appereanceLastChanged = DateTimeOffset.MinValue;
                        string? str = await ls.GetValueAsync("image", "LastChanged");
                        if (!string.IsNullOrEmpty(str))
                        {
                            try
                            {
                                _appereanceLastChanged = DateTimeOffset.Parse(str);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        DateTimeOffset? changed = await http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Image);
                        if (_appereanceLastChanged == DateTimeOffset.MinValue || changed > _appereanceLastChanged)
                        {
                            _appereanceLastChanged = changed;
                            await ls.ClearAsync();
                            //Save value to local storage.
                            await ls.SetValueAsync("image", "LastChanged", _appereanceLastChanged.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("GetImagesAsync failed {0}.", ex);
                }
                foreach (var name in list)
                {
                    string? value;
                    if (!_cached.TryGetValue("image:" + name, out value))
                    {
                        //Save value so it's not ask twice.
                        _cached["image:" + name] = null;
                        value = await ls.GetValueAsync("image", name);
                        if (value == null)
                        {
                            try
                            {
                                value = (await http.GetAsJsonAsync<GetAppearanceResponse>("api/Appearance?type=" + (int)ResourceType.Image + "&id=" + name))?.Item?.Value;
                                if (value != null)
                                {
                                    //Save value to local storage.
                                    await ls.SetValueAsync("image:", name, value);
                                    bool changed = _cached["image:" + name] == null;
                                    _cached["image:" + name] = value;
                                    if (changed && OnImageChanged != null)
                                    {
                                        //Notify that the image has changed.
                                        OnImageChanged.Invoke(null, new GXImageChangedArgs()
                                        {
                                            Id = name,
                                            Value = value
                                        });
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                if (name == "Unknown")
                                {
                                    value = null;
                                }
                                else
                                {
                                    value = await GetImageAsync("Unknown");
                                }
                            }
                        }
                    }
                    //Null values are also added to the list.
                    values.Add(value);
                }
            }
            return values;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<KeyValuePair<int, string>>?> RefreshEnumTypesAsync(string type)
        {
            ListEnumTypes req = new ListEnumTypes()
            {
                Filter = new GXEnumType()
                {
                    Type = type
                }
            };
            List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
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
                            int id = it.Value != null ? it.Value.Value : it.Id;
                            list.Add(new KeyValuePair<int, string>(id, it.Name));
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
            List<string?> values = new List<string?>();
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ListEnumTypes req = new ListEnumTypes()
                {
                    Filter = new GXEnumType()
                    {
                        Type = type
                    }
                };
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                foreach (var id in ids)
                {
                    string? value;
                    value = await ls.GetValueAsync(type, id.ToString());
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
                                await ls.SetValueAsync(type, id.ToString(), value);
                            }
                        }
                        catch (Exception)
                        {
                            value = null;
                        }
                    }
                    //Null values are also added to the list.
                    values.Add(value);
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
            string name = type + id;
            lock (_enumTypes)
            {
                var tp = _enumTypes.GetValueOrDefault(type);
                if (tp != null && id != null)
                {
                    ret = tp.GetValueOrDefault(id.Value);
                }
            }
            if (ret == null)
            {
                ret = "Unknown";
            }
            return ret;
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
                if (tp == null)
                {
                    tp = _enumTypes[type] = new Dictionary<int, string>();
                }
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
            return (await GetLocalizedTextsAsync(Thread.CurrentThread.CurrentUICulture, [name]))[0];
        }

        /// <inheritdoc/>
        public async Task<string> GetLocalizedTextAsync(CultureInfo cultureInfo, string name)
        {
            return (await GetLocalizedTextsAsync(cultureInfo, [name]))[0];
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetLocalizedTextsAsync(IEnumerable<string> names)
        {
            return await GetLocalizedTextsAsync(Thread.CurrentThread.CurrentUICulture, names);
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetLocalizedTextsAsync(CultureInfo cultureInfo, IEnumerable<string> names)
        {
            List<string> values = new List<string>();
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                //Ckeck are localized resources changed.
                if (_resourceLastChanged == null)
                {
                    _resourceLastChanged = DateTimeOffset.MinValue;
                    string? str = await ls.GetValueAsync("resource", "LastChanged");
                    if (!string.IsNullOrEmpty(str))
                    {
                        try
                        {
                            _resourceLastChanged = DateTimeOffset.Parse(str);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    DateTimeOffset? tmp = await http.PostAsJson<DateTimeOffset>("api/LocalizedResource/LastChanged", null);
                    if (_resourceLastChanged == DateTimeOffset.MinValue || tmp > _resourceLastChanged)
                    {
                        _logger.LogInformation("Appereance updated.");
                        _appereanceLastChanged = tmp;
                        await ls.ClearAsync();
                        //Save value to local storage.
                        await ls.SetValueAsync("resource", "LastChanged", _resourceLastChanged.ToString());
                    }
                }
                string lang = cultureInfo.TwoLetterISOLanguageName;
                foreach (var name in names)
                {
                    string? value = null;
                    if (!string.IsNullOrEmpty(name))
                    {
                        string hash = ClientHelpers.GetHashCode(name);
                        //Save value so it's not ask twice.
                        if (!_cached.TryGetValue(lang + hash, out value))
                        {
                            _cached[lang + hash] = name;
                            value = await ls.GetValueAsync(lang, hash);
                            if (string.IsNullOrEmpty(value))
                            {
                                try
                                {
                                    var ret = await http.GetAsJsonAsync<GetLocalizedResourceResponse>("api/LocalizedResource?lang=" + lang + "&hash=" + hash);
                                    value = ret.Item?.Value;
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        _logger.LogInformation("New text added {0}.", name);
                                        //Update text if unknown resource.
                                        ret = await http.GetAsJsonAsync<GetLocalizedResourceResponse>("api/LocalizedResource?lang=" + lang + "&hash=" + hash + "&text=" + name);
                                        value = name;
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Text updated from {0} to {1}.", name, value);
                                        //Notify that the text has changed.
                                        OnTextChanged.Invoke(null, new GXTextChangedEventArgs()
                                        {
                                            PreviousText = name,
                                            CurrentText = value,
                                        });
                                    }
                                    _logger.LogInformation("Server value: {0}: {1}.", name, value);
                                    //Save localized value to local storage.
                                    await ls.SetValueAsync(lang, hash, value);
                                }
                                catch (Exception)
                                {
                                    await ls.RemoveAsync(lang, hash);
                                    value = name;
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Local {0} value: {1}: {2}.", lang, name, value);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Cached {0} value: {1}: {2}.", lang, name, value);
                        }
                        if (!string.IsNullOrEmpty(value))
                        {
                            _cached[lang + hash] = value;
                        }
                    }
                    //Null values are also added to the list.
                    if (value == null)
                    {
                        values.Add("");
                    }
                    else
                    {
                        values.Add(value);
                    }
                }
            }
            return values;
        }

        /// <inheritdoc/>
        public async Task<GXThemeInfo?> GetCurrentThemeAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                try
                {
                    //Check are the appearances changed.
                    if (_appereanceLastChanged == null)
                    {
                        _appereanceLastChanged = DateTimeOffset.MinValue;
                        string? str = await ls.GetValueAsync("theme", "LastChanged");
                        if (!string.IsNullOrEmpty(str))
                        {
                            try
                            {
                                _appereanceLastChanged = DateTimeOffset.Parse(str);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        DateTimeOffset? changed = await http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Theme);
                        if (_appereanceLastChanged == DateTimeOffset.MinValue || changed > _appereanceLastChanged)
                        {
                            _appereanceLastChanged = changed;
                            await ls.ClearAsync();
                            //Save value to local storage.
                            await ls.SetValueAsync("theme", "LastChanged", _appereanceLastChanged.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                string? value = await ls.GetValueAsync("theme", "current");
                if (value == null)
                {
                    try
                    {
                        ListAppearances req = new ListAppearances()
                        {
                            Filter = new GXAppearance()
                            {
                                ResourceType = (byte)ResourceType.Theme,
                                Active = true,
                            }
                        };
                        var ret = await http.PostAsJson<ListAppearancesResponse>("api/Appearance/List", req);
                        if (ret.Appearances?.Length == 1)
                        {
                            value = ret.Appearances[0].Value;
                            //Save value to local storage.
                            await ls.SetValueAsync("theme", "current", value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetCurrentThemeAsync failed {0}.", ex);
                    }
                }
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        return JsonSerializer.Deserialize<GXThemeInfo>(value);
                    }
                    catch (Exception)
                    {
                        //Return default value if serialization fails.
                    }
                }
            }
            return null;
        }

        public async Task UpdateLocalizationAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                string? language = await ls.GetValueAsync("language", "current");
                if (string.IsNullOrEmpty(language))
                {
                    HttpResponseMessage response = await http.GetAsync("api/Localization/");
                    await AMI.Shared.Helpers.ValidateStatusCode(response, default);
                    language = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(language))
                    {
                        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger(nameof(GXResourceStorage));
                        logger.LogInformation("New language is " + language);
                        CultureInfo culture = new CultureInfo(language);
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
                        /*TODO:
                    if (LoginDisplay != null)
                    {
                        LoginDisplay.LanguageUpdated();
                    }
                    if (NavMenu != null)
                    {
                        NavMenu.LanguageUpdated();
                    }
                        */
                    }
                }
            }
        }

        public async Task<IEnumerable<string>?> GetCurrentIconPacksAsync()
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                HttpClient? http = scope.ServiceProvider.GetRequiredService<HttpClient>();
                try
                {
                    //Check are the appearances changed.
                    if (_iconpacksLastChanged == null)
                    {
                        _iconpacksLastChanged = DateTimeOffset.MinValue;
                        string? str = await ls.GetValueAsync("iconpack", "LastChanged");
                        if (!string.IsNullOrEmpty(str))
                        {
                            try
                            {
                                _iconpacksLastChanged = DateTimeOffset.Parse(str);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        DateTimeOffset? changed = await http.PostAsJson<DateTimeOffset>("api/Appearance/LastChanged", (byte)ResourceType.Iconpack);
                        if (_iconpacksLastChanged == DateTimeOffset.MinValue || changed > _iconpacksLastChanged)
                        {
                            _iconpacksLastChanged = changed;
                            await ls.ClearAsync();
                            //Save value to local storage.
                            await ls.SetValueAsync("iconpack", "LastChanged", _iconpacksLastChanged.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("GetCurrentIconPacksAsync failed {0}.", ex);
                }
                string? value = await ls.GetValueAsync("iconpack", "current");
                if (value == null)
                {
                    try
                    {
                        ListAppearances req = new ListAppearances()
                        {
                            Filter = new GXAppearance()
                            {
                                ResourceType = (byte)ResourceType.Iconpack,
                                Active = true,
                            }
                        };
                        var ret = await http.PostAsJson<ListAppearancesResponse>("api/Appearance/List", req);
                        if (ret.Appearances != null)
                        {
                            value = string.Join(';', ret.Appearances.Select(s => s.Value));
                        }
                        else
                        {
                            value = null;
                        }
                        //Save value to local storage.
                        await ls.SetValueAsync("iconpack", "current", value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetCurrentIconPacksAsync failed {0}.", ex);
                    }
                }
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        return value.Split(';');
                    }
                    catch (Exception)
                    {
                        //Return default value if serialization fails.
                    }
                }
            }
            return null;
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
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                    var value = await ls.GetValueAsync("user", "UtcTime");
                    _utcTime = Convert.ToBoolean(value);
                }
            }
            return _utcTime != true;
        }

        /// <inheritdoc/>
        public async Task SetLocalDateTimeAsync(bool value)
        {
            _utcTime = value;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXLocalStorage? ls = scope.ServiceProvider.GetRequiredService<IGXLocalStorage>();
                await ls.SetValueAsync("user", "UtcTime", value.ToString());
            }
        }
        /// <inheritdoc/>
        public string? DateTimeOffsetToString(DateTimeOffset? value)
        {
            if (_utcTime == true)
            {
                return value?.ToString();
            }
            return value?.LocalDateTime.ToString();
        }
    }
}