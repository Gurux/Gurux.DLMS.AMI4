using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
using System.Linq.Expressions;

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLog>> ListAsync(
            ListKeyManagementLogs? request = null,
            ListKeyManagementLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read key management log details.
        /// </summary>
        /// <param name="id">KeyManagement log id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>KeyManagement information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear key management logs.
        /// </summary>
        /// <param name="keys">Keys of the logs to clear.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearAsync(IEnumerable<Guid>? keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add key management logs.
        /// </summary>
        /// <param name="type">Key management log type.</param>
        /// <param name="logs">New key management logs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task AddAsync(string type, IEnumerable<GXLog> logs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Key management log type.</param>
        /// <param name="key">Key management.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> AddAsync(string type, GXKeyManagement key, Exception ex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close key management log(s).
        /// </summary>
        /// <param name="errors">Errors to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid> errors, CancellationToken cancellationToken = default);
    }
}
