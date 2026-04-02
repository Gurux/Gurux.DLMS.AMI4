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
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle contents.
    /// </summary>
    public interface IContentRepository
    {
        /// <summary>
        /// List contents.
        /// </summary>
        /// <returns>Contents.</returns>
        Task<GXContent[]> ListAsync(
            ListContents? request,
            ListContentsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read content.
        /// </summary>
        /// <param name="id">Content id.</param>
        /// <returns></returns>
        Task<GXContent> ReadAsync(Guid id);

        /// <summary>
        /// Update content(s).
        /// </summary>
        /// <param name="contents">Updated content(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXContent> contents,
            Expression<Func<GXContent, object?>>? columns = null);

        /// <summary>
        /// Delete content(s).
        /// </summary>
        /// <param name="contents">Content(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> contents, bool delete);

        /// <summary>
        /// Close content(s).
        /// </summary>
        /// <param name="contents">Contents to close.</param>
        Task CloseAsync(IEnumerable<Guid> contents);

        /// <summary>
        /// Get all users that can access this content.
        /// </summary>
        /// <param name="contentId">Content id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? contentId);

        /// <summary>
        /// Get all users that can access contents.
        /// </summary>
        /// <param name="contentIds">Content ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentIds);
    }
}
