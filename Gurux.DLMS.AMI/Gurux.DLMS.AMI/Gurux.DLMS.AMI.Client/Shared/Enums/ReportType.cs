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
    /// Report type.
    /// </summary>
    public enum ReportType : int
    {
        /// <summary>
        /// Value.
        /// </summary>
        [XmlEnum("0")]
        Value = 0,
        /// <summary>
        /// How many times target is read.
        /// </summary>
        [XmlEnum("1")]
        Read = 1,
        /// <summary>
        /// Write.
        /// </summary>
        [XmlEnum("2")]
        Write = 2,
        /// <summary>
        /// Action.
        /// </summary>
        [XmlEnum("3")]
        Action = 3,
        /// <summary>
        /// Read error.
        /// </summary>
        [XmlEnum("4")]
        ReadError = 4,
        /// <summary>
        /// Write error.
        /// </summary>
        [XmlEnum("5")]
        WriteError = 5,
        /// <summary>
        /// Action error.
        /// </summary>
        [XmlEnum("6")]
        ActionError = 6,
        /// <summary>
        /// Execution time.
        /// </summary>
        [XmlEnum("7")]
        ExecutionTime = 7,
        /// <summary>
        /// Connect.
        /// </summary>
        [XmlEnum("8")]
        Connect = 8,
        /// <summary>
        /// Disconnect.
        /// </summary>
        [XmlEnum("9")]
        Disconnect = 9,
        /// <summary>
        /// Generic errors.
        /// </summary>
        [XmlEnum("10")]
        Error = 10,
        /// <summary>
        /// Generic warnings.
        /// </summary>
        [XmlEnum("11")]
        Warning = 11,
    }
}
