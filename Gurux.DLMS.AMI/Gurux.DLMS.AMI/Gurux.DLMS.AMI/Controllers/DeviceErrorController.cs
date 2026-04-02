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
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device errors.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceErrorController : ControllerBase
    {
        private readonly IDeviceErrorRepository _deviceErrorRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceErrorController(IDeviceErrorRepository deviceErrorRepository)
        {
            _deviceErrorRepository = deviceErrorRepository;
        }

        /// <summary>
        /// Get device error information.
        /// </summary>
        /// <param name="id">Device trace id.</param>
        /// <returns>Device trace.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceLogPolicies.View)]
        public async Task<ActionResult<GetDeviceErrorResponse>> Get(Guid id)
        {
            return new GetDeviceErrorResponse()
            {
                Item = await _deviceErrorRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Add device Error.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXDeviceLogPolicies.Add)]
        public async Task<ActionResult<AddDeviceErrorResponse>> Post(AddDeviceError request)
        {
            await _deviceErrorRepository.AddAsync(request.Type, request.Errors);
            AddDeviceErrorResponse response = new AddDeviceErrorResponse();
            return response;
        }

        /// <summary>
        /// List device Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceLogPolicies.View)]
        public async Task<ActionResult<ListDeviceErrorsResponse>> Post(
            ListDeviceErrors request,
            CancellationToken cancellationToken)
        {
            ListDeviceErrorsResponse response = new ListDeviceErrorsResponse();
            await _deviceErrorRepository.ListAsync(request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear device error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXDeviceLogPolicies.Clear)]
        public async Task<ActionResult<ClearDeviceErrorsResponse>> Post(ClearDeviceErrors request)
        {
            await _deviceErrorRepository.ClearAsync(request.Devices);
            return new ClearDeviceErrorsResponse();
        }

        /// <summary>
        /// Close device error(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXDeviceLogPolicies.Close)]
        public async Task<ActionResult<CloseDeviceErrorResponse>> Post(CloseDeviceError request)
        {
            await _deviceErrorRepository.CloseAsync(request.Errors);
            return new CloseDeviceErrorResponse();
        }
    }
}
