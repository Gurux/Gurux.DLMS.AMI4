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

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Displays local notifications and delivers notifications to subscribed user devices.
    /// </summary>
    public interface IGXNotification
    {
        /// <summary>
        /// Asynchronously displays a notification in the current client.
        /// </summary>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification message.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task ShowNotificationAsync(string title, string message);

        /// <summary>Sends a notification to a user's subscribed devices, including when the AMI page is closed.</summary>
        /// <param name="userId">Recipient user identifier.</param>
        /// <param name="title">Notification title.</param>
        /// <param name="message">Notification message.</param>
        /// <param name="cancellationToken">Cancels delivery.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task ShowNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default);
    }
}
