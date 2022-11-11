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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle block groups.
    /// </summary>
    public interface IBlockGroupRepository
    {
        /// <summary>
        /// List block groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXBlockGroup[]> ListAsync(
            ClaimsPrincipal user, 
            ListBlockGroups? request, 
            ListBlockGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read block group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Block group id.</param>
        /// <returns></returns>
        Task<GXBlockGroup> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update block groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Updated block groups.</param>
        Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXBlockGroup> groups);

        /// <summary>
        /// Delete block group(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Block groups to delete.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> groups);

        /// <summary>
        /// Returns block groups list where block belongs.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blockId">Block ID</param>
        /// <returns>List of block groups.</returns>
        Task<List<GXBlockGroup>> GetJoinedBlockGroups(ClaimsPrincipal user, Guid blockId);

        /// <summary>
        /// Get all users that can access this block group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="blockGroupId">Block group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? blockGroupId);

        /// <summary>
        /// Get all users that can access block group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="blockGroupIds">Agent Block ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? blockGroupIds);
    }
}
