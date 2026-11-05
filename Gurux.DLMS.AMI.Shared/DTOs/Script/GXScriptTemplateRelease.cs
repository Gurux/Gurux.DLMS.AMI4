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

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script;

/// <summary>A retained immutable catalog release. Synchronization never changes installed source code.</summary>
[System.ComponentModel.Description("A retained immutable catalog release. Synchronization never changes installed source code.")]
[IndexCollection(true, nameof(CatalogId), nameof(Version))]
public sealed class GXScriptTemplateRelease : IUnique<Guid>
{
    /// <summary>Local release identifier.</summary>
    [System.ComponentModel.Description("Local release identifier.")]
    public Guid Id { get; set; }
    /// <summary>Publisher's stable script identifier.</summary>
    [System.ComponentModel.Description("Publisher's stable script identifier.")]
    [StringLength(200), Index(false)] public string CatalogId { get; set; } = "";
    /// <summary>Normalized NuGet version.</summary>
    [System.ComponentModel.Description("Normalized NuGet version.")]
    [StringLength(64)] public string Version { get; set; } = "";
    /// <summary>Validated index source, configured by the host.</summary>
    [System.ComponentModel.Description("Validated index source, configured by the host.")]
    [StringLength(2048)] public string CatalogSource { get; set; } = "";
    /// <summary>Validated native SDK manifest, including immutable source identity.</summary>
    [System.ComponentModel.Description("Validated native SDK manifest, including immutable source identity.")]
    [JsonIgnore] public string ManifestJson { get; set; } = "";
    /// <summary>False when a later complete index no longer advertises this release.</summary>
    [System.ComponentModel.Description("False when a later complete index no longer advertises this release.")]
    public bool Enabled { get; set; } = true;
}
