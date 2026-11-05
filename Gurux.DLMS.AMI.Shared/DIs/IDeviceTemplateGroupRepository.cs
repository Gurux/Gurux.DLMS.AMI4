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
