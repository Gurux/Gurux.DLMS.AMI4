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


using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements device repository that can be called from the agent script.
    /// </summary>
    class GXDeviceRepository : IDeviceRepository
    {
        /// <inheritdoc/>
        public Task DeleteAsync(ClaimsPrincipal? user, IEnumerable<Guid> devices, bool delete)
        {
            RemoveDevice req = new RemoveDevice() { Ids = devices.ToArray(), Delete = delete };
            return GXAgentWorker.client.PostAsJson<RemoveDeviceResponse>("/api/Device/Delete", req);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal? user, Guid? deviceId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? deviceIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXDevice[]> ListAsync(
            ClaimsPrincipal User,
            ListDevices? request, 
            ListDevicesResponse? response,
            CancellationToken cancellationToken)
        {
            ListDevicesResponse? ret = await GXAgentWorker.client.PostAsJson<ListDevicesResponse>("/api/Device/List", 
                request, cancellationToken);
            if (ret == null)
            {
                return new GXDevice[0];
            }
            if (response != null)
            {
                response.Count = ret.Count;
                response.Devices = ret.Devices;
            }
            return ret.Devices;
        }

        /// <inheritdoc/>
        public async Task<GXDevice> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return await GXAgentWorker.client.GetAsJsonAsync<GXDevice>(string.Format("/api/Device/?Id={0}", id));
        }

        public Task ResetAsync(ClaimsPrincipal user, IEnumerable<Guid> devices)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal? user, 
            IEnumerable<GXDevice> devices, 
            CancellationToken cancellationToken,
            Expression<Func<GXDevice, object?>>? columns = null,
            bool lateBinding = false,
            IEnumerable<GXDeviceGroup>? groups = null)
        {
            UpdateDevice req = new UpdateDevice() { Devices = devices.ToArray() };
            UpdateDeviceResponse? ret = await GXAgentWorker.client.PostAsJson<UpdateDeviceResponse>("/api/Device/Update", req, cancellationToken);
            if (ret == null)
            {
                return new Guid[0];
            }
            return ret.Ids;
        }

        public Task UpdateStatusAsync(ClaimsPrincipal User, Guid deviceId, DeviceStatus status)
        {
            throw new NotImplementedException();
        }
    }
}