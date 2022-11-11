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

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    public interface IUserGroupRepository
    {
        /// <summary>
        /// List user groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXUserGroup[]> ListAsync(
            ClaimsPrincipal user, 
            ListUserGroups? request, 
            ListUserGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read user group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">User group id.</param>
        /// <returns></returns>
        Task<GXUserGroup> ReadAsync(ClaimsPrincipal user, Guid id);


        /// <summary>
        /// Update user groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Updated user groups.</param>
        Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXUserGroup> groups);

        /// <summary>
        /// Delete user group(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">User groups to delete.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> groups);

        /// <summary>
        /// Get all users that can access this user group. 
        /// </summary>
        /// <param name="user">Current User.</param>
        /// <param name="groupId">User group ID.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId);

        /// <summary>
        /// Get all users that can access user groups. 
        /// </summary>
        /// <param name="user">Current User.</param>
        /// <param name="groupId">User group IDs.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? groupId);

        /// <summary>
        /// Add user to user group.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="group">Group ID of the group where the user is added.</param>
        void AddUserToGroup(string userId, Guid group);

        /// <summary>
        /// Add user to user groups.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="group">Group ID of the group where the user is added.</param>
        void AddUserToGroups(string userId, IEnumerable<Guid> groups);

        /// <summary>
        /// Returns list from user groups where user belongs.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns></returns>
        Task<List<GXUserGroup>> GetJoinedUserGroups(string userId);

        /// <summary>
        /// Returns default user groups for the user.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="userId">User ID.</param>
        /// <returns>List of user groups.</returns>
        Task<List<GXUserGroup>> GetDefaultUserGroups(ClaimsPrincipal user, string userId);
    }
}
