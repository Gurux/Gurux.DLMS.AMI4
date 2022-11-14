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
using Gurux.DLMS.AMI.Shared.Rest;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements device error repository that can be called from the agent script.
    /// </summary>
    class GXDeviceErrorRepository : IDeviceErrorRepository
    {
        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal? User, IEnumerable<GXDeviceError> errors)
        {
            AddDeviceError req = new AddDeviceError() { Errors = errors.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddDeviceErrorResponse>("/api/DeviceError/Add", req);
        }

        /// <inheritdoc/>
        public Task<GXDeviceError> AddAsync(ClaimsPrincipal? User, GXDevice device, Exception ex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task ClearAsync(ClaimsPrincipal? User, IEnumerable<Guid>? devices)
        {
            ClearDeviceErrors req = new ClearDeviceErrors()
            {
                Devices = devices != null ? devices.ToArray() : null
            };
            _ = await GXAgentWorker.client.PostAsJson<ClearDeviceErrorsResponse>("/api/DeviceError/Clear", req);
        }

        /// <inheritdoc/>
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            CloseDeviceError req = new CloseDeviceError() { Errors = errors.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddDeviceErrorResponse>("/api/DeviceError/Close", req);
        }

        /// <inheritdoc/>
        public async Task<GXDeviceError[]> ListAsync(
            ClaimsPrincipal User, 
            ListDeviceErrors? request, 
            ListDeviceErrorsResponse? response,
            CancellationToken cancellationToken)
        {
            ListDeviceErrorsResponse? ret = await GXAgentWorker.client.PostAsJson<ListDeviceErrorsResponse>("/api/DeviceError/List", request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Errors = ret.Errors;
            }
            return ret.Errors;
        }

        /// <inheritdoc/>
        public async Task<GXDeviceError> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return await Helpers.GetAsync<GXDeviceError>(string.Format("/api/DeviceError/?Id={0}", id));
        }
    }
}