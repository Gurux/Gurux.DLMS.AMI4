//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
