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
using Gurux.DLMS.AMI.Shared;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.Update;

internal static class GXCatalogReleaseSummary
{
    internal static List<GXAvailableRelease> Create(GXUpdateCatalogItem item, IReadOnlyList<CatalogRelease>? selected = null)
    {
        var assemblies = GXCatalogCompatibility.HostAssemblies();
        return [.. item.Releases.Where(r => GXCatalogCompatibility.IsCompatible(item.Type, r, assemblies) &&
                GXReleaseVersionComparer.TryNormalize(r.Version, out _) &&
                (selected == null || selected.Any(s => s.Url != null && GXReleaseVersionComparer.Instance.Compare(s.Number, r.Version) == 0)))
            .Select(r => new GXAvailableRelease
            {
                Version = Normalize(r.Version),
                Prerelease = r.IsPrerelease || GXReleaseVersionComparer.IsPrerelease(r.Version),
                IsSecurityUpdate = r.IsSecurityUpdate,
                PublishedAt = r.PublishedAt,
                ReleaseNotesUrl = SafeUrl(r.ReleaseUrl),
                ReleaseNotes = r.ReleaseNotes
            }).OrderByDescending(r => r.Version, GXReleaseVersionComparer.Instance)];
    }

    private static string Normalize(string value) => GXReleaseVersionComparer.TryNormalize(value, out var normalized) ? normalized : value;
    private static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        GXCatalogUpdateService.IsSupportedUri(uri) ? uri.AbsoluteUri : null;
}
