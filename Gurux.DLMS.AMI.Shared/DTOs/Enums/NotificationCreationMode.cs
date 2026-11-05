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
    /// Notification creation mode defines how notifications are created for a user.
    /// </summary>
    public enum NotificationCreationMode
    {
        /// <summary>
        /// User can create a single notification configuration.
        /// </summary>
        [XmlEnum("0")]
        Single = 0,
        /// <summary>
        /// Single notification configuration is created automatically for the user.
        /// </summary>
        [XmlEnum("1")]
        Automatic = 1,
        /// <summary>
        /// User can create multiple notification configurations.
        /// </summary>
        [XmlEnum("2")]
        Multiple = 2
    }
}
