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

namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>A published catalog release with its product's display metadata.</summary>
public sealed class GXCatalogUpdateEntry
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    /// <summary>Release-specific summary, separate from the component description.</summary>
    public string? ReleaseNotes { get; set; }
    /// <summary>Catalog image URL, independent of installed module metadata.</summary>
    public string? IconUrl { get; set; }
    /// <summary>Optional manufacturer image reference, fetched through the image API with its normal access checks.</summary>
    public Guid? ManufacturerLogoId { get; set; }
    public bool IsModule { get; set; }
    public string? ReleaseUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
