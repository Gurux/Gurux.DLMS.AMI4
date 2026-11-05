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
    /// <summary>
    /// What types of notifications the user is interested in.
    /// </summary>
    [System.ComponentModel.Description("What types of notifications the user is interested in.")]
    public enum UserNotification : byte
    {
        /// <summary>
        /// User wants no notifications.
        /// </summary>
        [System.ComponentModel.Description("User wants no notifications.")]
        [XmlEnum("0")]
        None = 0,
        /// <summary>
        /// The user is notified when logging in from a new IP address.
        /// </summary>
        [System.ComponentModel.Description("The user is notified when logging in from a new IP address.")]
        [XmlEnum("1")]
        NewIp = 1,
        /// <summary>Notify when a personal access token is expiring or has expired.</summary>
        [System.ComponentModel.Description("Notify when a personal access token is expiring or has expired.")]
        [XmlEnum("2")]
        PetExpiring = 2,
        /// <summary>
        /// The user is notified when content is modified.
        /// </summary>
        [System.ComponentModel.Description("The user is notified when content is modified.")]
        [XmlEnum("4")]
        Content = 4,
    }
}