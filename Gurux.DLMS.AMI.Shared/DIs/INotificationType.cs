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
        /// <returns>A task whose result contains the matching notification types.</returns>
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
        /// <returns>A task whose result contains the requested notification type.</returns>
        [System.ComponentModel.Description("Read notification type details.")]
        Task<GXNotificationType> ReadAsync(Guid id, Expression<Func<GXNotificationType, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new notification types or updates existing ones.
        /// </summary>
        /// <param name="list">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
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
        /// <returns>A task representing completion of the request to delete notification types.</returns>
        [System.ComponentModel.Description("Delete notification types.")]
        Task DeleteAsync(IEnumerable<Guid> list, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>Create missing built-in notification types without modifying existing rows.</summary>
        /// <returns>A task representing completion of the request to create missing built-in notification types without modifying existing rows.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Create missing built-in notification types without modifying existing rows.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restore selected built-ins in place. Empty selections are rejected.</summary>
        /// <returns>A task representing completion of the request to restore selected built-ins in place. Empty selections are rejected.</returns>
        /// <param name="ids">Identifiers of existing built-in resources to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Restore selected built-ins in place. Empty selections are rejected.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
