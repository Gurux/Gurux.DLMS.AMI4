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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle device groups.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle device groups.")]
    public interface IDeviceGroupRepository
    {
        /// <summary>
        /// List device groups.
        /// </summary>
        /// <returns>Device groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List device groups.")]
        Task<IEnumerable<GXDeviceGroup>> ListAsync(
            ListDeviceGroups? request = null,
            ListDeviceGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads device group details by identifier.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested device group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Reads device group details by identifier.")]
        Task<GXDeviceGroup> ReadAsync(Guid id, Expression<Func<GXDeviceGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new device groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated device groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new device groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDeviceGroup> groups,
            Expression<Func<GXDeviceGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete device groups.</summary>
        /// <param name="groups">Device groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete device groups.</returns>
        [System.ComponentModel.Description("Delete device groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns device groups where device belongs.
        /// </summary>
        /// <param name="deviceId">Device ID</param>
        /// <returns>List of device groups where device id belongs.</returns>
        [System.ComponentModel.Description("Returns device groups where device belongs.")]
        Task<List<GXDeviceGroup>> GetDeviceGroupsByDeviceId(
            Guid deviceId);

        /// <summary>
        /// Returns device groups where agent group belongs.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID</param>
        /// <returns>List of device groups where agent group id belongs.</returns>
        [System.ComponentModel.Description("Returns device groups where agent group belongs.")]
        Task<List<GXDeviceGroup>> GetDeviceGroupsByAgentId(
            Guid agentGroupId);

        /// <summary>
        /// Returns device groups where gateway group belongs.
        /// </summary>
        /// <param name="agentGroupId">Gateway group ID</param>
        /// <returns>List of device groups where agent group id belongs.</returns>
        [System.ComponentModel.Description("Returns device groups where gateway group belongs.")]
        Task<List<GXDeviceGroup>> GetDeviceGroupsByGatewayId(
            Guid agentGroupId);

        /// <summary>
        /// Returns list of users that can access this device group.
        /// </summary>
        /// <param name="groupId">Device group Id.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access this device group.")]
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access device groups.
        /// </summary>
        /// <param name="groupIds">Device group Ids.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access device groups.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default);
    }
}
