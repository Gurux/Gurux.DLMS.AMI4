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
    public interface IManufacturerGroupRepository
    {
        /// <summary>
        /// List manufacturer groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXManufacturerGroup[]> ListAsync(
            ClaimsPrincipal user, 
            ListManufacturerGroups? request, 
            ListManufacturerGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read manufacturer group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Manufacturer group id.</param>
        /// <returns></returns>
        Task<GXManufacturerGroup> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update manufacturer groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Updated manufacturer groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXManufacturerGroup> groups,
            Expression<Func<GXManufacturerGroup, object?>>? columns = null);

        /// <summary>
        /// Delete manufacturer group(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Manufacturer groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns manufacturer groups list where manufacturer belongs.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="manufacturerId">Manufacturer ID</param>
        /// <returns>List of manufacturer groups.</returns>
        Task<List<GXManufacturerGroup>> GetJoinedManufacturerGroups(ClaimsPrincipal user, Guid manufacturerId);

        /// <summary>
        /// Get all users that can access this manufacturer group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturerGroupId">Manufacturer group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? manufacturerGroupId);

        /// <summary>
        /// Get all users that can access manufacturer group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturerGroupIds">Agent Manufacturer ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? manufacturerGroupIds);
    }
}
