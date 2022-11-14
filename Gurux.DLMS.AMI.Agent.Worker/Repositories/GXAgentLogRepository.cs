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

using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements device log repository that can be called from the agent script.
    /// </summary>
    class GXAgentLogRepository : IAgentLogRepository
    {
        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal? User, IEnumerable<GXAgentLog> logs)
        {
            AddAgentLog req = new AddAgentLog() { Logs = logs.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/AgentLog/Add", req);
        }

        /// <inheritdoc/>
        public Task<GXAgentLog> AddAsync(ClaimsPrincipal? User, GXAgent device, Exception ex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task ClearAsync(ClaimsPrincipal User, Guid[] agents)
        {
            ClearAgentLogs req = new ClearAgentLogs()
            {
                Agents = agents != null ? agents.ToArray() : null
            };
            _ = await GXAgentWorker.client.PostAsJson<ClearAgentLogsResponse>("/api/AgentLog/Clear", req);
        }

        /// <inheritdoc/>
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> logs)
        {
            CloseAgentLog req = new CloseAgentLog() { Logs = logs.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/AgentLog/Close", req);
        }

        /// <inheritdoc/>
        public async Task<GXAgentLog[]> ListAsync(
            ClaimsPrincipal User,
            ListAgentLogs? request,
            ListAgentLogsResponse? response,
            CancellationToken cancellationToken)
        {
            ListAgentLogsResponse? ret = await GXAgentWorker.client.PostAsJson<ListAgentLogsResponse>("/api/AgentLog/List",
                request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Logs = ret.Logs;
            }
            return ret.Logs;
        }

        /// <inheritdoc/>
        public Task<GXAgentLog> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return Helpers.GetAsync<GXAgentLog>(string.Format("/api/AgentLog/?Id={0}", id));
        }
    }
}