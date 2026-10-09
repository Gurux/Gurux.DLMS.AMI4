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
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle block groups.
    /// </summary>
    public interface IBlockGroupRepository
    {
        /// <summary>
        /// List block groups.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>User groups.</returns>
        Task<IEnumerable<GXBlockGroup>> ListAsync(
            ListBlockGroups? request = null,
            ListBlockGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read block group details.
        /// </summary>
        /// <param name="id">Block group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXBlockGroup> ReadAsync(Guid id,
            Expression<Func<GXBlockGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update block groups.
        /// </summary>
        /// <param name="groups">Updated block groups.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXBlockGroup> groups,
            Expression<Func<GXBlockGroup, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete block group(s).
        /// </summary>
        /// <param name="groups">Block groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns block groups list where block belongs.
        /// </summary>
        /// <param name="blockId">Block ID</param>
        /// <returns>List of block groups.</returns>
        Task<List<GXBlockGroup>> GetJoinedBlockGroups(Guid blockId);

        /// <summary>
        /// Get all users that can access this block group.
        /// </summary>
        /// <param name="blockGroupId">Block group id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? blockGroupId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access block group.
        /// </summary>
        /// <param name="blockGroupIds">Agent Block ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockGroupIds, CancellationToken cancellationToken = default);
    }
}
