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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle schedule groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle schedule groups.")]
    public interface IScheduleGroupRepository
    {
        /// <summary>
        /// List schedule groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List schedule groups.")]
        Task<IEnumerable<GXScheduleGroup>> ListAsync(
            ListScheduleGroups? request = null,
            ListScheduleGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read schedule group details.
        /// </summary>
        /// <param name="id">Schedule group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested schedule group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read schedule group details.")]
        Task<GXScheduleGroup> ReadAsync(Guid id, Expression<Func<GXScheduleGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new schedule groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated schedule groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new schedule groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScheduleGroup> groups,
            Expression<Func<GXScheduleGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete schedule groups.</summary>
        /// <param name="groups">Schedule groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete schedule groups.</returns>
        [System.ComponentModel.Description("Delete schedule groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns schedule groups list where schedule belongs.
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>List of schedule groups.</returns>
        [System.ComponentModel.Description("Returns schedule groups list where schedule belongs.")]
        Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(Guid scheduleId);

        /// <summary>
        /// Get all users that can access this schedule group.
        /// </summary>
        /// <param name="scheduleGroupId">Schedule group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this schedule group.")]
        Task<List<string>> GetUsersAsync(Guid? scheduleGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access schedule groups.
        /// </summary>
        /// <param name="scheduleGroupIds">Schedule group ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access schedule groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? scheduleGroupIds, CancellationToken cancellationToken = default);
    }
}
