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
    /// This interface is used to handle attributes.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle attributes.")]
    public interface IAttributeRepository
    {
        /// <summary>
        /// List attributes.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Attributes.</returns>
        [System.ComponentModel.Description("List attributes.")]
        Task<IEnumerable<GXAttribute>> ListAsync(
            ListAttributes? request = null,
            ListAttributesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read attribute details.
        /// </summary>
        /// <param name="id">Attribute id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Attribute information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read attribute details.")]
        Task<GXAttribute> ReadAsync(Guid id, Expression<Func<GXAttribute, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new attributes or updates existing ones.
        /// </summary>
        /// <param name="attributers">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new attributes or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAttribute> attributers,
            Expression<Func<GXAttribute, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete attributes.</summary>
        /// <param name="attributes">Attribute(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete attributes.</returns>
        [System.ComponentModel.Description("Delete attributes.")]
        Task DeleteAsync(IEnumerable<Guid> attributes, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update attribute datatype.
        /// </summary>
        /// <param name="attributes">Attributes whose datatype metadata is updated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the attribute datatype metadata has been updated.</returns>
        [System.ComponentModel.Description("Update attribute datatype.")]
        Task UpdateDatatypeAsync(IEnumerable<GXAttribute> attributes,
            CancellationToken cancellationToken = default);
    }
}
