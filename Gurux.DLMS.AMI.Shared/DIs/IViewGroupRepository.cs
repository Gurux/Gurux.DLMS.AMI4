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
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle view groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle view groups.")]
    public interface IViewGroupRepository
    {
        /// <summary>
        /// List view groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List view groups.")]
        Task<IEnumerable<GXViewGroup>> ListAsync(
            ListViewGroups? request = null,
            ListViewGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read view group information.
        /// </summary>
        /// <param name="id">View group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested view group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read view group information.")]
        Task<GXViewGroup> ReadAsync(Guid id, Expression<Func<GXViewGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new view groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated view groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new view groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXViewGroup> groups,
            Expression<Func<GXViewGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete view groups.</summary>
        /// <param name="groups">View groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete view groups.</returns>
        [System.ComponentModel.Description("Delete view groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns view groups list where view belongs.
        /// </summary>
        /// <param name="viewId">View ID</param>
        /// <returns>List of view groups.</returns>
        [System.ComponentModel.Description("Returns view groups list where view belongs.")]
        Task<List<GXViewGroup>> GetJoinedViewGroups(Guid viewId);

        /// <summary>
        /// Get all users that can access this view group.
        /// </summary>
        /// <param name="viewGroupId">View group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this view group.")]
        Task<List<string>> GetUsersAsync(Guid? viewGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access view group.
        /// </summary>
        /// <param name="viewGroupIds">View group identifiers.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access view group.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? viewGroupIds, CancellationToken cancellationToken = default);
    }
}
