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
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle gateway groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle gateway groups.")]
    public interface IGatewayGroupRepository
    {
        /// <summary>
        /// List gateway groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List gateway groups.")]
        Task<IEnumerable<GXGatewayGroup>> ListAsync(
            ListGatewayGroups? request = null,
            ListGatewayGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a gateway group by its identifier.
        /// </summary>
        /// <param name="id">Gateway id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested gateway group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Reads a gateway group by its identifier.")]
        Task<GXGatewayGroup> ReadAsync(Guid id, Expression<Func<GXGatewayGroup, object>>? columns = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Inserts new gateway groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated gateway groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new gateway groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXGatewayGroup> groups,
            Expression<Func<GXGatewayGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete gateway groups.</summary>
        /// <param name="groups">Gateway groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete gateway groups.</returns>
        [System.ComponentModel.Description("Delete gateway groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this gateway group.
        /// </summary>
        /// <param name="gatewayGroupId">Gateway group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this gateway group.")]
        Task<List<string>> GetUsersAsync(Guid? gatewayGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access gateway groups.
        /// </summary>
        /// <param name="gatewayGroupIds">Gateway group ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access gateway groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? gatewayGroupIds, CancellationToken cancellationToken = default);
    }
}
