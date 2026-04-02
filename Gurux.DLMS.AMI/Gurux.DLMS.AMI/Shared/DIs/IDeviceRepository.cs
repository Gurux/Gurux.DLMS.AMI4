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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle devices.
    /// </summary>
    public interface IDeviceRepository
    {
        /// <summary>
        /// List devices.
        /// </summary>
        /// <returns>Devices.</returns>
        Task<GXDevice[]> ListAsync(
        ListDevices? request,
        ListDevicesResponse? response = null,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device information.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <returns>Device information.</returns>
        Task<GXDevice> ReadAsync(Guid id);

        /// <summary>
        /// Update device(s).
        /// </summary>
        /// <param name="devices">Updated device(s).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="columns">Updated columns(s).</param>
        /// <param name="groups">Device groups where devices are added. This can be used with batch.</param>
        /// <param name="lateBinding">Device objects are create only when they are read from the meter.</param>
        /// <remarks>
        /// Late binding improves device creation when a huge amount of devices is created. 
        /// This is handy when only a part of device objects is read, but the objects want to remain active just in case.
        /// </remarks>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken,
            Expression<Func<GXDevice, object?>>? columns = null,
            bool lateBinding = false,
            IEnumerable<GXDeviceGroup>? groups = null);

        /// <summary>
        /// Delete device(s).
        /// </summary>
        /// <param name="devices">Deleted device(s).</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(
            IEnumerable<Guid> devices,
            bool delete);

        /// <summary>
        /// Returns list of users that can access this device.
        /// </summary>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(Guid? deviceId);

        /// <summary>
        /// Returns list of users that can access devices.
        /// </summary>
        /// <param name="deviceIds">Device Ids.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid> deviceIds);

        /// <summary>
        /// Updates the device status.
        /// </summary>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="status">Device status</param>
        Task UpdateStatusAsync(Guid deviceId, DeviceStatus status);

        /// <summary>
        /// Reset devices to disconnected state.
        /// </summary>
        /// <param name="devices">Resetted device(s).</param>
        Task ResetAsync(IEnumerable<Guid> devices);
    }
}
