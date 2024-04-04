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
    public interface IScheduleGroupRepository
    {
        /// <summary>
        /// List schedule groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXScheduleGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListScheduleGroups? request,
            ListScheduleGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read schedule group information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Schedule group id.</param>
        /// <returns></returns>
        Task<GXScheduleGroup> ReadAsync(ClaimsPrincipal User, Guid id);


        /// <summary>
        /// Update schedule groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated schedule groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXScheduleGroup> groups,
            Expression<Func<GXScheduleGroup, object?>>? columns = null);

        /// <summary>
        /// Delete schedule group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Schedule groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns schedule groups list where schedule belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>List of schedule groups.</returns>
        Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(ClaimsPrincipal User, Guid scheduleId);

        /// <summary>
        /// Get all users that can access this schedule group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scheduleGroupId">Schedule group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? scheduleGroupId);

        /// <summary>
        /// Get all users that can access schedule groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scheduleGroupIds">Schedule group ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? scheduleGroupIds);
    }
}
