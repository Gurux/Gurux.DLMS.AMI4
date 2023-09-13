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
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Repository;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the devices.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceRepository _deviceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceController(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        /// <summary>
        /// Update device.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDevicePolicies.Add)]
        public async Task<ActionResult<UpdateDeviceResponse>> Post(UpdateDevice request, CancellationToken cancellationToken)
        {
            if (request.Devices == null || !request.Devices.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateDeviceResponse ret = new UpdateDeviceResponse();
            ret.Ids = (await _deviceRepository.UpdateAsync(User,
                request.Devices,
                cancellationToken,
                null,
                request.LateBinding));
            return ret;
        }

        /// <summary>
        /// Get available devices.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXDevicePolicies.View)]
        public async Task<ActionResult<ListDevicesResponse>> Post(
            ListDevices request,
            CancellationToken cancellationToken)
        {
            ListDevicesResponse ret = new ListDevicesResponse();
            await _deviceRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get device information.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <returns>Device information.</returns>
        [HttpGet]
        [Authorize(Policy = GXDevicePolicies.View)]
        public async Task<ActionResult<GetDeviceResponse>> Get(Guid id)
        {
            return new GetDeviceResponse()
            {
                Item = await _deviceRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Remove selected device
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDevicePolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceResponse>> Post(RemoveDevice request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _deviceRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveDeviceResponse();
        }

        /// <summary>
        /// Update device status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXDevicePolicies.Edit)]
        public async Task<ActionResult> Post(UpdateDeviceStatus request)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _deviceRepository.UpdateStatusAsync(User, request.Id, request.Status);
            return Ok();
        }
    }
}
