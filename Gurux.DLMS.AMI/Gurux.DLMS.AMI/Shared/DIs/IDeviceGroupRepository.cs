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
    /// This interface is used to handle device groups.
    /// </summary>
    public interface IDeviceGroupRepository
    {
        /// <summary>
        /// List device groups.
        /// </summary>
        /// <returns>Device groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXDeviceGroup>> ListAsync(
            ListDeviceGroups? request = null,
            ListDeviceGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device details.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXDeviceGroup> ReadAsync(Guid id, Expression<Func<GXDeviceGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update device groups.
        /// </summary>
        /// <param name="groups">Updated device groups.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXDeviceGroup> groups,
            Expression<Func<GXDeviceGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete device group(s).
        /// </summary>
        /// <param name="groups">Device groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns device groups where device belongs.
        /// </summary>
        /// <param name="deviceId">Device ID</param>
        /// <returns>List of device groups where device id belongs.</returns>
        Task<List<GXDeviceGroup>> GetDeviceGroupsByDeviceId(
            Guid deviceId);

        /// <summary>
        /// Returns device groups where agent group belongs.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID</param>
        /// <returns>List of device groups where agent group id belongs.</returns>
        Task<List<GXDeviceGroup>> GetDeviceGroupsByAgentId(
            Guid agentGroupId);

        /// <summary>
        /// Returns device groups where gateway group belongs.
        /// </summary>
        /// <param name="agentGroupId">Gateway group ID</param>
        /// <returns>List of device groups where agent group id belongs.</returns>
        Task<List<GXDeviceGroup>> GetDeviceGroupsByGatewayId(
            Guid agentGroupId);

        /// <summary>
        /// Returns list of users that can access this device group.
        /// </summary>
        /// <param name="groupId">Device group Id.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access device groups.
        /// </summary>
        /// <param name="groupIds">Device group Ids.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default);
    }
}
