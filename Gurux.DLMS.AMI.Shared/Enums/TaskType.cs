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
    /// <summary>Device operations and other work that can be queued as tasks.</summary>
    [System.ComponentModel.Description("Device operations and other work that can be queued as tasks.")]
    public enum TaskType : byte
    {
        /// <summary>
        /// No task type is selected.
        /// </summary>
        [System.ComponentModel.Description("No task type is selected.")]
        [XmlEnum("0")]
        None = 0,
        /// <summary>Read an attribute value from a device.</summary>
        [System.ComponentModel.Description("Read an attribute value from a device.")]
        [XmlEnum("1")]
        Read = 1,
        /// <summary>Write an attribute value to a device.</summary>
        [System.ComponentModel.Description("Write an attribute value to a device.")]
        [XmlEnum("2")]
        Write = 2,
        /// <summary>Invoke a method on a COSEM object.</summary>
        [System.ComponentModel.Description("Invoke a method on a COSEM object.")]
        [XmlEnum("3")]
        Action = 3,
        /// <summary>
        /// Refresh meter's association view.
        /// </summary>
        [System.ComponentModel.Description("Refresh meter's association view.")]
        [XmlEnum("4")]
        Refresh = 4,
        /// <summary>
        /// Send email.
        /// </summary>
        [System.ComponentModel.Description("Send email.")]
        [XmlEnum("5")]
        Email = 5,
    }
}
