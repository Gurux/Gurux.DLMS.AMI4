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

using Gurux.DLMS.AMI.Components;
using Microsoft.JSInterop;

/// <summary>
/// Cookie storage is used to save user cookies.
/// </summary>
public class GXCookieStorage : IGXCookieStorage
{
    private Lazy<IJSObjectReference> _accessorJsRef = new();
    private readonly IJSRuntime _jsRuntime;

    public GXCookieStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    private async Task SetCookie(string value)
    {
        await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = \"{value}\"");
    }

    private async Task<string> GetCookie()
    {
        return await _jsRuntime.InvokeAsync<string>("eval", $"document.cookie");
    }

    public async Task SetValueAsync(
        string key,
        string? value,
        int days)
    {
        var curExp = days > 0 ? DateToUTC(days) : "";
        await SetCookie($"{key}={value}; expires={curExp}; path=/");
    }

    public async Task<string?> GetValueAsync(
        string key,
        string? def)
    {
        var value = await GetCookie();
        if (string.IsNullOrEmpty(value))
        {
            return def;
        }
        var vals = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var val in vals)
        {
            int pos = val.IndexOf('=');
            if (pos != -1)
            {
                if (string.Compare(val.Substring(0, pos).Trim(), key, true) == 0)
                {
                    return val.Substring(1 + pos);
                }
            }
        }
        return def;
    }

    /// <summary>
    /// Convert expiration date to UTC time.
    /// </summary>
    /// <param name="days">Days before expiration.</param>
    /// <returns></returns>
    private static string DateToUTC(int days) =>
        DateTime.Now.AddDays(days).ToUniversalTime().ToString("R");

    public async ValueTask DisposeAsync()
    {
        if (_accessorJsRef.IsValueCreated)
        {
            await _accessorJsRef.Value.DisposeAsync();
        }
    }
}
