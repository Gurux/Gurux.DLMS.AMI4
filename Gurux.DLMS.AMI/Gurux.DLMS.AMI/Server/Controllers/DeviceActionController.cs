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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device actions.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceActionController : ControllerBase
    {
        private readonly IDeviceActionRepository _deviceActionRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceActionController(
            IDeviceActionRepository deviceActionRepository)
        {
            _deviceActionRepository = deviceActionRepository;
        }

        /// <summary>
        /// Get device action information.
        /// </summary>
        /// <param name="id">Device action id.</param>
        /// <returns>Device action.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceActionPolicies.View)]
        public async Task<ActionResult<GetDeviceActionResponse>> Get(Guid id)
        {
            return new GetDeviceActionResponse()
            {
                Item = await _deviceActionRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List device actions.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceActionPolicies.View)]
        public async Task<ActionResult<ListDeviceActionResponse>> Post(
        ListDeviceAction request,
        CancellationToken cancellationToken)
        {
            ListDeviceActionResponse ret = new ListDeviceActionResponse();
            await _deviceActionRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New device action is added.
        /// </summary>
        /// <param name="request">Device action item.</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXDeviceActionPolicies.Add)]
        public async Task<ActionResult<AddDeviceActionResponse>> Post(AddDeviceAction request)
        {
            AddDeviceActionResponse ret = new AddDeviceActionResponse();
            await _deviceActionRepository.AddAsync(User, request.Actions);
            return ret;
        }

        /// <summary>
        /// New Device action is added..
        /// </summary>
        /// <param name="request">Device action item.</param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXDeviceActionPolicies.Clear)]
        public async Task<ActionResult<ClearDeviceActionResponse>> Post(ClearDeviceAction request)
        {
            ClearDeviceActionResponse ret = new ClearDeviceActionResponse();
            await _deviceActionRepository.ClearAsync(User, request.Ids);
            return ret;
        }
    }
}