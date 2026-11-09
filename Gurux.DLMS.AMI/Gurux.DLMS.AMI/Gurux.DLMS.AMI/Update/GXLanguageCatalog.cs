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

using System.Globalization;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Lists application languages through the same updater operations as localization list --owner.</summary>
public sealed class GXLanguageCatalog(HttpClient http, CatalogUpdaterOptions options)
{
    /// <summary>Fetches the localization index and projects the latest application language packages.</summary>
    public async Task<GXAvailableLanguage[]> ListAsync(CancellationToken cancellationToken = default)
    {
        const string owner = "Gurux.DLMS.AMI";
        var updater = new GXCatalogUpdateService(http);
        var source = options.LocalizationCatalogSource;
        if (string.IsNullOrWhiteSpace(source))
        {
            if (!Uri.TryCreate(options.CatalogUrl, UriKind.Absolute, out var catalogUri) || !GXCatalogUpdateService.IsSupportedUri(catalogUri))
                {
                throw new InvalidOperationException("The language catalog requires an HTTPS or loopback HTTP URL.");
            }
            var catalog = await updater.GetCatalogAsync(options.CatalogUrl, cancellationToken);
            var releases = await updater.GetReleasesAsync(catalog, "Gurux.AMI.Localization", "localization-index.json",
                count: 1, includePrereleases: options.IncludePrereleases, cancellationToken: cancellationToken);
            source = releases.FirstOrDefault()?.Asset?.DownloadUrl
                ?? throw new InvalidDataException("The update catalog has no localization-index.json asset.");
        }
        if (!Uri.TryCreate(source, UriKind.Absolute, out var indexUri) || !GXCatalogUpdateService.IsSupportedUri(indexUri))
            {
            throw new InvalidDataException("The localization index requires an HTTPS or loopback HTTP URL.");
        }
        var localizations = new GXLocalizationService(http, allowLoopbackHttp: true);
        var index = await localizations.GetCatalogAsync(source, cancellationToken);
        var packages = localizations.List(index, owner: owner, ownerType: "application", includePrereleases: options.IncludePrereleases);
        var languages = new Dictionary<string, GXAvailableLanguage>(StringComparer.OrdinalIgnoreCase);
        languages.Add("en", CreateLanguage("en", null));
        foreach (var release in packages.SelectMany(p => p.Releases))
        {
            foreach (var culture in release.Cultures)
            {
                if (culture.Equals("en", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (!languages.TryGetValue(culture, out var existing)
                    || Gurux.DLMS.AMI.Shared.GXReleaseVersionComparer.Instance.Compare(release.Version, existing.Version) > 0)
                    {
                    languages[culture] = CreateLanguage(culture, release.Version);
                }
            }
        }
        return languages.Values.OrderByDescending(l => l.Id == "en").ThenBy(l => l.EnglishName, StringComparer.Ordinal).ToArray();
    }

    private static GXAvailableLanguage CreateLanguage(string id, string? version)
    {
        var culture = CultureInfo.GetCultureInfo(id);
        return new() { Id = culture.Name, NativeName = culture.NativeName, EnglishName = culture.EnglishName,
            Version = version, Installed = id.Equals("en", StringComparison.OrdinalIgnoreCase) };
    }
}
