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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle component view groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle component view groups.")]
    public interface IComponentViewGroupRepository
    {
        /// <summary>
        /// List component view groups.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>User groups.</returns>
        [System.ComponentModel.Description("List component view groups.")]
        Task<IEnumerable<GXComponentViewGroup>> ListAsync(
            ListComponentViewGroups? request = null,
            ListComponentViewGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read component view group details.
        /// </summary>
        /// <param name="id">Component view id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested component view group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read component view group details.")]
        Task<GXComponentViewGroup> ReadAsync(Guid id, Expression<Func<GXComponentViewGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new component view groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated component view groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new component view groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(

            IEnumerable<GXComponentViewGroup> groups,
            Expression<Func<GXComponentViewGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete component view groups.</summary>
        /// <param name="groups">Block component view to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete component view groups.</returns>
        [System.ComponentModel.Description("Delete component view groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns component view groups list where component view belongs.
        /// </summary>
        /// <param name="componentViewId">Component view ID</param>
        /// <returns>List of component view groups.</returns>
        [System.ComponentModel.Description("Returns component view groups list where component view belongs.")]
        Task<List<GXComponentViewGroup>> GetJoinedComponentViewGroups(Guid componentViewId);

        /// <summary>
        /// Get all users that can access this component view group.
        /// </summary>
        /// <param name="componentViewGroupId">Component view group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this component view group.")]
        Task<List<string>> GetUsersAsync(Guid? componentViewGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access component view groups.
        /// </summary>
        /// <param name="componentViewGroupIds">Component view group ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access component view groups.")]
        Task<List<string>> GetUsersAsync(
            IEnumerable<Guid>? componentViewGroupIds, CancellationToken cancellationToken = default);
    }
}
