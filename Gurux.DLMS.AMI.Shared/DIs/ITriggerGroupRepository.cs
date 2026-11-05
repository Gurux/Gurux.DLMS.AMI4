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
    /// This interface is used to handle trigger groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle trigger groups.")]
    public interface ITriggerGroupRepository
    {
        /// <summary>
        /// List trigger groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List trigger groups.")]
        Task<IEnumerable<GXTriggerGroup>> ListAsync(
            ListTriggerGroups? request = null,
            ListTriggerGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read trigger group details.
        /// </summary>
        /// <param name="id">Trigger group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested trigger group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read trigger group details.")]
        Task<GXTriggerGroup> ReadAsync(Guid id, Expression<Func<GXTriggerGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new trigger groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated trigger groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new trigger groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXTriggerGroup> groups,
            Expression<Func<GXTriggerGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete trigger groups.</summary>
        /// <param name="groups">Trigger groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete trigger groups.</returns>
        [System.ComponentModel.Description("Delete trigger groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns trigger groups list where trigger belongs.
        /// </summary>
        /// <param name="triggerId">Trigger ID</param>
        /// <returns>List of trigger groups.</returns>
        [System.ComponentModel.Description("Returns trigger groups list where trigger belongs.")]
        Task<List<GXTriggerGroup>> GetJoinedTriggerGroups(Guid triggerId);

        /// <summary>
        /// Get all users that can access trigger group.
        /// </summary>
        /// <param name="groupId">Trigger group id.</param>
        /// <returns>User Ids that can access this script.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access trigger group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access trigger groups.
        /// </summary>
        /// <param name="groupIds">Trigger group ids.</param>
        /// <returns>User Ids that can access this scripts.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access trigger groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default);
    }
}
