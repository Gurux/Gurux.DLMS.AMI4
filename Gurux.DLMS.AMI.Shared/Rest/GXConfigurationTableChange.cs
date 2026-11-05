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

namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Describes the pending schema changes for an application table.</summary>
[System.ComponentModel.Description("Describes the pending schema changes for an application table.")]
public sealed class GXConfigurationTableChange
{
    /// <summary>Gets or sets the name of the table with pending schema changes.</summary>
    [System.ComponentModel.Description("Gets or sets the name of the table with pending schema changes.")]
    public string TableName { get; set; } = string.Empty;
    /// <summary>Gets or sets the descriptions of the pending schema changes.</summary>
    [System.ComponentModel.Description("Gets or sets the descriptions of the pending schema changes.")]
    public string[] Changes { get; set; } = [];
}
