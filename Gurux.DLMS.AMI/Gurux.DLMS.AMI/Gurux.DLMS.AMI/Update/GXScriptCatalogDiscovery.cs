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

using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Resolves the script index through the shared catalog using the updater SDK.</summary>
public static class GXScriptCatalogDiscovery
{
    /// <summary>Discovers metadata only; executable source remains untouched until an explicit import/update.</summary>
    public static async Task SynchronizeAsync(HttpClient http, GXUpdateCatalog catalog, CatalogUpdaterOptions options,
        GXScriptCatalogStore scripts, CancellationToken token = default)
    {
        var source = options.ScriptCatalogSource;
        if (string.IsNullOrWhiteSpace(source) && catalog.Items.Any(item => item.Id == options.ScriptProductId))
        {
            var releases = await new GXCatalogUpdateService(http).GetReleasesAsync(catalog, options.ScriptProductId, "script-index.json",
                count: 1, includePrereleases: options.IncludePrereleases, cancellationToken: token);
            source = (releases.Count == 0 ? null : releases[0]?.Asset?.DownloadUrl) ?? throw new InvalidDataException("The script index asset is missing.");
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || !GXCatalogUpdateService.IsSupportedUri(uri))
        {
            throw new InvalidDataException("The script index must use HTTPS or loopback HTTP.");
        }
        var index = await new GXScriptService(http).GetCatalogAsync(source, token);
        await scripts.SynchronizeAsync(source, index, token);
    }
}
