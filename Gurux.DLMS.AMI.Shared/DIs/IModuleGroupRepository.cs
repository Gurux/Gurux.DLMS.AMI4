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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle module groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle module groups.")]
    public interface IModuleGroupRepository
    {
        /// <summary>
        /// List module groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List module groups.")]
        Task<IEnumerable<GXModuleGroup>> ListAsync(
            ListModuleGroups? request = null,
            ListModuleGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read module group details.
        /// </summary>
        /// <param name="id">Module group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested module group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read module group details.")]
        Task<GXModuleGroup> ReadAsync(Guid id, Expression<Func<GXModuleGroup, object>>? columns = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Inserts new module groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated module groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new module groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXModuleGroup> groups,
            Expression<Func<GXModuleGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete module groups.</summary>
        /// <param name="groups">Module groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, users are notified about the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete module groups.</returns>
        [System.ComponentModel.Description("Delete module groups.")]
        Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns module groups list where module belongs.
        /// </summary>
        /// <param name="moduleId">Module ID</param>
        /// <returns>List of module groups.</returns>
        [System.ComponentModel.Description("Returns module groups list where module belongs.")]
        Task<List<GXModuleGroup>> GetJoinedModuleGroups(string moduleId);

        /// <summary>
        /// Returns list of users that can access this module group.
        /// </summary>
        /// <param name="groupId">Module group Id.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access this module group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access module groups.
        /// </summary>
        /// <param name="groupIds">Module group Ids.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access module groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default);
    }
}
