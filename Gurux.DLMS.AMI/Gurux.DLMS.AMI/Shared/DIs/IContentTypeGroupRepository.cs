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
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle content type groups.
    /// </summary>
    public interface IContentTypeGroupRepository
    {
        /// <summary>
        /// List content type groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXContentTypeGroup[]> ListAsync(
            ListContentTypeGroups? request,
            ListContentTypeGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read content type group information.
        /// </summary>
        /// <param name="id">ContentType group id.</param>
        /// <returns></returns>
        Task<GXContentTypeGroup> ReadAsync(Guid id);

        /// <summary>
        /// Update content type groups.
        /// </summary>
        /// <param name="groups">Updated content type groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXContentTypeGroup> groups,
            Expression<Func<GXContentTypeGroup, object?>>? columns = null);

        /// <summary>
        /// Delete content type group(s).
        /// </summary>
        /// <param name="groups">ContentType groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns content type groups list where content type belongs.
        /// </summary>
        /// <param name="contentTypeId">Content type ID</param>
        /// <returns>List of content type groups.</returns>
        Task<List<GXContentTypeGroup>> GetJoinedContentTypeGroups(Guid contentTypeId);

        /// <summary>
        /// Get all users that can access this content type group.
        /// </summary>
        /// <param name="contentTypeGroupId">Content type group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? contentTypeGroupId);

        /// <summary>
        /// Get all users that can access content type group.
        /// </summary>
        /// <param name="contentTypeIds">Content type ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentTypeIds);
    }
}
