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
    /// <summary>Resource events that can cause a notification to be delivered.</summary>
    [System.ComponentModel.Description("Resource events that can cause a notification to be delivered.")]
    [Flags]
    public enum NotificationAction : UInt16
    {
        /// <summary>Do not send notifications for any action.</summary>
        [System.ComponentModel.Description("Do not send notifications for any action.")]
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>Notify when an item is added.</summary>
        [System.ComponentModel.Description("Notify when an item is added.")]
        [XmlEnum("1")]
        Add = 0x1,
        /// <summary>Notify when an item is modified.</summary>
        [System.ComponentModel.Description("Notify when an item is modified.")]
        [XmlEnum("2")]
        Edit = 0x2,
        /// <summary>Notify when an item is removed.</summary>
        [System.ComponentModel.Description("Notify when an item is removed.")]
        [XmlEnum("4")]
        Remove = 0x4,
        /// <summary>Notify when items are cleared within a resource scope.</summary>
        [System.ComponentModel.Description("Notify when items are cleared within a resource scope.")]
        [XmlEnum("8")]
        Clear = 0x8,
        /// <summary>Notify when an item is executed.</summary>
        [System.ComponentModel.Description("Notify when an item is executed.")]
        [XmlEnum("16")]
        Execute = 0x10,
        /// <summary>Notify when execution reports an error.</summary>
        [System.ComponentModel.Description("Notify when execution reports an error.")]
        [XmlEnum("32")]
        Error = 0x20,
        /// <summary>Notify when an item is closed.</summary>
        [System.ComponentModel.Description("Notify when an item is closed.")]
        [XmlEnum("64")]
        Close = 0x40,
        /// <summary>Notify when an item's status changes.</summary>
        [System.ComponentModel.Description("Notify when an item's status changes.")]
        [XmlEnum("128")]
        StatusChange = 0x80,
        /// <summary>Notify when an operation starts.</summary>
        [System.ComponentModel.Description("Notify when an operation starts.")]
        [XmlEnum("256")]
        Start = 0x100,
        /// <summary>Notify when an operation ends.</summary>
        [System.ComponentModel.Description("Notify when an operation ends.")]
        [XmlEnum("512")]
        End = 0x200,
    }
}
