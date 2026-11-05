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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums;

/// <summary>The purpose and presentation role of a menu.</summary>
[System.ComponentModel.Description("The purpose and presentation role of a menu.")]
public enum MenuKind : byte
{
    /// <summary>Navigation links.</summary>
    [System.ComponentModel.Description("Navigation links.")]
    Navigation = 0,
    /// <summary>Tabbed content.</summary>
    [System.ComponentModel.Description("Tabbed content.")]
    Tabs = 1,
    /// <summary>Configuration entries.</summary>
    [System.ComponentModel.Description("Configuration entries.")]
    Configuration = 2,
    /// <summary>Menu bar commands.</summary>
    [System.ComponentModel.Description("Menu bar commands.")]
    MenuBar = 3,
    /// <summary>Context menu commands.</summary>
    [System.ComponentModel.Description("Context menu commands.")]
    ContextMenu = 4,
    /// <summary>Dropdown menu items.</summary>
    [System.ComponentModel.Description("Dropdown menu items.")]
    Dropdown = 5,
    /// <summary>Overflow menu items.</summary>
    [System.ComponentModel.Description("Overflow menu items.")]
    Overflow = 6,
    /// <summary>All menu kinds for filtering or default operations; not a stored or rendered menu kind.</summary>
    [System.ComponentModel.Description("All menu kinds for filtering or default operations; not a stored or rendered menu kind.")]
    All = 255
}
