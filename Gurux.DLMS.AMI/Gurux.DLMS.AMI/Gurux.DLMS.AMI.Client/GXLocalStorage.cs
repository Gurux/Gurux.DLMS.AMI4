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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text;

/// <summary>
/// Cookie storage is used to save user cookies.
/// </summary>
public class GXLocalStorage : IGXLocalStorage
{
    private readonly IJSRuntime? _jsRuntime;
    private readonly ILogger<GXLocalStorage>? _logger;

    public GXLocalStorage(IJSRuntime? jsRuntime, 
        IGXNotifier notifier,
        ILogger<GXLocalStorage>? logger)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
        if (_jsRuntime?.GetType().Name == "UnsupportedJavaScriptRuntime")
        {
            _jsRuntime = null;
        }

        notifier.On<IEnumerable<GXAppearance>>(this, nameof(IGXHubEvents.AppearanceUpdate), async (appearance) =>
    {
        foreach (var it in appearance)
        {
            if (string.IsNullOrEmpty(it.Category))
            {
                await RemoveAsync(it.Id);
            }
            else
            {
                await RemoveAsync(it.Category, it.Id);
            }
        }
    });
        notifier.On<IEnumerable<GXAppearance>>(this, nameof(IGXHubEvents.AppearanceDelete), async (appearance) =>
        {
            if (appearance == null)
            {
                await ClearAsync();
            }
        });
    }

    public Task SetValueAsync(string key, string? value) => SetValueAsync("", key, value);

    public async Task SetValueAsync(string group, string key, string? value)
    {
        _logger?.LogDebug("Set local storage value, key: {Key}, value: {Value}", key, value);
        //_jsRuntime is null on the server side.
        if (_jsRuntime != null)
        {
            string tmp;
            if (!string.IsNullOrEmpty(group))
            {
                key = group + ":" + key;
            }
            if (value != null)
            {
                tmp = "localStorage.setItem(\"" + key + "\", \"" + Convert.ToBase64String(ASCIIEncoding.Unicode.GetBytes(value)) + "\")";
            }
            else
            {
                tmp = "localStorage.setItem(\"" + key + "\", \"" + value + "\")";
            }
            await _jsRuntime.InvokeVoidAsync("eval", tmp);
        }
    }

    public Task<string?> GetValueAsync(string key) => GetValueAsync("", key);

    public async Task<string?> GetValueAsync(string group, string key)
    {
        try
        {
            string? value = null;
            if (_jsRuntime != null)
            {
                if (!string.IsNullOrEmpty(group))
                {
                    key = group + ":" + key;
                }
                string tmp = "localStorage.getItem(\"" + key + "\")";
                value = await _jsRuntime.InvokeAsync<string>("eval", tmp);
                if (string.IsNullOrEmpty(value) || value == "null")
                {
                    value = null;
                }
                if (value != null)
                {
                    try
                    {
                        value = ASCIIEncoding.Unicode.GetString(Convert.FromBase64String(value));
                    }
                    catch (Exception)
                    {
                        value = null;
                    }
                }
                else
                {
                    value = null;
                }
            }
            _logger?.LogDebug("Get local storage value, key: {Key}, value: {Value}", key, value);
            return value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting local storage value, key: {Key}", key);
        }
        return null;
    }

    public Task RemoveAsync(string key) => RemoveAsync("", key);

    public async Task RemoveAsync(string group, string key)
    {
        if (_jsRuntime != null)
        {
            string tmp = "localStorage.removeItem(\"" + key + "\")";
            await _jsRuntime.InvokeVoidAsync("eval", tmp);
        }
    }

    public Task ClearAsync() => ClearAsync("");

    public async Task ClearAsync(string? group)
    {
        if (_jsRuntime != null)
        {
            if (string.IsNullOrEmpty(group))
            {
                await _jsRuntime.InvokeVoidAsync("eval", "localStorage.clear()");
            }
            else
            {
                string tmp = "Object.keys(localStorage).filter(k => k.startsWith(\"" + group + ":\"))";
                string[]? values = await _jsRuntime.InvokeAsync<string[]>("eval", tmp);
                foreach (var it in values)
                {
                    tmp = "localStorage.removeItem(\"" + it + "\")";
                    await _jsRuntime.InvokeVoidAsync("eval", tmp);
                }
            }
        }
    }
}
