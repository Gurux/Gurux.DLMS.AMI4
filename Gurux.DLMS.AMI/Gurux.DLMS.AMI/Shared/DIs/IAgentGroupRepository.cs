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
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle agent groups.
    /// </summary>
    public interface IAgentGroupRepository
    {
        /// <summary>
        /// List agent groups.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>User groups.</returns>
        Task<IEnumerable<GXAgentGroup>> ListAsync(
            ListAgentGroups? request = null,
            ListAgentGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>Operation result.</returns>
        Task<GXAgentGroup> ReadAsync(Guid id, Expression<Func<GXAgentGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update agent groups.
        /// </summary>
        /// <param name="groups">Updated agent groups.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXAgentGroup> groups,
            Expression<Func<GXAgentGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete agent group(s).
        /// </summary>
        /// <param name="groups">User agent to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>Operation result.</returns>
        Task<List<string>> GetUsersAsync(Guid? agentGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access agent groups.
        /// </summary>
        /// <param name="agentGroupIds">Agent group ids.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>Operation result.</returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentGroupIds,
            CancellationToken cancellationToken = default);
    }
}
