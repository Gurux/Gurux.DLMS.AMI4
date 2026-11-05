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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle script groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle script groups.")]
    public interface IScriptGroupRepository
    {
        /// <summary>
        /// List script groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List script groups.")]
        Task<IEnumerable<GXScriptGroup>> ListAsync(
            ListScriptGroups? request = null,
            ListScriptGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read script group details.
        /// </summary>
        /// <param name="id">Script group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested script group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read script group details.")]
        Task<GXScriptGroup> ReadAsync(Guid id, Expression<Func<GXScriptGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new script groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated script groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new script groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScriptGroup> groups,
            Expression<Func<GXScriptGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete script groups.</summary>
        /// <param name="groups">Script groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete script groups.</returns>
        [System.ComponentModel.Description("Delete script groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns script groups list where script belongs.
        /// </summary>
        /// <param name="scriptId">Script ID</param>
        /// <returns>List of script groups.</returns>
        [System.ComponentModel.Description("Returns script groups list where script belongs.")]
        Task<List<GXScriptGroup>> GetJoinedScriptGroups(Guid scriptId);

        /// <summary>
        /// Get all users that can access this script group.
        /// </summary>
        /// <param name="groupId">Script group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this script group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access script groups.
        /// </summary>
        /// <param name="groupId">Script group ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access script groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupId, CancellationToken cancellationToken = default);
    }
}
