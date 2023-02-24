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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle component view groups.
    /// </summary>
    public interface IComponentViewGroupRepository
    {
        /// <summary>
        /// List component view groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXComponentViewGroup[]> ListAsync(
            ClaimsPrincipal User, 
            ListComponentViewGroups? request, 
            ListComponentViewGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read component view group information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Component view id.</param>
        /// <returns></returns>
        Task<GXComponentViewGroup> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update component view groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated component view groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXComponentViewGroup> groups,
            Expression<Func<GXComponentViewGroup, object?>>? columns = null);

        /// <summary>
        /// Delete component view group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Block component view to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns component view groups list where component view belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="componentViewId">Component view ID</param>
        /// <returns>List of component view groups.</returns>
        Task<List<GXComponentViewGroup>> GetJoinedComponentViewGroups(ClaimsPrincipal User, Guid componentViewId);


        /// <summary>
        /// Get all users that can access this component view group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="componentViewGroupId">Component view group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? componentViewGroupId);

        /// <summary>
        /// Get all users that can access component view groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="componentViewGroupIds">Component view group ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? componentViewGroupIds);

    }
}
