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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle module groups.
    /// </summary>
    public interface IModuleGroupRepository
    {
        /// <summary>
        /// List module groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXModuleGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListModuleGroups? request,
            ListModuleGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read module group information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Module group id.</param>
        /// <returns></returns>
        Task<GXModuleGroup> ReadAsync(ClaimsPrincipal User, Guid id);


        /// <summary>
        /// Update module groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated module groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXModuleGroup> groups,
            Expression<Func<GXModuleGroup, object?>>? columns = null);

        /// <summary>
        /// Delete module group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Module groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> groups,
            bool delete);

        /// <summary>
        /// Returns module groups list where module belongs.
        /// </summary>
        /// <param name="moduleId">Module ID</param>
        /// <returns>List of module groups.</returns>
        Task<List<GXModuleGroup>> GetJoinedModuleGroups(string moduleId);

        /// <summary>
        /// Returns list of users that can access this module group.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groupId">Module group Id.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId);

        /// <summary>
        /// Returns list of users that can access module groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groupIds">Module group Ids.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? groupIds);
    }
}
