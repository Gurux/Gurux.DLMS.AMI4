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

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>
/// The layout region hosting a shared menu.
/// </summary>
[System.ComponentModel.Description("The layout region hosting a shared menu.")]
public enum MenuLocation
{
    /// <summary>The top menu area.</summary>
    [System.ComponentModel.Description("The top menu area.")]
    Top,
    /// <summary>The left menu area.</summary>
    [System.ComponentModel.Description("The left menu area.")]
    Left,
    /// <summary>The right menu area.</summary>
    [System.ComponentModel.Description("The right menu area.")]
    Right,
    /// <summary>The bottom menu area.</summary>
    [System.ComponentModel.Description("The bottom menu area.")]
    Bottom,
    /// <summary>The footer menu area.</summary>
    [System.ComponentModel.Description("The footer menu area.")]
    Footer,
}
