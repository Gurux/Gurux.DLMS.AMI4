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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXUser>> ListAsync(
            ListUsers? request = null,
            ListUsersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>User information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXUser> ReadAsync(string? id, Expression<Func<GXUser, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update users.
        /// </summary>
        /// <param name="users">Updated users.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<string>> UpdateAsync(
            IEnumerable<GXUser> users,
            Expression<Func<GXUser, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user(s).
        /// </summary>
        /// <param name="users">Users to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<string> users, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return users that are in the given role.
        /// </summary>
        /// <param name="roles">Searched roles.</param>
        /// <returns>List of used IDs.</returns>
        Task<List<string>> GetUserIdsInRoleAsync(IEnumerable<string> roles);
    }
}
