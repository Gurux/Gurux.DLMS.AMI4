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
    [System.ComponentModel.Description("This interface is used to handle agents.")]
    public interface IAgentRepository
    {
        /// <summary>
        /// List agents.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Agents.</returns>
        [System.ComponentModel.Description("List agents.")]
        Task<IEnumerable<GXAgent>> ListAsync(
            ListAgents? request = null,
            ListAgentsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested agent.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read agent.")]
        Task<GXAgent> ReadAsync(Guid id,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new agents or updates existing ones.
        /// </summary>
        /// <param name="agents">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new agents or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAgent> agents,
            Expression<Func<GXAgent, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>Delete agents.</summary>
        /// <param name="agents">Agent(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete agents.</returns>
        [System.ComponentModel.Description("Delete agents.")]
        Task DeleteAsync(IEnumerable<Guid> agents, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this agent.
        /// </summary>
        /// <param name="agentId">Agent id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this agent.")]
        Task<List<string>> GetUsersAsync(Guid? agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access agents.
        /// </summary>
        /// <param name="agentIds">Agent ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access agents.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Agent updates the status.
        /// </summary>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="connectionInfo">Connection info e.g. IP address.</param>
        /// <param name="status">Agent status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="data">Optional data. List of available serial ports.</param>
        /// <returns>A task representing completion of the request to update the agent status.</returns>
        [System.ComponentModel.Description("Agent updates the status.")]
        Task UpdateStatusAsync(Guid agentId, string? connectionInfo, AgentStatus status, string? data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upgrade agent version.
        /// </summary>
        /// <param name="agents">Upgraded agents.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to upgrade agent version.</returns>
        [System.ComponentModel.Description("Upgrade agent version.")]
        Task UpgradeAsync(IEnumerable<GXAgent> agents, CancellationToken cancellationToken = default);

        /// <summary>
        /// List agent installers.
        /// </summary>
        /// <returns>Agent installers.</returns>
        /// <param name="request">The request parameters.</param>
        /// <param name="includeRemoved">Are removed agents included.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("List agent installers.")]
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
        /// <returns>A task representing completion of the request to clear agents' cache.</returns>
        [System.ComponentModel.Description("Clear agents' cache.")]
        Task ClearCache(IEnumerable<Guid>? Ids, IEnumerable<string> names,
            CancellationToken cancellationToken = default);
    }
}
