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
    /// <summary>The connection and task execution state of a gateway.</summary>
    [System.ComponentModel.Description("The connection and task execution state of a gateway.")]
    public enum GatewayStatus : byte
    {
        /// <summary>
        /// Gateway is disconnected.
        /// </summary>
        [System.ComponentModel.Description("Gateway is disconnected.")]
        [XmlEnum("0")]
        Offline = 0,
        /// <summary>
        /// Gateway is connected.
        /// </summary>
        [System.ComponentModel.Description("Gateway is connected.")]
        [XmlEnum("1")]
        Connected = 1,
        /// <summary>
        /// Gateway is idle.
        /// </summary>
        [System.ComponentModel.Description("Gateway is idle.")]
        [XmlEnum("2")]
        Idle = 2,
        /// <summary>
        /// Gateway is processing the task.
        /// </summary>
        [System.ComponentModel.Description("Gateway is processing the task.")]
        [XmlEnum("3")]
        Process = 3,
        /// <summary>The gateway is in an error state.</summary>
        [System.ComponentModel.Description("The gateway is in an error state.")]
        [XmlEnum("4")]
        Error = 4,
        /// <summary>
        /// Indicates that the gateway has been detected.
        /// </summary>
        [System.ComponentModel.Description("Indicates that the gateway has been detected.")]
        [XmlEnum("5")]
        Detected = 5
    }
}
