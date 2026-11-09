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

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Resource type.
    /// </summary>
    public enum ResourceType : UInt32
    {
        /// <summary>
        /// General Image.
        /// </summary>
        [XmlEnum("0")]
        Image = 0,
        /// <summary>
        /// Icon pack image.
        /// </summary>
        [XmlEnum("1")]
        Iconpack = 1,
        /// <summary>
        /// Theme variable.
        /// </summary>
        [XmlEnum("2")]
        Theme = 2,
        /// <summary>
        /// Css style.
        /// </summary>
        [XmlEnum("3")]
        Css = 3,
    }
}