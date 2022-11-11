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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// List users.
        /// </summary>
        /// <returns>Users.</returns>
        Task<GXUser[]> ListAsync(
            ClaimsPrincipal User,
            ListUsers? request,
            ListUsersResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read user.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">User id.</param>
        /// <returns>User information.</returns>
        Task<GXUser> ReadAsync(ClaimsPrincipal User, string? id);

        /// <summary>
        /// Update users.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="users">Updated users.</param>
        Task<string[]> UpdateAsync(ClaimsPrincipal User, IEnumerable<GXUser> users);

        /// <summary>
        /// Delete user(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="users">Users to delete.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<string> users);

        /// <summary>
        /// Return users that are in the given role.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="roles">´Searched roles.</param>
        /// <returns>List of used IDs.</returns>
        Task<List<string>> GetUserIdsInRoleAsync(ClaimsPrincipal User, IEnumerable<string> roles);
    }
}
