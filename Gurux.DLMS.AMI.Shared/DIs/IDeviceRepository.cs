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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle devices.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle devices.")]
    public interface IDeviceRepository
    {
        /// <summary>
        /// List devices.
        /// </summary>
        /// <returns>Devices.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List devices.")]
        Task<IEnumerable<GXDevice>> ListAsync(
        ListDevices? request = null,
        ListDevicesResponse? response = null,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device details.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Device information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read device details.")]
        Task<GXDevice> ReadAsync(Guid id, Expression<Func<GXDevice, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new devices or updates existing ones.
        /// </summary>
        /// <param name="devices">The records to insert or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="groups">Device groups where devices are added. This can be used with batch.</param>
        /// <param name="lateBinding">Device objects are create only when they are read from the meter.</param>
        /// <remarks>
        /// Late binding improves device creation when a huge amount of devices is created. 
        /// This is handy when only a part of device objects is read, but the objects want to remain active just in case.
        /// </remarks>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new devices or updates existing ones.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068", Justification = "Preserves the existing public repository signature and positional callers.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken = default,
            Expression<Func<GXDevice, object>>? columns = null,
            bool lateBinding = false,
            IEnumerable<GXDeviceGroup>? groups = null);

        /// <summary>Delete devices.</summary>
        /// <param name="devices">Deleted device(s).</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, users are notified about the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete devices.</returns>
        [System.ComponentModel.Description("Delete devices.")]
        Task DeleteAsync(
            IEnumerable<Guid> devices,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access this device.
        /// </summary>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access this device.")]
        Task<List<string>> GetUsersAsync(Guid? deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns list of users that can access devices.
        /// </summary>
        /// <param name="deviceIds">Device Ids.</param>
        /// <returns>List of users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Returns list of users that can access devices.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid> deviceIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the device status.
        /// </summary>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="status">Device status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to update the device status.</returns>
        [System.ComponentModel.Description("Updates the device status.")]
        Task UpdateStatusAsync(Guid deviceId, DeviceStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset devices to disconnected state.
        /// </summary>
        /// <param name="devices">Resetted device(s).</param>
        /// <returns>A task representing completion of the request to reset devices to disconnected state.</returns>
        [System.ComponentModel.Description("Reset devices to disconnected state.")]
        Task ResetAsync(IEnumerable<Guid> devices);
    }
}
