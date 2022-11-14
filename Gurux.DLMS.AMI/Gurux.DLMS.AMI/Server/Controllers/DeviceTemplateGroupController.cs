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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device template groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTemplateGroupController : ControllerBase
    {
        private readonly IDeviceTemplateGroupRepository _deviceTemplateGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateGroupController(
            IDeviceTemplateGroupRepository DeviceTemplateGroupRepository)
        {
            _deviceTemplateGroupRepository = DeviceTemplateGroupRepository;
        }

        /// <summary>
        /// Get device template group information.
        /// </summary>
        /// <param name="id">Device template group id.</param>
        /// <returns>Device template group information.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.View)]
        public async Task<ActionResult<GXDeviceTemplateGroup>> Get(Guid id)
        {
            return await _deviceTemplateGroupRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Update device template group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.Add)]
        public async Task<ActionResult<AddDeviceTemplateGroupResponse>> Post(AddDeviceTemplateGroup request)
        {
            await _deviceTemplateGroupRepository.UpdateAsync(User, request.DeviceTemplateGroups);
            return new AddDeviceTemplateGroupResponse() { DeviceTemplateGroups = request.DeviceTemplateGroups };
        }

        /// <summary>
        /// List device template groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.View)]
        public async Task<ActionResult<ListDeviceTemplateGroupsResponse>> Post(
            ListDeviceTemplateGroups request, CancellationToken cancellationToken)
        {
            ListDeviceTemplateGroupsResponse ret = new ListDeviceTemplateGroupsResponse();
            await _deviceTemplateGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove device template groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceTemplateGroupResponse>> Post(RemoveDeviceTemplateGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _deviceTemplateGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveDeviceTemplateGroupResponse();
        }
    }
}
