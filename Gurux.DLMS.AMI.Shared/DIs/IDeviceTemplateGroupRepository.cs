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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle device template groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle device template groups.")]
    public interface IDeviceTemplateGroupRepository
    {
        /// <summary>
        /// List device template groups.
        /// </summary>
        /// <returns>Device template groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List device template groups.")]
        Task<IEnumerable<GXDeviceTemplateGroup>> ListAsync(
            ListDeviceTemplateGroups? request = null,
            ListDeviceTemplateGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device template group.
        /// </summary>
        /// <param name="id">Device template group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Device template group information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read device template group.")]
        Task<GXDeviceTemplateGroup> ReadAsync(Guid id, Expression<Func<GXDeviceTemplateGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new device template groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated device template groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new device template groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDeviceTemplateGroup> groups,
            Expression<Func<GXDeviceTemplateGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete device template groups.</summary>
        /// <param name="groups">Device template groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete device template groups.</returns>
        [System.ComponentModel.Description("Delete device template groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns device template groups list where device template belongs.
        /// </summary>
        /// <param name="deviceTemplateId">Device template ID</param>
        /// <returns>List of device template groups.</returns>
        [System.ComponentModel.Description("Returns device template groups list where device template belongs.")]
        Task<List<GXDeviceTemplateGroup>> GetJoinedDeviceTemplateGroups(Guid deviceTemplateId);

        /// <summary>
        /// Returns list of users that can access this device template group.
        /// </summary>
        /// <param name="groupId">Device template group Id.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access this device template group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access device template groups.
        /// </summary>
        /// <param name="groupIds">Device template group Ids.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access device template groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default);
    }
}
