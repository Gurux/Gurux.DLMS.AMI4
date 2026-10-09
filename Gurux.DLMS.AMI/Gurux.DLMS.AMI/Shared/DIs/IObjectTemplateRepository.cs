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
    /// This interface is used to handle object templates.
    /// </summary>
    public interface IObjectTemplateRepository
    {
        /// <summary>
        /// List objects.
        /// </summary>
        /// <returns>ObjectTemplates.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXObjectTemplate>> ListAsync(
            ListObjectTemplates? request = null,
            ListObjectTemplatesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read object details.
        /// </summary>
        /// <param name="id">ObjectTemplate id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>ObjectTemplate information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXObjectTemplate> ReadAsync(Guid id, Expression<Func<GXObjectTemplate, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update object(s).
        /// </summary>
        /// <param name="objects">Updated object(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXObjectTemplate> objects,
            Expression<Func<GXObjectTemplate, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete object(s).
        /// </summary>
        /// <param name="objects">ObjectTemplate(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> objects, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this object template.
        /// </summary>
        /// <param name="objectId">ObjectTemplate id.</param>
        /// <returns>Collection of User IDs.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access object templates.
        /// </summary>
        /// <param name="objectIds">ObjectTemplate ids.</param>
        /// <returns>Collection of User IDs.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? objectIds, CancellationToken cancellationToken = default);
    }
}
