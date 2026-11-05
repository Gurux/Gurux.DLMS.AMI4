// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle object.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle object.")]
    public interface IObjectRepository
    {
        /// <summary>
        /// List objects.
        /// </summary>
        /// <returns>Objects.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List objects.")]
        Task<IEnumerable<GXObject>> ListAsync(
            ListObjects? request = null,
            ListObjectsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read object details.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Object information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read object details.")]
        Task<GXObject> ReadAsync(Guid id, Expression<Func<GXObject, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new objects or updates existing ones.
        /// </summary>
        /// <param name="objects">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new objects or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXObject> objects,
            Expression<Func<GXObject, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete objects.</summary>
        /// <param name="objects">Object(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete objects.</returns>
        [System.ComponentModel.Description("Delete objects.")]
        Task DeleteAsync(IEnumerable<Guid> objects, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this object.
        /// </summary>
        /// <param name="objectId">Object id.</param>
        /// <returns>Collection of User IDs.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this object.")]
        Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access objects.
        /// </summary>
        /// <param name="objectIds">Object ids.</param>
        /// <returns>Collection of User IDs.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access objects.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? objectIds, CancellationToken cancellationToken = default);
    }
}
