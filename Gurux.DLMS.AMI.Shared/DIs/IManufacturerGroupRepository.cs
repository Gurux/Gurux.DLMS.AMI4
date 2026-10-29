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

using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle manufacturer groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle manufacturer groups.")]
    public interface IManufacturerGroupRepository
    {
        /// <summary>
        /// List manufacturer groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List manufacturer groups.")]
        Task<IEnumerable<GXManufacturerGroup>> ListAsync(
            ListManufacturerGroups? request = null,
            ListManufacturerGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read manufacturer group details.
        /// </summary>
        /// <param name="id">Manufacturer group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested manufacturer group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read manufacturer group details.")]
        Task<GXManufacturerGroup> ReadAsync(Guid id, Expression<Func<GXManufacturerGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new manufacturer groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated manufacturer groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new manufacturer groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXManufacturerGroup> groups,
            Expression<Func<GXManufacturerGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete manufacturer groups.</summary>
        /// <param name="groups">Manufacturer groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete manufacturer groups.</returns>
        [System.ComponentModel.Description("Delete manufacturer groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns manufacturer groups list where manufacturer belongs.
        /// </summary>
        /// <param name="manufacturerId">Manufacturer ID</param>
        /// <returns>List of manufacturer groups.</returns>
        [System.ComponentModel.Description("Returns manufacturer groups list where manufacturer belongs.")]
        Task<List<GXManufacturerGroup>> GetJoinedManufacturerGroups(Guid manufacturerId);

        /// <summary>
        /// Get all users that can access this manufacturer group.
        /// </summary>
        /// <param name="manufacturerGroupId">Manufacturer group id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this manufacturer group.")]
        Task<List<string>> GetUsersAsync(Guid? manufacturerGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access manufacturer group.
        /// </summary>
        /// <param name="manufacturerGroupIds">Agent Manufacturer ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access manufacturer group.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? manufacturerGroupIds, CancellationToken cancellationToken = default);
    }
}
