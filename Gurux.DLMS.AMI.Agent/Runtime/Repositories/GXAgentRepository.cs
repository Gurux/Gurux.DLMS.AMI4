// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
    class GXAgentRepository(HttpClient http) : IAgentRepository
    {
        /// <inheritdoc/>
        public Task ClearCache(IEnumerable<Guid>? Ids, IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
            //The agent doesn't need this.
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> devices, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            RemoveAgent req = new() { Ids = [.. devices], Delete = delete };
            _ = await http.PostAsJson<RemoveAgentResponse>("/api/Agent/Delete", req, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(Guid? deviceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXAgent>> ListAsync(
            ListAgents? request,
            ListAgentsResponse? response,
            CancellationToken cancellationToken)
        {
            ListAgentsResponse? ret = await http.PostAsJson<ListAgentsResponse>("/api/Agent/List",
                request ?? new ListAgents(), cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Agents = ret.Agents;
            }
            return ret?.Agents ?? [];
        }

        public Task<IEnumerable<GXAgent>> ListInstallersAsync(
                ListAgentInstallers? request,
                bool includeRemoved,
                ListAgentInstallersResponse? response,
                CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXAgent> ReadAsync(Guid id,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            return await http.GetAsJsonAsync<GXAgent>(string.Format("/api/Agent/?Id={0}", id), cancellationToken)
                ?? throw new KeyNotFoundException($"Item {id} was not returned.");
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXAgent> items,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default)
            => UpsertAsync(items, columns, cancellationToken);

        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXAgent> devices,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            UpdateAgent req = new() { Agents = [.. devices] };
            return (await http.PostAsJson<UpdateAgentResponse>("/api/Agent/Update", req, cancellationToken))?.AgentIds
                ?? throw new HttpRequestException("Update response was not returned.");
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(IEnumerable<GXAgent> agents, Expression<Func<GXAgent, object?>> columns)
        {
            var projection = Expression.Lambda<Func<GXAgent, object>>(columns.Body, columns.Parameters);
            return [.. (await UpsertAsync(agents, projection))];
        }

        /// <inheritdoc/>
        public Task UpdateStatusAsync(Guid agentId, string? connectionInfo, AgentStatus status, string? data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpgradeAsync(IEnumerable<GXAgent> agents, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
