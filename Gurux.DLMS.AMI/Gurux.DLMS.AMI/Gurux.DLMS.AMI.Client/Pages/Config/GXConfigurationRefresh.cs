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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Gurux.DLMS.AMI.Client.Pages.Config;

/// <summary>Reloads configuration invalidations without replacing local edits or disposed views.</summary>
internal sealed class GXConfigurationRefresh : IDisposable
{
    private long _generation;
    private bool _disposed;

    // The caller dispatches this method through ComponentBase.InvokeAsync so applying a response
    // and checking the edit context both execute on the renderer's synchronization context.
    public async Task RefreshAsync(HttpClient http, IEnumerable<GXTarget> updates, string name,
        Action<GXConfiguration> apply, Func<bool> canApply, IGXNotificationService? notifications = null)
    {
        var update = updates.LastOrDefault(it => (it.Type == GXConfigurationPolicies.Edit || it.Type == GXCatalogNotifications.ConfigurationChecked) && it.Name == name);
        if (_disposed || update == null || string.IsNullOrEmpty(update.Id) || !canApply())
        {
            return;
        }
        var generation = ++_generation;
        bool Current() => !_disposed && generation == _generation && canApply();
        try
        {
            // Notifications are invalidations; settings must be read through the authorized endpoint.
            var configuration = (await http.GetFromJsonAsync<GetConfigurationResponse>(
                "api/Configuration?id=" + Uri.EscapeDataString(update.Id)))?.Item;
            if (Current() && configuration?.Name == name && !string.IsNullOrEmpty(configuration.Settings))
            {
                apply(configuration);
            }
        }
        catch (AccessTokenNotAvailableException exception)
        {
            if (Current()) {
                exception.Redirect();
            }
        }
        catch (Exception exception)
        {
            if (Current()) {
                notifications?.ReportError(exception, "Configuration.Refresh." + name);
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        ++_generation;
    }
}
