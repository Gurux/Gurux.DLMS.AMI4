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
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle content groups.
    /// </summary>
    public interface IContentGroupRepository
    {
        /// <summary>
        /// List content groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXContentGroup[]> ListAsync(
            ListContentGroups? request,
            ListContentGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read content group information.
        /// </summary>
        /// <param name="id">Content group id.</param>
        /// <returns></returns>
        Task<GXContentGroup> ReadAsync(Guid id);

        /// <summary>
        /// Update content groups.
        /// </summary>
        /// <param name="groups">Updated content groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXContentGroup> groups,
            Expression<Func<GXContentGroup, object?>>? columns = null);

        /// <summary>
        /// Delete content group(s).
        /// </summary>
        /// <param name="groups">Content groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns content groups list where content belongs.
        /// </summary>
        /// <param name="contentId">Content ID</param>
        /// <returns>List of content groups.</returns>
        Task<List<GXContentGroup>> GetJoinedContentGroups(Guid contentId);

        /// <summary>
        /// Get all users that can access this content group.
        /// </summary>
        /// <param name="contentGroupId">Content group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? contentGroupId);

        /// <summary>
        /// Get all users that can access content group.
        /// </summary>
        /// <param name="contentGroupIds">Agent Content ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentGroupIds);
    }
}
