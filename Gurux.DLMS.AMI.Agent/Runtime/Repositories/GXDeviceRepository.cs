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

using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements device repository that can be called from the agent script.
    /// </summary>
    class GXDeviceRepository(HttpClient http) : IDeviceRepository
    {
        /// <inheritdoc/>
        public Task DeleteAsync(IEnumerable<Guid> devices, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            RemoveDevice req = new() { Ids = [.. devices], Delete = delete };
            return http.PostAsJson<RemoveDeviceResponse>("/api/Device/Delete", req, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(Guid? deviceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(IEnumerable<Guid>? deviceIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXDevice>> ListAsync(
            ListDevices? request,
            ListDevicesResponse? response,
            CancellationToken cancellationToken)
        {
            ListDevicesResponse? ret = await http.PostAsJson<ListDevicesResponse>("/api/Device/List",
                request ?? new ListDevices(), cancellationToken);
            if (ret == null)
            {
                return [];
            }
            if (response != null)
            {
                response.Count = ret.Count;
                response.Devices = ret.Devices;
            }
            return ret.Devices ?? [];
        }

        /// <inheritdoc/>
        public async Task<GXDevice> ReadAsync(Guid id, Expression<Func<GXDevice, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            var response = await http.GetAsJsonAsync<GetDeviceResponse>(string.Format("/api/Device/?Id={0}&includeKeys=true", id), cancellationToken: cancellationToken);
            return response.Item ?? throw new KeyNotFoundException("Device was not returned.");
        }

        public Task ResetAsync(IEnumerable<Guid> devices)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068", Justification = "Preserves the existing public repository signature and positional callers.")]
        public Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken = default,
            Expression<Func<GXDevice, object>>? columns = null,
            bool lateBinding = false, IEnumerable<GXDeviceGroup>? groups = null)
            => UpsertAsync(devices, cancellationToken, columns, lateBinding, groups);

        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> UpsertAsync(

            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken = default,
            Expression<Func<GXDevice, object>>? columns = null,
            bool lateBinding = false,
            IEnumerable<GXDeviceGroup>? groups = null)
        {
            UpdateDevice req = new() { Devices = [.. devices], LateBinding = lateBinding, Groups = groups };
            UpdateDeviceResponse? ret = await http.PostAsJson<UpdateDeviceResponse>("/api/Device/Update", req, cancellationToken);
            if (ret == null)
            {
                return [];
            }
            return ret.Ids;
        }

        public Task UpdateStatusAsync(Guid deviceId, DeviceStatus status, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
