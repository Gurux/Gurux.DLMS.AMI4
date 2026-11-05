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
using Gurux.Service.Orm.Common;
namespace Gurux.DLMS.AMI.Shared.DTOs.Update;

/// <summary>
/// Persistent catalog identity, independent of mutable display names and agent GUIDs.
/// </summary>
[System.ComponentModel.Description("Persistent catalog identity, independent of mutable display names and agent GUIDs.")]
public sealed class GXCatalogProduct : IUnique<string>
{
    /// <summary>Gets or sets the stable product identifier in the update catalog.</summary>
    [System.ComponentModel.Description("Gets or sets the stable product identifier in the update catalog.")]
    [Key, StringLength(256)]
    public string Id { get; set; } = "";

    /// <summary>Gets or sets the identifier of the application entity associated with the catalog product.</summary>
    [System.ComponentModel.Description("Gets or sets the identifier of the application entity associated with the catalog product.")]
    [StringLength(128)]
    public string EntityId { get; set; } = "";
}
