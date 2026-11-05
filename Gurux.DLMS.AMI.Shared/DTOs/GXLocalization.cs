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

using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>Persisted localization release metadata synchronized from the update catalog.</summary>
[System.ComponentModel.Description("Persisted localization release metadata synchronized from the update catalog.")]
[IndexCollection(true, nameof(Owner), nameof(OwnerType), nameof(Version))]
public sealed class GXLocalization : IUnique<Guid>
{
    /// <summary>Gets or sets the unique identifier of the localization release.</summary>
    [System.ComponentModel.Description("Gets or sets the unique identifier of the localization release.")]
    public Guid Id { get; set; }
    /// <summary>Gets or sets the identifier of the product that owns this localization release.</summary>
    [System.ComponentModel.Description("Gets or sets the identifier of the product that owns this localization release.")]
    [StringLength(256)] public string Owner { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of product that owns this localization release.</summary>
    [System.ComponentModel.Description("Gets or sets the type of product that owns this localization release.")]
    [StringLength(32)] public string OwnerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the localization release version.</summary>
    [System.ComponentModel.Description("Gets or sets the localization release version.")]
    [StringLength(64)] public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the culture identifiers supported by the localization package.</summary>
    [System.ComponentModel.Description("Gets or sets the culture identifiers supported by the localization package.")]
    [StringLength(1024)] public string Cultures { get; set; } = string.Empty;
    /// <summary>Gets or sets the compatible owner version range, if specified.</summary>
    [System.ComponentModel.Description("Gets or sets the compatible owner version range, if specified.")]
    [StringLength(128)] public string? OwnerVersionRange { get; set; }
    /// <summary>Gets or sets the default culture of the localization package.</summary>
    [System.ComponentModel.Description("Gets or sets the default culture of the localization package.")]
    [StringLength(32)] public string DefaultCulture { get; set; } = string.Empty;
    /// <summary>Gets or sets the description of the localization release.</summary>
    [System.ComponentModel.Description("Gets or sets the description of the localization release.")]
    public string? Description { get; set; }
    /// <summary>Gets or sets the release notes of the localization package.</summary>
    [System.ComponentModel.Description("Gets or sets the release notes of the localization package.")]
    public string? ReleaseNotes { get; set; }
    /// <summary>Gets or sets the download URL of the localization package.</summary>
    [System.ComponentModel.Description("Gets or sets the download URL of the localization package.")]
    [StringLength(2048)] public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets the expected SHA-256 hash of the localization package.</summary>
    [System.ComponentModel.Description("Gets or sets the expected SHA-256 hash of the localization package.")]
    [StringLength(64)] public string Sha256 { get; set; } = string.Empty;
    /// <summary>Gets or sets the localization package size in bytes.</summary>
    [System.ComponentModel.Description("Gets or sets the localization package size in bytes.")]
    public long Size { get; set; }
    /// <summary>Gets or sets whether the localization release is enabled.</summary>
    [System.ComponentModel.Description("Gets or sets whether the localization release is enabled.")]
    public bool Enabled { get; set; }
    /// <summary>Gets or sets the time the localization metadata was last updated.</summary>
    [System.ComponentModel.Description("Gets or sets the time the localization metadata was last updated.")]
    public DateTimeOffset Updated { get; set; }
}
