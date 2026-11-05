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
    /// <summary>Connection, task execution, and update states reported by an agent.</summary>
    [System.ComponentModel.Description("Connection, task execution, and update states reported by an agent.")]
    public enum AgentStatus : byte
    {
        /// <summary>
        /// Agent is disconnected.
        /// </summary>
        [System.ComponentModel.Description("Agent is disconnected.")]
        [XmlEnum("0")]
        Offline = 0,
        /// <summary>
        /// Agent is connected.
        /// </summary>
        [System.ComponentModel.Description("Agent is connected.")]
        [XmlEnum("1")]
        Connected = 1,
        /// <summary>
        /// Agent is idle.
        /// </summary>
        [System.ComponentModel.Description("Agent is idle.")]
        [XmlEnum("2")]
        Idle = 2,
        /// <summary>
        /// Agent is processing the task.
        /// </summary>
        [System.ComponentModel.Description("Agent is processing the task.")]
        [XmlEnum("3")]
        Process = 3,
        /// <summary>The agent has reported an error.</summary>
        [System.ComponentModel.Description("The agent has reported an error.")]
        [XmlEnum("4")]
        Error = 4,
        /// <summary>
        /// The agent is downloading the new version.
        /// </summary>
        [System.ComponentModel.Description("The agent is downloading the new version.")]
        [XmlEnum("5")]
        Downloading = 5,
        /// <summary>The agent is installing a new version.</summary>
        [System.ComponentModel.Description("The agent is installing a new version.")]
        [XmlEnum("6")]
        Updating = 6,
        /// <summary>
        /// The agent is restarting after the update.
        /// </summary>
        [System.ComponentModel.Description("The agent is restarting after the update.")]
        [XmlEnum("7")]
        Restarting = 7,
        /// <summary>The agent reports a change to its available serial ports.</summary>
        [System.ComponentModel.Description("The agent reports a change to its available serial ports.")]
        [XmlEnum("8")]
        SerialPortChange = 8,
        /// <summary>
        /// Indicates that the agent has been detected.
        /// </summary>
        [System.ComponentModel.Description("Indicates that the agent has been detected.")]
        [XmlEnum("9")]
        Detected = 9,
    }
}
