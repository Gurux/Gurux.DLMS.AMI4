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
    /// <summary>
    /// Module types.
    /// </summary>
    public enum ModuleType : UInt16
    {
        /// <summary>
        /// There are no modules.
        /// </summary>
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>
        /// Extension module.
        /// </summary>
        [XmlEnum("1")]
        Extension = 0x1,
        /// <summary>
        /// Device group extension module.
        /// </summary>
        [XmlEnum("2")]
        DeviceGroup = 0x2,
        /// <summary>
        /// Device extension module.
        /// </summary>
        [XmlEnum("4")]
        Device = 0x4,
        /// <summary>
        /// Object extension module.
        /// </summary>
        [XmlEnum("8")]
        Object = 0x8,
        /// <summary>
        /// Attribute extension module.
        /// </summary>
        [XmlEnum("16")]
        Attribute = 0x10,
        /// <summary>
        /// Settings module.
        /// </summary>
        [XmlEnum("32")]
        Settings = 0x20,
        /// <summary>
        /// User group.
        /// </summary>
        [XmlEnum("64")]
        UserGroup = 0x40,
        /// <summary>
        /// User.
        /// </summary>
        [XmlEnum("128")]
        User = 0x80,
        /// <summary>
        /// This module can be invoked from schedule.
        /// </summary>
        [XmlEnum("256")]
        Schedule = 0x100,
    }
}
