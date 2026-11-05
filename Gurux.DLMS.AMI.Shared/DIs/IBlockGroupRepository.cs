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
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle block groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle block groups.")]
    public interface IBlockGroupRepository
    {
        /// <summary>
        /// List block groups.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>User groups.</returns>
        [System.ComponentModel.Description("List block groups.")]
        Task<IEnumerable<GXBlockGroup>> ListAsync(
            ListBlockGroups? request = null,
            ListBlockGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read block group details.
        /// </summary>
        /// <param name="id">Block group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested block group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read block group details.")]
        Task<GXBlockGroup> ReadAsync(Guid id,
            Expression<Func<GXBlockGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new block groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated block groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new block groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXBlockGroup> groups,
            Expression<Func<GXBlockGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>Delete block groups.</summary>
        /// <param name="groups">Block groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete block groups.</returns>
        [System.ComponentModel.Description("Delete block groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns block groups list where block belongs.
        /// </summary>
        /// <param name="blockId">Block ID</param>
        /// <returns>List of block groups.</returns>
        [System.ComponentModel.Description("Returns block groups list where block belongs.")]
        Task<List<GXBlockGroup>> GetJoinedBlockGroups(Guid blockId);

        /// <summary>
        /// Get all users that can access this block group.
        /// </summary>
        /// <param name="blockGroupId">Block group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this block group.")]
        Task<List<string>> GetUsersAsync(Guid? blockGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access block group.
        /// </summary>
        /// <param name="blockGroupIds">Agent Block ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access block group.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockGroupIds, CancellationToken cancellationToken = default);
    }
}
