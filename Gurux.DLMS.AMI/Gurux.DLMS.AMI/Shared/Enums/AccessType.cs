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
    /// <summary>
    /// Configuration value access types.
    /// </summary>
    public enum AccessType : byte
    {
        /// <summary>
        /// Configuration value is hidden on the UI.
        /// </summary>
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>
        /// Configuration value is shown on the UI.
        /// </summary>
        [XmlEnum("1")]
        View = 0x1,
        /// <summary>
        /// User can change configuration value from the UI.
        /// </summary>
        [XmlEnum("2")]
        Edit = 0x2
    }
}
