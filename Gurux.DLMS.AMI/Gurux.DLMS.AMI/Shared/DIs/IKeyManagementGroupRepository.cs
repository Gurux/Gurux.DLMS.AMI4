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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle key management groups.
    /// </summary>
    public interface IKeyManagementGroupRepository
    {
        /// <summary>
        /// List key management groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXKeyManagementGroup>> ListAsync(
            ListKeyManagementGroups? request = null,
            ListKeyManagementGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read key management group details.
        /// </summary>
        /// <param name="id">KeyManagement group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXKeyManagementGroup> ReadAsync(Guid id, Expression<Func<GXKeyManagementGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update key management groups.
        /// </summary>
        /// <param name="groups">Updated key management groups.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXKeyManagementGroup> groups,
            Expression<Func<GXKeyManagementGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete key management group(s).
        /// </summary>
        /// <param name="groups">KeyManagement groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns key management groups list where key management belongs.
        /// </summary>
        /// <param name="keyId">Key management ID</param>
        /// <returns>List of key management groups.</returns>
        Task<List<GXKeyManagementGroup>> GetJoinedKeyManagementGroups(Guid keyId);

        /// <summary>
        /// Get all users that can access this key management group.
        /// </summary>
        /// <param name="groupId">Key management group id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access key management groups.
        /// </summary>
        /// <param name="groupId">KeyManagement group ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupId, CancellationToken cancellationToken = default);
    }
}
