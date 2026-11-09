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
    /// Report operation.
    /// </summary>
    public enum ReportOperation : byte
    {
        /// <summary>
        /// Sum value.
        /// </summary>
        [XmlEnum("0")]
        Sum = 0,
        /// <summary>
        /// Average value.
        /// </summary>
        [XmlEnum("1")]
        Average = 1,
        /// <summary>
        /// Minimum value.
        /// </summary>
        [XmlEnum("2")]
        Minimum = 2,
        /// <summary>
        /// Maximum value.
        /// </summary>
        [XmlEnum("3")]
        Maximum = 3,
        /// <summary>
        /// Count.
        /// </summary>
        [XmlEnum("4")]
        Count = 4,
        /// <summary>
        /// Instant values are returned.
        /// </summary>
        [XmlEnum("5")]
        Instant = 5
    }
}
