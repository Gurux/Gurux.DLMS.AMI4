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
    /// <summary>Identifies the type of source that initiated a recorded entity change.</summary>
    /// <remarks>Describes the source category, not the identifier of a specific user, agent, scheduled task, or module.</remarks>
    [System.ComponentModel.Description("Identifies the type of source that initiated a recorded entity change.")]
    public enum ChangeSource : byte
    {
        /// <summary>The change was initiated by a user.</summary>
        [System.ComponentModel.Description("The change was initiated by a user.")]
        [XmlEnum("0")]
        User,
        /// <summary>The change was initiated by an agent.</summary>
        [System.ComponentModel.Description("The change was initiated by an agent.")]
        [XmlEnum("1")]
        Agent,
        /// <summary>The change was initiated by a scheduled task.</summary>
        [System.ComponentModel.Description("The change was initiated by a scheduled task.")]
        [XmlEnum("2")]
        Scheduler,
        /// <summary>The change was initiated by an internal system process.</summary>
        [System.ComponentModel.Description("The change was initiated by an internal system process.")]
        [XmlEnum("3")]
        System,
        /// <summary>The change was initiated by a module.</summary>
        [System.ComponentModel.Description("The change was initiated by a module.")]
        [XmlEnum("4")]
        Module
    }
}
