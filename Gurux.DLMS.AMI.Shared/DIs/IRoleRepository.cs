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
    /// This interface is used to handle roles.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle roles.")]
    public interface IRoleRepository
    {
        /// <summary>
        /// List roles.
        /// </summary>
        /// <returns>Roles.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List roles.")]
        Task<IEnumerable<GXRole>> ListAsync(
            ListRoles? request = null,
            ListRolesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read role.
        /// </summary>
        /// <param name="id">Role id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read role.")]
        Task<GXRole> ReadAsync(string id, Expression<Func<GXRole, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read by role name.
        /// </summary>
        /// <param name="name">Role name.</param>
        /// <returns>Operation result.</returns>
        [System.ComponentModel.Description("Read by role name.")]
        Task<GXRole> ReadByNameAsync(string name);

        /// <summary>
        /// Inserts new roles or updates existing ones.
        /// </summary>
        /// <param name="roles">The records to insert or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new roles or updates existing ones.")]
        Task<IEnumerable<string>> UpsertAsync(IEnumerable<GXRole> roles, CancellationToken cancellationToken = default);

        /// <summary>Delete roles.</summary>
        /// <param name="roles">Role(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete roles.")]
        Task DeleteAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously restores the default roles.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>true if the refresh succeeds; otherwise, false.</returns>
        [System.ComponentModel.Description("Asynchronously restores the default roles.")]
        Task<bool> RestoreDefaultRoles(CancellationToken cancellationToken);
    }
}
