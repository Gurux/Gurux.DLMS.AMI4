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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle notifications.
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// List notifications.
        /// </summary>
        /// <returns>Notifications.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXNotification>> ListAsync(
            ListNotifications? request = null,
            ListNotificationResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read notification.
        /// </summary>
        /// <param name="id">Notification id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXNotification> ReadAsync(Guid id, Expression<Func<GXNotification, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update notification(s).
        /// </summary>
        /// <param name="notifications">Updated notification(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXNotification> notifications,
            Expression<Func<GXNotification, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete notification(s).
        /// </summary>
        /// <param name="notifications">Notification(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> notifications, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh notifications.
        /// </summary>
        /// <param name="user">User for whom the notifications are being refreshed.</param>
        /// <param name="delete">If true, exists notifications are deleted and the new ones are generated.</param>
        /// <param name="notify">If true, notifications are sent for the refresh.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> RefreshAsync(GXUser? user, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);
    }
}
