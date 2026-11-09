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
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.Updater.Enums;
using Gurux.Updater.Model;

namespace Gurux.DLMS.AMI.Update;

internal static class GXCatalogUpdateEntries
{
    internal static GXCatalogUpdateEntry[] Create(GXUpdateCatalog catalog, IEnumerable<GXManufacturer> manufacturers)
        => Project(catalog, manufacturers)
            .OrderByDescending(item => item.PublishedAt)
            .ThenBy(item => item.ProductId, StringComparer.Ordinal)
            .ThenByDescending(item => item.Version, GXReleaseVersionComparer.Instance)
            .DistinctBy(item => item.ProductId, StringComparer.OrdinalIgnoreCase)
            .Take(10).ToArray();

    internal static GXCatalogUpdateEntry? Find(GXUpdateCatalog catalog, IEnumerable<GXManufacturer> manufacturers, string productId, string version)
        => Project(catalog, manufacturers).FirstOrDefault(item => item.ProductId == productId && item.Version == version);

    private static IEnumerable<GXCatalogUpdateEntry> Project(GXUpdateCatalog catalog, IEnumerable<GXManufacturer> manufacturers)
    {
        var brands = manufacturers.ToArray();
        return catalog.Items.SelectMany(product => product.Releases
                .Where(release => !(product.Type == CatalogProductType.Configuration && release.TagName == "index"))
                .Select(release => new GXCatalogUpdateEntry
                {
                    ProductId = product.Id,
                    Name = string.IsNullOrWhiteSpace(product.Name) ? product.Id : product.Name,
                    Version = release.Version,
                    Description = string.IsNullOrWhiteSpace(product.Description) ? release.ReleaseNotes : product.Description,
                    ReleaseNotes = release.ReleaseNotes,
                    IsModule = product.Type == CatalogProductType.Module,
                    IconUrl = SafeUrl(product.IconUrl),
                    ManufacturerLogoId = FindManufacturerLogo(product, brands),
                    ReleaseUrl = SafeUrl(release.ReleaseUrl),
                    PublishedAt = release.PublishedAt
                }));
    }

    private static Guid? FindManufacturerLogo(GXUpdateCatalogItem product, GXManufacturer[] manufacturers)
    {
        // The aggregate DeviceProfiles product does not select a particular brand.
        // Match only an exact manufacturer name or a namespaced name/ID, never a substring.
        var prefix = new[] { "Gurux.DLMS.DeviceProfiles", "Gurux.DLMS.DeviceProfile" }
            .FirstOrDefault(p => product.Id.Equals(p, StringComparison.OrdinalIgnoreCase) || product.Id.StartsWith(p + ".", StringComparison.OrdinalIgnoreCase));
        if (prefix == null) {
            return null;
        }
        var identity = product.Id.Length > prefix.Length ? product.Id[(prefix.Length + 1)..] : null;
        var matches = manufacturers.Where(m => m.Removed == null &&
            ((!string.IsNullOrWhiteSpace(m.Name) && (string.Equals(m.Name, product.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Name, identity, StringComparison.OrdinalIgnoreCase))) ||
             (Guid.TryParse(identity, out var id) && m.Id == id))).Take(2).ToArray();
        return matches.Length == 1 && matches[0].ProfilePicture?.Id is { } logo && logo != Guid.Empty ? logo : null;
    }

    private static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) ? uri.AbsoluteUri : null;
}
