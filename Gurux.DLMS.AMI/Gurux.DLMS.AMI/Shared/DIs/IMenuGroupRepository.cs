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
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle menu groups.
    /// </summary>
    public interface IMenuGroupRepository
    {
        /// <summary>
        /// List menu groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXMenuGroup>> ListAsync(
            ListMenuGroups? request = null,
            ListMenuGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read menu group information.
        /// </summary>
        /// <param name="id">Menu group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXMenuGroup> ReadAsync(Guid id, Expression<Func<GXMenuGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update menu groups.
        /// </summary>
        /// <param name="groups">Updated menu groups.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXMenuGroup> groups,
            Expression<Func<GXMenuGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete menu group(s).
        /// </summary>
        /// <param name="groups">Menu groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns menu groups list where menu belongs.
        /// </summary>
        /// <param name="menuId">Menu ID</param>
        /// <returns>List of menu groups.</returns>
        Task<List<GXMenuGroup>> GetJoinedMenuGroups(Guid menuId);

        /// <summary>
        /// Get all users that can access this menu group.
        /// </summary>
        /// <param name="menuGroupId">Menu group id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? menuGroupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access menu group.
        /// </summary>
        /// <param name="menuGroupIds">Agent Menu ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? menuGroupIds, CancellationToken cancellationToken = default);
    }
}
