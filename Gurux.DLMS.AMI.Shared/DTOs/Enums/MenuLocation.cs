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
