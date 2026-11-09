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

namespace Gurux.DLMS.AMI.Update;

/// <summary>Provides catalog updater options.</summary>
public sealed class CatalogUpdaterOptions
{
    /// <summary>Gets or sets the core product id.</summary>
    public string CoreProductId { get; set; } = "Gurux.DLMS.AMI";
    /// <summary>Gets or sets the manufacturer product id.</summary>
    public string ManufacturerProductId { get; set; } = "Gurux.DLMS.DeviceProfiles";
    /// <summary>Gets or sets the creator id.</summary>
    public string? CreatorId { get; set; }
    /// <summary>Gets or sets the include prereleases.</summary>
    public bool IncludePrereleases { get; set; }
    /// <summary>Gets or sets the catalog url.</summary>
    public string CatalogUrl { get; set; } = "https://gurux.github.io/Gurux.AMI.Catalog/catalog.json";
    /// <summary>Optional localization index override; otherwise resolved from Gurux.AMI.Localization in CatalogUrl.</summary>
    public string? LocalizationCatalogSource { get; set; }
    /// <summary>Optional HTTPS/loopback script index; otherwise resolved from Gurux.AMI.Scripts in the shared catalog.</summary>
    public string? ScriptCatalogSource { get; set; }
    /// <summary>Shared catalog product containing the script index.</summary>
    public string ScriptProductId { get; set; } = "Gurux.AMI.Scripts";
    /// <summary>Gets or sets the default module asset pattern.</summary>
    public string DefaultModuleAssetPattern { get; set; } = "*.zip";
    /// <summary>Gets or sets the default agent asset pattern.</summary>
    public string DefaultAgentAssetPattern { get; set; } = "*.zip";
    /// <summary>Gets or sets the asset patterns.</summary>
    public Dictionary<string, string> AssetPatterns { get; set; } = new(StringComparer.Ordinal);
}

