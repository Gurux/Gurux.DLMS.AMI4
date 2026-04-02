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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements agent repository that can be called from the agent script.
    /// </summary>
    class GXAgentRepository : IAgentRepository
    {
        /// <inheritdoc/>
        public Task ClearCache(Guid[]? Ids, string[] names)
        {
            //The agent doesn't need this.
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> devices, bool delete)
        {
            RemoveAgent req = new RemoveAgent() { Ids = devices.ToArray(), Delete = delete };
            _ = await GXAgentWorker.client.PostAsJson<RemoveAgentResponse>("/api/Agent/Delete", req);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(Guid? deviceId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXAgent[]> ListAsync(
            ListAgents? request,
            ListAgentsResponse? response,
            CancellationToken cancellationToken)
        {
            ListAgentsResponse? ret = await GXAgentWorker.client.PostAsJson<ListAgentsResponse>("/api/Agent/List",
                request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Agents = ret.Agents;
            }
            return ret.Agents;
        }

        public Task<GXAgent[]> ListInstallersAsync(
                ListAgentInstallers? request,
                bool includeRemoved,
                ListAgentInstallersResponse? response)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXAgent> ReadAsync(Guid id)
        {
            return await Helpers.GetAsync<GXAgent>(string.Format("/api/Agent/?Id={0}", id));
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(IEnumerable<GXAgent> devices)
        {
            UpdateAgent req = new UpdateAgent() { Agents = devices.ToArray() };
            return (await GXAgentWorker.client.PostAsJson<UpdateAgentResponse>("/api/Agent/Update", req)).AgentIds;
        }

        /// <inheritdoc/>
        public Task<Guid[]> UpdateAsync(IEnumerable<GXAgent> agents, Expression<Func<GXAgent, object?>> columns)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpdateStatusAsync(Guid agentId, string connectionInfo, AgentStatus status, string? data)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpgradeAsync(IEnumerable<GXAgent> agents)
        {
            throw new NotImplementedException();
        }
    }
}