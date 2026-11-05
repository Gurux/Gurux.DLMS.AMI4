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
