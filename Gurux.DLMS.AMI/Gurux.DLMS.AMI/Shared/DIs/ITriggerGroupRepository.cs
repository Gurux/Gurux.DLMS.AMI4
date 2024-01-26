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
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle trigger groups.
    /// </summary>
    public interface ITriggerGroupRepository
    {
        /// <summary>
        /// List trigger groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXTriggerGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListTriggerGroups? request,
            ListTriggerGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read trigger group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Trigger group id.</param>
        /// <returns></returns>
        Task<GXTriggerGroup> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update trigger groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated trigger groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXTriggerGroup> groups,
            Expression<Func<GXTriggerGroup, object?>>? columns = null);

        /// <summary>
        /// Delete trigger group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Trigger groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns trigger groups list where trigger belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="triggerId">Trigger ID</param>
        /// <returns>List of trigger groups.</returns>
        Task<List<GXTriggerGroup>> GetJoinedTriggerGroups(ClaimsPrincipal User, Guid triggerId);

        /// <summary>
        /// Get all users that can access trigger group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groupId">Trigger group id.</param>
        /// <returns>User Ids that can access this script.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId);

        /// <summary>
        /// Get all users that can access trigger groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groupIds">Trigger group ids.</param>
        /// <returns>User Ids that can access this scripts.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupIds);

    }
}
