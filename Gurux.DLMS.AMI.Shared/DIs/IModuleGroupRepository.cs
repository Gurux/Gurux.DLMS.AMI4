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
