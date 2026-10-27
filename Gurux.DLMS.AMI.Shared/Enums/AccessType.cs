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
using System;
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Controls whether a configuration value is hidden, displayed, or editable in the user interface.</summary>
    [System.ComponentModel.Description("Controls whether a configuration value is hidden, displayed, or editable in the user interface.")]
    public enum AccessType : byte
    {
        /// <summary>Hide the configuration value in the user interface.</summary>
        [System.ComponentModel.Description("Hide the configuration value in the user interface.")]
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>Display the configuration value without allowing edits.</summary>
        [System.ComponentModel.Description("Display the configuration value without allowing edits.")]
        [XmlEnum("1")]
        View = 0x1,
        /// <summary>Allow the user to view and edit the configuration value.</summary>
        [System.ComponentModel.Description("Allow the user to view and edit the configuration value.")]
        [XmlEnum("2")]
        Edit = 0x2
    }
}
