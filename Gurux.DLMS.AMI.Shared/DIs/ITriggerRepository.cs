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
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle triggers.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle triggers.")]
    public interface ITriggerRepository
    {
        /// <summary>
        /// List triggers.
        /// </summary>
        /// <returns>Triggers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List triggers.")]
        Task<IEnumerable<GXTrigger>> ListAsync(
            ListTriggers? request = null,
            ListTriggersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read trigger.
        /// </summary>
        /// <param name="id">Trigger id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested trigger.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read trigger.")]
        Task<GXTrigger> ReadAsync(Guid id, Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new triggers or updates existing ones.
        /// </summary>
        /// <param name="triggers">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new triggers or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete triggers.</summary>
        /// <param name="triggers">Trigger(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete triggers.</returns>
        [System.ComponentModel.Description("Delete triggers.")]
        Task DeleteAsync(IEnumerable<Guid> triggers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this trigger.
        /// </summary>
        /// <param name="triggerId">Trigger id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this trigger.")]
        Task<List<string>> GetUsersAsync(Guid? triggerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access triggers.
        /// </summary>
        /// <param name="Ids">Trigger ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access triggers.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>Add missing registered built-in defaults while preserving existing rows.</summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to add missing registered built-in defaults while preserving existing rows.</returns>
        [System.ComponentModel.Description("Add missing registered built-in defaults while preserving existing rows.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
    }
}
