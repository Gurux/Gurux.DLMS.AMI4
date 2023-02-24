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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle object.
    /// </summary>
    public interface IObjectRepository
    {
        /// <summary>
        /// List objects.
        /// </summary>
        /// <returns>Objects.</returns>
        Task<GXObject[]> ListAsync(ClaimsPrincipal user,
            ListObjects? request,
            ListObjectsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read object information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Object id.</param>
        /// <returns>Object information.</returns>
        Task<GXObject> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update object(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="objects">Updated object(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXObject> objects,
            Expression<Func<GXObject, object?>>? columns = null);

        /// <summary>
        /// Delete object(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="objects">Object(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> objects, bool delete);

        /// <summary>
        /// Get all users that can access this object.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="objectId">Object id.</param>
        /// <returns>Collection of User IDs.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? objectId);

        /// <summary>
        /// Get all users that can access objects.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="objectIds">Object ids.</param>
        /// <returns>Collection of User IDs.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? objectIds);
    }
}
