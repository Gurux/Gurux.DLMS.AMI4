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
        /// <returns>User groups.</returns>
        Task<GXAgentGroup[]> ListAsync(
            ClaimsPrincipal User, 
            ListAgentGroups? request, 
            ListAgentGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Agent id.</param>
        /// <returns></returns>
        Task<GXAgentGroup> ReadAsync(ClaimsPrincipal User, Guid id);


        /// <summary>
        /// Update agent groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated agent groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXAgentGroup> groups,
            Expression<Func<GXAgentGroup, object?>>? columns = null);

        /// <summary>
        /// Delete agent group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">User agent to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Get all users that can access this agent group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentGroupId">Agent group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? agentGroupId);

        /// <summary>
        /// Get all users that can access agent groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentGroupIds">Agent group ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? agentGroupIds);
    }
}
