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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle agent groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle agent groups.")]
    public interface IAgentGroupRepository
    {
        /// <summary>
        /// List agent groups.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>User groups.</returns>
        [System.ComponentModel.Description("List agent groups.")]
        Task<IEnumerable<GXAgentGroup>> ListAsync(
            ListAgentGroups? request = null,
            ListAgentGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads an agent group by its identifier.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>A task whose result contains the requested agent group.</returns>
        [System.ComponentModel.Description("Reads an agent group by its identifier.")]
        Task<GXAgentGroup> ReadAsync(Guid id, Expression<Func<GXAgentGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new agent groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated agent groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new agent groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAgentGroup> groups,
            Expression<Func<GXAgentGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>Delete agent groups.</summary>
        /// <param name="groups">User agent to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>A task representing completion of the request to delete agent groups.</returns>
        [System.ComponentModel.Description("Delete agent groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        [System.ComponentModel.Description("Get all users that can access this agent group.")]
        Task<List<string>> GetUsersAsync(Guid? agentGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access agent groups.
        /// </summary>
        /// <param name="agentGroupIds">Agent group ids.</param>
        /// <param name="cancellationToken">The cancellation token.</param>  
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        [System.ComponentModel.Description("Get all users that can access agent groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentGroupIds,
            CancellationToken cancellationToken = default);
    }
}
