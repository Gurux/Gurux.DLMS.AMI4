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
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used to handle listed notification types.
    /// </summary>
    public interface INotificationTypeRepository
    {
        /// <summary>
        /// List notification types.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<GXNotificationType>> ListAsync(
            ListNotificationType? request = null,
            ListNotificationTypeResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read notification type details.
        /// </summary>
        /// <param name="id">Notification type id.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXNotificationType> ReadAsync(Guid id, Expression<Func<GXNotificationType, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update notification types.
        /// </summary>
        /// <param name="list">List of notification types to update.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXNotificationType> list,
            Expression<Func<GXNotificationType, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete notification types.
        /// </summary>
        /// <param name="list">List of notification types to delete.</param>
        /// <param name="delete">If true, items are deleted permanently instead of being soft-deleted.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> list, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate notification types.
        /// </summary>
        /// <param name="notificationTypes">Notification type(s) to regenerate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RegenerateAsync(IEnumerable<Guid>? notificationTypes,
        CancellationToken cancellationToken = default);

    }
}
