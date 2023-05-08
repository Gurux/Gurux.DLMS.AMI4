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
using Gurux.DLMS.AMI.Shared.DTOs;
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
        /// <returns>Agents.</returns>
        Task<GXAgent[]> ListAsync(
            ClaimsPrincipal User,
            ListAgents? request,
            ListAgentsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read agent.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Agent id.</param>
        /// <returns></returns>
        Task<GXAgent> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update agent(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agents">Updated agent(s).</param>
        /// <param name="columns">Updated column(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXAgent> agents, 
            Expression<Func<GXAgent, object?>>? columns = null);

        /// <summary>
        /// Delete agent(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agents">Agent(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> agents, bool delete);

        /// <summary>
        /// Get all users that can access this agent.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentId">Agent id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? agentId);

        /// <summary>
        /// Get all users that can access agents.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentIds">Agent ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? agentIds);

        /// <summary>
        /// Agent updates the status.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="status">Agent status</param>
        /// <param name="data">Optional data. List of available serial ports.</param>
        Task UpdateStatusAsync(ClaimsPrincipal User, Guid agentId, AgentStatus status, string? data);

        /// <summary>
        /// Upgrade agent version.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agents">Upgraded agents.</param>
        Task UpgradeAsync(ClaimsPrincipal User, IEnumerable<GXAgent> agents);

        /// <summary>
        /// List agent installers.
        /// </summary>
        /// <returns>Agent installers.</returns>
        Task<GXAgent[]> ListInstallersAsync(
            ClaimsPrincipal User,
            ListAgentInstallers? request,
            bool includeRemoved,
            ListAgentInstallersResponse? response);

    }
}
