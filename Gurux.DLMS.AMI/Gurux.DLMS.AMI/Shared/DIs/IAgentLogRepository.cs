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

using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle agent logs.
    /// </summary>
    public interface IAgentLogRepository
    {
        /// <summary>
        /// List agent logs.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of agent logs.</returns>
        Task<IEnumerable<GXLog>> ListAsync(
            ListAgentLogs? request = null,
            ListAgentLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent log details.
        /// </summary>
        /// <param name="id">Agent log id.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Agent log information.</returns>
        Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear agent logs.
        /// </summary>
        /// <param name="agents">The agent IDs to clear logs for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearAsync(IEnumerable<Guid>? agents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add agent logs.
        /// </summary>
        /// <param name="type">Agent log type.</param>
        /// <param name="logs">New logs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> AddAsync(string type, IEnumerable<GXLog> logs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Agent log type.</param>
        /// <param name="agent">Agent.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> AddAsync(string type, GXAgent agent, Exception ex,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Close agent log(s).
        /// </summary>
        /// <param name="logs">Logs to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid> logs,
            CancellationToken cancellationToken = default);
    }
}
