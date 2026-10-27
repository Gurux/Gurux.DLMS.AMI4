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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to manage device templates.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to manage device templates.")]
    public interface IDeviceTemplateRepository
    {
        /// <summary>
        /// List device templates.
        /// </summary>
        /// <returns>Device templates.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List device templates.")]
        Task<IEnumerable<GXDeviceTemplate>> ListAsync(
            ListDeviceTemplates? request = null,
            ListDeviceTemplatesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device template details.
        /// </summary>
        /// <param name="id">Device template id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read device template details.")]
        Task<GXDeviceTemplate> ReadAsync(Guid id, Expression<Func<GXDeviceTemplate, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new device templates or updates existing ones.
        /// </summary>
        /// <param name="templates">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new device templates or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDeviceTemplate> templates,
            Expression<Func<GXDeviceTemplate, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete device templates.</summary>
        /// <param name="templates">Device templates to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete device templates.")]
        Task DeleteAsync(IEnumerable<Guid> templates, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access device template.
        /// </summary>
        /// <param name="deviceTemplateId">Device template id.</param>
        /// <returns>User Ids that can access device template.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access device template.")]
        Task<List<string>> GetUsersAsync(Guid? deviceTemplateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access given device templates.
        /// </summary>
        /// <param name="deviceTemplateIds">Device template ids.</param>
        /// <returns>User Ids that can access device template.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access given device templates.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? deviceTemplateIds, CancellationToken cancellationToken = default);
    }
}
