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
