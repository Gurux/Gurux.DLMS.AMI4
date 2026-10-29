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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle used groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle used groups.")]
    public interface IUserGroupRepository
    {
        /// <summary>
        /// List user groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List user groups.")]
        Task<IEnumerable<GXUserGroup>> ListAsync(
            ListUserGroups? request = null,
            ListUserGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user group details.
        /// </summary>
        /// <param name="id">User group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested user group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read user group details.")]
        Task<GXUserGroup> ReadAsync(Guid id, Expression<Func<GXUserGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new user groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated user groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new user groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXUserGroup> groups,
            Expression<Func<GXUserGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete user groups.</summary>
        /// <param name="groups">User groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete user groups.</returns>
        [System.ComponentModel.Description("Delete user groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this user group. 
        /// </summary>
        /// <param name="groupId">User group ID.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this user group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access user groups. 
        /// </summary>
        /// <param name="groupId">User group IDs.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access user groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add user to user groups.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="groups">Group ID of the group where the user is added.</param>
        /// <returns>A task representing completion of the request to add user to user groups.</returns>
        [System.ComponentModel.Description("Add user to user groups.")]
        Task AddUserToGroupsAsync(string userId, IEnumerable<Guid> groups);

        /// <summary>
        /// Returns list from user groups where user belongs.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>A task whose result contains the user groups to which the specified user belongs.</returns>
        [System.ComponentModel.Description("Returns list from user groups where user belongs.")]
        Task<List<GXUserGroup>> GetJoinedUserGroupsAsync(string userId);

        /// <summary>
        /// Returns default user groups for the user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <returns>List of user groups.</returns>
        [System.ComponentModel.Description("Returns default user groups for the user.")]
        Task<List<GXUserGroup>> GetDefaultUserGroupsAsync(string userId);
    }
}
