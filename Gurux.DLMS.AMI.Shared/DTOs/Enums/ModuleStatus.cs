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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>The installation state or build origin of a module.</summary>
    [System.ComponentModel.Description("The installation state or build origin of a module.")]
    [Flags]
    public enum ModuleStatus : byte
    {
        /// <summary>The module is available for installation.</summary>
        [System.ComponentModel.Description("The module is available for installation.")]
        [XmlEnum("1")]
        Installable = 0x1,
        /// <summary>The module is installed.</summary>
        [System.ComponentModel.Description("The module is installed.")]
        [XmlEnum("2")]
        Installed = 0x2,
        /// <summary>The module is supplied as a custom build.</summary>
        [System.ComponentModel.Description("The module is supplied as a custom build.")]
        [XmlEnum("4")]
        CustomBuild = 0x4,
    }
}
