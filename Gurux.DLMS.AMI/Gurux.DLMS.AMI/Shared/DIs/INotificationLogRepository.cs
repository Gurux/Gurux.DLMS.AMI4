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

using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle notification logs.
    /// </summary>
    public interface INotificationLogRepository
    {
        /// <summary>
        /// List notification logs.
        /// </summary>
        /// <returns>List of notification logs.</returns>
        Task<GXNotificationLog[]> ListAsync(
            ListNotificationLogs? request,
            ListNotificationLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read notification log information.
        /// </summary>
        /// <param name="id">Notification log id.</param>
        /// <returns>Notification log information.</returns>
        Task<GXNotificationLog> ReadAsync(Guid id);

        /// <summary>
        /// Clear notification logs.
        /// </summary>
        Task ClearAsync(Guid[]? notifications);

        /// <summary>
        /// Add notification logs.
        /// </summary>
        /// <param name="type">Notification log type.</param>
        /// <param name="logs">New logs.</param>
        Task AddAsync(string type, IEnumerable<GXNotificationLog> logs);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Notification log type.</param>
        /// <param name="notification">Notification.</param>
        /// <param name="ex">Exception.</param>
        Task<GXNotificationLog> AddAsync(string type, GXNotification notification, Exception ex);       
    }
}
