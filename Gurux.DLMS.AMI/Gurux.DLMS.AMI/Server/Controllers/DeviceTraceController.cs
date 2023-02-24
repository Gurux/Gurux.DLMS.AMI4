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
    /// This controller is used to handle the device traces.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTraceController : ControllerBase
    {
        private readonly IDeviceTraceRepository _deviceTraceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTraceController(
            IDeviceTraceRepository deviceTraceRepository)
        {
            _deviceTraceRepository = deviceTraceRepository;
        }

        /// <summary>
        /// Get device trace information.
        /// </summary>
        /// <param name="id">Device trace id.</param>
        /// <returns>Device trace.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceTracePolicies.View)]
        public async Task<ActionResult<GetDeviceTrace>> Get(Guid id)
        {
            return new GetDeviceTrace()
            {
                Item = await _deviceTraceRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List device traces.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTracePolicies.View)]
        public async Task<ActionResult<ListDeviceTraceResponse>> Post(
            ListDeviceTrace request,
            CancellationToken cancellationToken)
        {
            ListDeviceTraceResponse ret = new ListDeviceTraceResponse();
            await _deviceTraceRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New device trace is added.
        /// </summary>
        /// <param name="request">Device trace item.</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXDeviceTracePolicies.Add)]
        public async Task<ActionResult<AddDeviceTraceResponse>> Post(AddDeviceTrace request)
        {
            AddDeviceTraceResponse ret = new AddDeviceTraceResponse();
            await _deviceTraceRepository.AddAsync(User, request.Traces);
            return ret;
        }

        /// <summary>
        /// New Device trace is added..
        /// </summary>
        /// <param name="request">Device trace item.</param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXDeviceTracePolicies.Clear)]
        public async Task<ActionResult<ClearDeviceTraceResponse>> Post(ClearDeviceTrace request)
        {
            ClearDeviceTraceResponse ret = new ClearDeviceTraceResponse();
            await _deviceTraceRepository.ClearAsync(User, request.Ids);
            return ret;
        }
    }
}