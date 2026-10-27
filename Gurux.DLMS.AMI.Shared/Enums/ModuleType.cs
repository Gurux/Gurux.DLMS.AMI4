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
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Module capabilities and the resource contexts in which they can be used.</summary>
    [System.ComponentModel.Description("Module capabilities and the resource contexts in which they can be used.")]
    public enum ModuleType : UInt16
    {
        /// <summary>No module capabilities are specified.</summary>
        [System.ComponentModel.Description("No module capabilities are specified.")]
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>
        /// Extension module.
        /// </summary>
        [System.ComponentModel.Description("Extension module.")]
        [XmlEnum("1")]
        Extension = 0x1,
        /// <summary>
        /// Device group extension module.
        /// </summary>
        [System.ComponentModel.Description("Device group extension module.")]
        [XmlEnum("2")]
        DeviceGroup = 0x2,
        /// <summary>
        /// Device extension module.
        /// </summary>
        [System.ComponentModel.Description("Device extension module.")]
        [XmlEnum("4")]
        Device = 0x4,
        /// <summary>
        /// Object extension module.
        /// </summary>
        [System.ComponentModel.Description("Object extension module.")]
        [XmlEnum("8")]
        Object = 0x8,
        /// <summary>
        /// Attribute extension module.
        /// </summary>
        [System.ComponentModel.Description("Attribute extension module.")]
        [XmlEnum("16")]
        Attribute = 0x10,
        /// <summary>
        /// Settings module.
        /// </summary>
        [System.ComponentModel.Description("Settings module.")]
        [XmlEnum("32")]
        Settings = 0x20,
        /// <summary>The module provides functionality for user groups.</summary>
        [System.ComponentModel.Description("The module provides functionality for user groups.")]
        [XmlEnum("64")]
        UserGroup = 0x40,
        /// <summary>The module provides functionality for individual users.</summary>
        [System.ComponentModel.Description("The module provides functionality for individual users.")]
        [XmlEnum("128")]
        User = 0x80,
        /// <summary>The module can be invoked by a schedule.</summary>
        [System.ComponentModel.Description("The module can be invoked by a schedule.")]
        [XmlEnum("256")]
        Schedule = 0x100,
    }
}
