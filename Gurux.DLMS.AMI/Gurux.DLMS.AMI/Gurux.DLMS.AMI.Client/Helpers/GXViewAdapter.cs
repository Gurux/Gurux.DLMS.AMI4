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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Internal;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Reads shared table defaults using the AMI view contract.</summary>
/// <param name="http">HTTP transport used to access the AMI API.</param>
/// <param name="storage">Optional shared view storage used to read named defaults and refresh them after saving.</param>
public sealed class GXViewAdapter(HttpClient http, IAmiViewStorage? storage = null)
{
    /// <summary>Web JSON settings shared by view reads and writes.</summary>
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the view with the specified identifier.</summary>
    /// <param name="id">Identifier of the view to read.</param>
    /// <returns>The requested view, or null when it does not exist.</returns>
    public Task<GXView?> ReadAsync(Guid id) => ReadUrlAsync($"api/View?id={id}");

    /// <summary>Reads the named table defaults.</summary>
    /// <param name="name">Stable table name used to resolve shared view defaults.</param>
    /// <returns>The named view defaults, or null when unavailable.</returns>
    public Task<GXView?> ReadNameAsync(string name) => storage?.GetViewAsync(name) ?? ReadUrlAsync($"api/View/name?name={Uri.EscapeDataString(name)}");

    /// <summary>Saves shared overrides; an explicit empty Columns list restores the original defaults.</summary>
    /// <param name="view">Shared view definition to save; an empty columns collection restores its defaults.</param>
    /// <returns>The saved view, or null when an empty columns collection restores the original defaults.</returns>
    /// <exception cref="InvalidOperationException">The saved view cannot be retrieved after a successful write.</exception>
    public async Task<GXView?> SaveAsync(GXView view)
    {
        using var response = await http.PostAsJsonAsync(view.Id == Guid.Empty ? "api/View/Add" : "api/View/Update",
            new UpdateView { Views = [view] }, _options);
        response.EnsureSuccessStatusCode();
        if (storage != null)
        {
            await storage.RefreshViewsAsync();
        }
        if (view.Columns is { Count: 0 })
        {
            return null;
        }
        return await ReadNameAsync(view.Name!) ?? throw new InvalidOperationException(Properties.Resources.SavedViewCouldNotBeRead);
    }

    /// <summary>Reads a view endpoint and treats missing or empty-identifier results as absent.</summary>
    /// <param name="url">API endpoint from which to load the resource representation.</param>
    /// <returns>The returned view, or null when the endpoint reports absence or an invalid identifier.</returns>
    private async Task<GXView?> ReadUrlAsync(string url)
    {
        using var response = await http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetViewResponse>(_options);
        return result?.Item is { Id: var id } item && id != Guid.Empty ? item : null;
    }
}
