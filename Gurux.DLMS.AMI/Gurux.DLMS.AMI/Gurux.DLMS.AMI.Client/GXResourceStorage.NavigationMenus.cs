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

using System.Net.Http.Json;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Client;

public partial class GXResourceStorage
{
    private Task<string>? _navigationMenusLoad;
    private long _navigationMenusGeneration;
    private bool _navigationMenusRequested;
    private bool _navigationMenusDisposed;

    /// <inheritdoc />
    public event EventHandler? NavigationMenusChanged;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GXMenu>> GetNavigationMenusAsync()
    {
        Task<string> load;
        long generation;
        lock (_changesLock)
        {
            if (_navigationMenusDisposed)
            {
                return [];
            }

            _navigationMenusRequested = true;
            generation = _navigationMenusGeneration;
            load = _navigationMenusLoad ??= LoadNavigationMenusAsync();
        }
        string body;
        try
        {
            body = await load;
        }
        catch
        {
            lock (_changesLock)
            {
                if (ReferenceEquals(load, _navigationMenusLoad))
                {
                    _navigationMenusLoad = null;
                }
            }
        
            throw;
        }
        var menus = JsonSerializer.Deserialize<GetMenuResponse[]>(body, JsonSerializerOptions.Web) ?? [];
        lock (_changesLock)
        {
            if (_navigationMenusDisposed || generation != _navigationMenusGeneration)
            {
                return [];
            }
        }
        // Keep each consumer's DTOs separate from the shared representation.
        return menus.Where(m => m.Item != null).Select(m => m.Item!).ToArray();
    }

    private async Task<string> LoadNavigationMenusAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HttpClient>().GetStringAsync("api/Menu/Navigation");
    }

    private void InvalidateNavigationMenus()
    {
        lock (_changesLock)
        {
            ++_navigationMenusGeneration;
            _navigationMenusLoad = null;
        }
        NavigationMenusChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RefreshNavigationMenusAsync()
    {
        lock (_changesLock)
        {
            if (!_navigationMenusRequested || _navigationMenusDisposed)
            {
                return;
            }
        }
        try
        {
            await GetNavigationMenusAsync();
            NavigationMenusChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot refresh placed navigation menus.");
        }
    }
}
