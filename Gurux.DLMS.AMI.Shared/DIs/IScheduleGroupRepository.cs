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
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read schedule group details.")]
        Task<GXScheduleGroup> ReadAsync(Guid id, Expression<Func<GXScheduleGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new schedule groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated schedule groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new schedule groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScheduleGroup> groups,
            Expression<Func<GXScheduleGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete schedule groups.</summary>
        /// <param name="groups">Schedule groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
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
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this schedule group.")]
        Task<List<string>> GetUsersAsync(Guid? scheduleGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access schedule groups.
        /// </summary>
        /// <param name="scheduleGroupIds">Schedule group ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access schedule groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? scheduleGroupIds, CancellationToken cancellationToken = default);
    }
}
