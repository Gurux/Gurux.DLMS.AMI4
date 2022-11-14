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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceGroupController : ControllerBase
    {
        private readonly IDeviceGroupRepository _deviceGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceGroupController(
            IDeviceGroupRepository DeviceGroupRepository)
        {
            _deviceGroupRepository = DeviceGroupRepository;
        }

        /// <summary>
        /// Get device group information.
        /// </summary>
        /// <param name="id">Device group id.</param>
        /// <returns>Device group.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceGroupPolicies.View)]
        public async Task<ActionResult<GXDeviceGroup>> Get(Guid id)
        {
            return await _deviceGroupRepository.ReadAsync(User, id);
        }


        /// <summary>
        /// Update device group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceGroupPolicies.Add)]
        public async Task<ActionResult<AddDeviceGroupResponse>> Post(AddDeviceGroup request)
        {
            if (request.DeviceGroups == null || request.DeviceGroups.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _deviceGroupRepository.UpdateAsync(User, request.DeviceGroups);
            return new AddDeviceGroupResponse() { DeviceGroups = request.DeviceGroups };
        }

        /// <summary>
        /// List user groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceGroupPolicies.View)]
        public async Task<ActionResult<ListDeviceGroupsResponse>> Post(
            ListDeviceGroups request,
            CancellationToken cancellationToken)
        {
            ListDeviceGroupsResponse ret = new ListDeviceGroupsResponse();
            await _deviceGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceGroupResponse>> Post(RemoveDeviceGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _deviceGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveDeviceGroupResponse();
        }
    }
}
