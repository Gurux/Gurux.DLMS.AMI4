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
    /// This interface is used to handle content types.
    /// </summary>
    public interface IContentTypeRepository
    {
        /// <summary>
        /// List content types.
        /// </summary>
        /// <returns>ContentTypes.</returns>
        Task<GXContentType[]> ListAsync(
            ListContentTypes? request,
            ListContentTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read content type.
        /// </summary>
        /// <param name="id">ContentType id.</param>
        /// <returns></returns>
        Task<GXContentType> ReadAsync(Guid id);

        /// <summary>
        /// Update content type(s).
        /// </summary>
        /// <param name="contentTypes">Updated content type(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXContentType> contentTypes,
            Expression<Func<GXContentType, object?>>? columns = null);

        /// <summary>
        /// Delete content type(s).
        /// </summary>
        /// <param name="contentTypes">Content type(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> contentTypes, bool delete);

        /// <summary>
        /// Get all users that can access this content type.
        /// </summary>
        /// <param name="contentTypeId">ContentType id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? contentTypeId);

        /// <summary>
        /// Get all users that can access content types.
        /// </summary>
        /// <param name="contentTypeIds">ContentType ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? contentTypeIds);
    }
}
