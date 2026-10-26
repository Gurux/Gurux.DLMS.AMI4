//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
