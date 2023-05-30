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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle key management logs.
    /// </summary>
    public interface IKeyManagementLogRepository
    {
        /// <summary>
        /// List key management logs.
        /// </summary>
        /// <returns>List of key management logs.</returns>
        Task<GXKeyManagementLog[]> ListAsync(
            ClaimsPrincipal user, 
            ListKeyManagementLogs? request, 
            ListKeyManagementLogsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read key management log information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">KeyManagement log id.</param>
        /// <returns>KeyManagement information.</returns>
        Task<GXKeyManagementLog> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear key management logs.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, Guid[]? keys);

        /// <summary>
        /// Add key management logs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="logs">New key management logs.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXKeyManagementLog> logs);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="key">Key management.</param>
        /// <param name="ex">Exception.</param>
        Task<GXKeyManagementLog> AddAsync(ClaimsPrincipal User, GXKeyManagement key, Exception ex);

        /// <summary>
        /// Close key management log(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">Errors to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors);
    }
}
