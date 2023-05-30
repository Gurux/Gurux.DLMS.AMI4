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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle key managements.
    /// </summary>
    public interface IKeyManagementRepository
    {
        /// <summary>
        /// Get all users that can access this key management.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">KeyManagement id.</param>
        /// <returns>User Ids that can access this key management.</returns>
        /// <remarks>
        /// If key management is null all the users who can access the key managements are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Get all users that can access this key management.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="keyIds">KeyManagement ids.</param>
        /// <returns>User Ids that can access this key managements.</returns>
        /// <remarks>
        /// If key management is null all the users who can access the key managements are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid> keyIds);


        /// <summary>
        /// List key managements.
        /// </summary>
        /// <returns>KeyManagements.</returns>
        Task<GXKeyManagement[]> ListAsync(
            ClaimsPrincipal user,
            ListKeyManagements? request,
            ListKeyManagementsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read key management.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Key management id.</param>
        /// <returns>Read key management.</returns>
        Task<GXKeyManagement> ReadAsync(
            ClaimsPrincipal user,
            Guid id);

        /// <summary>
        /// Update key management(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="keys">Updated key management(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXKeyManagement> keys,
            Expression<Func<GXKeyManagement, object?>>? columns = null);

        /// <summary>
        /// Delete key management(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="keys">Key management(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> keys, bool delete);
    }
}
