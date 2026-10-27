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
    [System.ComponentModel.Description("This public interface is used to handle listed notification types.")]
    public interface INotificationTypeRepository
    {
        /// <summary>
        /// List notification types.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("List notification types.")]
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
        [System.ComponentModel.Description("Read notification type details.")]
        Task<GXNotificationType> ReadAsync(Guid id, Expression<Func<GXNotificationType, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new notification types or updates existing ones.
        /// </summary>
        /// <param name="list">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new notification types or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
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
        [System.ComponentModel.Description("Delete notification types.")]
        Task DeleteAsync(IEnumerable<Guid> list, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>Create missing built-in notification types without modifying existing rows.</summary>
        [System.ComponentModel.Description("Create missing built-in notification types without modifying existing rows.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restore selected built-ins in place. Empty selections are rejected.</summary>
        [System.ComponentModel.Description("Restore selected built-ins in place. Empty selections are rejected.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
