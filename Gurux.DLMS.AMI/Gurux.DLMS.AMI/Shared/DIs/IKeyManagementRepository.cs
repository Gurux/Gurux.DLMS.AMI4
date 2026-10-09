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
        /// <param name="id">KeyManagement id.</param>
        /// <returns>User Ids that can access this key management.</returns>
        /// <remarks>
        /// If key management is null all the users who can access the key managements are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this key management.
        /// </summary>
        /// <param name="keyIds">KeyManagement ids.</param>
        /// <returns>User Ids that can access this key managements.</returns>
        /// <remarks>
        /// If key management is null all the users who can access the key managements are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid> keyIds, CancellationToken cancellationToken = default);


        /// <summary>
        /// List key managements.
        /// </summary>
        /// <returns>KeyManagements.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXKeyManagement>> ListAsync(
            ListKeyManagements? request = null,
            ListKeyManagementsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read key management.
        /// </summary>
        /// <param name="id">Key management id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Read key management.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXKeyManagement> ReadAsync(Guid id, Expression<Func<GXKeyManagement, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update key management(s).
        /// </summary>
        /// <param name="keys">Updated key management(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXKeyManagement> keys,
            Expression<Func<GXKeyManagement, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete key management(s).
        /// </summary>
        /// <param name="keys">Key management(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> keys, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);
    }
}
