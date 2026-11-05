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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle notifications.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle notifications.")]
    public interface INotificationRepository
    {
        /// <summary>
        /// List notifications.
        /// </summary>
        /// <returns>Notifications.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List notifications.")]
        Task<IEnumerable<GXNotification>> ListAsync(
            ListNotifications? request = null,
            ListNotificationResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read notification.
        /// </summary>
        /// <param name="id">Notification id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested notification.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read notification.")]
        Task<GXNotification> ReadAsync(Guid id, Expression<Func<GXNotification, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new notifications or updates existing ones.
        /// </summary>
        /// <param name="notifications">Updated notification(s).</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new notifications or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXNotification> notifications,
            Expression<Func<GXNotification, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete notifications.</summary>
        /// <param name="notifications">Notification(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete notifications.</returns>
        [System.ComponentModel.Description("Delete notifications.")]
        Task DeleteAsync(IEnumerable<Guid> notifications, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>Creates missing Automatic notification defaults without changing existing subscriptions.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Identifiers of newly created defaults.</returns>
        Task<IEnumerable<Guid>> EnsureDefaultsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>Restores selected Automatic defaults in place, preserving all unselected subscriptions.</summary>
        /// <param name="ids">Identifiers of the existing automatic notification defaults to restore.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Identifiers of restored defaults.</returns>
        Task<IEnumerable<Guid>> ResetToDefaultsAsync(IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default);
    }
}
