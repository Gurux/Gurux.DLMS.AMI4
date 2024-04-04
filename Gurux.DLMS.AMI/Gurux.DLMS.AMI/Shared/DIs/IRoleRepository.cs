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
    public interface IRoleRepository
    {
        /// <summary>
        /// List roles.
        /// </summary>
        /// <returns>Roles.</returns>
        Task<GXRole[]> ListAsync(
            ClaimsPrincipal user, 
            ListRoles? request, 
            ListRolesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read role.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Role id.</param>
        /// <returns></returns>
        Task<GXRole> ReadAsync(ClaimsPrincipal user, string id);

        /// <summary>
        /// Add role(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="roles">Added role(s).</param>
        Task<string[]> AddAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXRole> roles);

        /// <summary>
        /// Delete role(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="roles">Role(s) to delete.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<string> roles);
    }
}
