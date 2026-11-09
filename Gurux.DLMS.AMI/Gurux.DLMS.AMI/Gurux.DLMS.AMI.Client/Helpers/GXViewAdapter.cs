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
public sealed class GXViewAdapter(HttpClient http, IAmiViewStorage? storage = null)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the view with the specified identifier.</summary>
    public Task<GXView?> ReadAsync(Guid id) => ReadUrlAsync($"api/View?id={id}");

    /// <summary>Reads the named table defaults.</summary>
    public Task<GXView?> ReadNameAsync(string name) => storage?.GetViewAsync(name) ?? ReadUrlAsync($"api/View/name?name={Uri.EscapeDataString(name)}");

    /// <summary>Saves shared overrides; an explicit empty Columns list restores the original defaults.</summary>
    public async Task<GXView?> SaveAsync(GXView view)
    {
        using var response = await http.PostAsJsonAsync(view.Id == Guid.Empty ? "api/View/Add" : "api/View/Update",
            new UpdateView { Views = [view] }, Options);
        response.EnsureSuccessStatusCode();
        if (storage != null)
        {
            await storage.RefreshViewsAsync();
        }
        if (view.Columns is { Count: 0 })
        {
            return null;
        }
        return await ReadNameAsync(view.Name!) ?? throw new InvalidOperationException("The saved view could not be read.");
    }

    private async Task<GXView?> ReadUrlAsync(string url)
    {
        using var response = await http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetViewResponse>(Options);
        return result?.Item is { Id: var id } item && id != Guid.Empty ? item : null;
    }
}
