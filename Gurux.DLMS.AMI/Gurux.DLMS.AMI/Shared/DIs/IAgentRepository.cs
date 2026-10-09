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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle agents.
    /// </summary>
    public interface IAgentRepository
    {
        /// <summary>
        /// List agents.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Agents.</returns>
        Task<IEnumerable<GXAgent>> ListAsync(
            ListAgents? request = null,
            ListAgentsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXAgent> ReadAsync(Guid id,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update agent(s).
        /// </summary>
        /// <param name="agents">Updated agent(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXAgent> agents,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete agent(s).
        /// </summary>
        /// <param name="agents">Agent(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> agents, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this agent.
        /// </summary>
        /// <param name="agentId">Agent id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access agents.
        /// </summary>
        /// <param name="agentIds">Agent ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Agent updates the status.
        /// </summary>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="connectionInfo">Connection info e.g. IP address.</param>
        /// <param name="status">Agent status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="data">Optional data. List of available serial ports.</param>
        Task UpdateStatusAsync(Guid agentId, string? connectionInfo, AgentStatus status, string? data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upgrade agent version.
        /// </summary>
        /// <param name="agents">Upgraded agents.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpgradeAsync(IEnumerable<GXAgent> agents, CancellationToken cancellationToken = default);

        /// <summary>
        /// List agent installers.
        /// </summary>
        /// <returns>Agent installers.</returns>
        /// <param name="request">The request parameters.</param>
        /// <param name="includeRemoved">Are removed agents included.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<GXAgent>> ListInstallersAsync(
            ListAgentInstallers? request = null,
            bool includeRemoved = false,
            ListAgentInstallersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear agents' cache.
        /// </summary>
        /// <param name="Ids">Agent IDs.</param>
        /// <param name="names">Cache names to clear</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearCache(IEnumerable<Guid>? Ids, IEnumerable<string> names,
            CancellationToken cancellationToken = default);
    }
}
