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
    /// This interface is used to handle script groups.
    /// </summary>
    public interface IScriptGroupRepository
    {
        /// <summary>
        /// List script groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXScriptGroup[]> ListAsync(ClaimsPrincipal user,
            ListScriptGroups? request,
            ListScriptGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read script group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Script group id.</param>
        /// <returns></returns>
        Task<GXScriptGroup> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update script groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Updated script groups.</param>
        Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXScriptGroup> groups);

        /// <summary>
        /// Delete script group(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Script groups to delete.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> groups);

        /// <summary>
        /// Returns script groups list where script belongs.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="scriptId">Script ID</param>
        /// <returns>List of script groups.</returns>
        Task<List<GXScriptGroup>> GetJoinedScriptGroups(ClaimsPrincipal user, Guid scriptId);

        /// <summary>
        /// Get all users that can access this script group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groupId">Script group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId);

        /// <summary>
        /// Get all users that can access script groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groupId">Script group ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupId);
    }
}
