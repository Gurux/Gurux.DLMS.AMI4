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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle subtotal groups.
    /// </summary>
    public interface ISubtotalGroupRepository
    {
        /// <summary>
        /// List subtotal groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXSubtotalGroup[]> ListAsync(
            ClaimsPrincipal user, 
            ListSubtotalGroups? request, 
            ListSubtotalGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read subtotal group information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Subtotal group id.</param>
        /// <returns></returns>
        Task<GXSubtotalGroup> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update subtotal groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Updated subtotal groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXSubtotalGroup> groups,
            Expression<Func<GXSubtotalGroup, object?>>? columns = null);

        /// <summary>
        /// Delete subtotal group(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groups">Subtotal groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns subtotal groups list where subtotal belongs.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="subtotalId">Subtotal ID</param>
        /// <returns>List of subtotal groups.</returns>
        Task<List<GXSubtotalGroup>> GetJoinedSubtotalGroups(ClaimsPrincipal user, Guid subtotalId);

        /// <summary>
        /// Get all users that can access this subtotal group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="subtotalGroupId">Subtotal group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? subtotalGroupId);

        /// <summary>
        /// Get all users that can access subtotal group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="subtotalGroupIds">Agent Subtotal ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? subtotalGroupIds);
    }
}
