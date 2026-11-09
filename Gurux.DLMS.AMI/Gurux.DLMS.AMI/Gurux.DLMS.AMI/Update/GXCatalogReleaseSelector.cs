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

using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Shared;
using Gurux.Updater.Enums;
using Gurux.Updater.Model;
using Gurux.Updater.Services;
namespace Gurux.DLMS.AMI.Update;

/// <summary>Provides catalog release.</summary>
/// <param name="Number">The number.</param>
/// <param name="Prerelease">The prerelease.</param>
/// <param name="PublishedAt">The published at.</param>
/// <param name="Description">The description.</param>
/// <param name="FileName">The file name.</param>
/// <param name="Url">The url.</param>
public sealed record CatalogRelease(string Number, bool Prerelease, DateTimeOffset? PublishedAt, string? Description, string? FileName, string? Url);
/// <summary>Provides catalog release selector.</summary>
public static class GXCatalogReleaseSelector
{
    /// <summary>Performs the latest operation.</summary>
    /// <param name="releases">The releases.</param>
    /// <param name="includePrereleases">The include prereleases.</param>
    public static CatalogRelease? Latest(IEnumerable<CatalogRelease> releases, bool includePrereleases = false)
        => releases.Where(r => (includePrereleases || !r.Prerelease) && !string.IsNullOrWhiteSpace(r.Url))
            .OrderByDescending(r => r.Number, GXReleaseVersionComparer.Instance).FirstOrDefault();

    /// <summary>Performs the select operation.</summary>
    /// <param name="item">The item.</param>
    /// <param name="pattern">The pattern.</param>
    public static async Task<IReadOnlyList<CatalogRelease>> SelectAsync(GXUpdateCatalogItem item, string? pattern)
    {
        using var client = new HttpClient();
        var service = new GXCatalogUpdateService(client);
        var catalog = new GXUpdateCatalog { SchemaVersion = 1, Items = [item] };
        // The library performs catalog validation and cloning; never truncate the stored history to its default10.
        var releases = await service.GetReleasesAsync(catalog, item.Id, pattern, Math.Max(1, item.Releases.Count), true);
        var matcher = new Regex("^" + Regex.Escape(pattern ?? "*.zip").Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var selected = new List<CatalogRelease>();
        var assemblies = GXCatalogCompatibility.HostAssemblies();
        foreach (var release in releases)
        {
            if (!GXReleaseVersionComparer.TryNormalize(release.Version, out var number))
            {
                throw new InvalidDataException("Invalid catalog version for " + item.Id + ": " + release.Version);
            }

            var assets = release.Assets.Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && matcher.IsMatch(a.Name)).ToArray();
            var asset = assets.Length == 1 ? assets[0] : null;
            if (!GXCatalogCompatibility.IsCompatible(item.Type, release, assemblies))
            {
                asset = null;
            }
            selected.Add(new(number, release.IsPrerelease || GXReleaseVersionComparer.IsPrerelease(number), release.PublishedAt, release.ReleaseNotes, asset?.Name, asset?.DownloadUrl));
        }
        return selected;
    }
}


