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
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the gateway errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayLogController : ControllerBase
    {
        private readonly IGatewayLogRepository _gatewayErrorRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayLogController(IGatewayLogRepository gatewayErrorRepository)
        {
            _gatewayErrorRepository = gatewayErrorRepository;
        }

        /// <summary>
        /// Get gateway error information.
        /// </summary>
        /// <param name="id">Gateway error id.</param>
        /// <returns>Gateway error.</returns>
        [HttpGet]
        [Authorize(Policy = GXGatewayLogPolicies.View)]
        public async Task<ActionResult<GetGatewayLogResponse>> Get(Guid id)
        {
            return new GetGatewayLogResponse()
            {
                Item = await _gatewayErrorRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add gateway Error(s).
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXGatewayLogPolicies.Add)]
        public async Task<ActionResult<AddGatewayLogResponse>> Post(AddGatewayLog request)
        {
            await _gatewayErrorRepository.AddAsync(User, request.Logs);
            AddGatewayLogResponse response = new AddGatewayLogResponse();
            return response;
        }

        /// <summary>
        /// List gateway logs
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXGatewayLogPolicies.View)]
        public async Task<ActionResult<ListGatewayLogsResponse>> Post(
            ListGatewayLogs request,
            CancellationToken cancellationToken)
        {
            ListGatewayLogsResponse response = new ListGatewayLogsResponse();
            await _gatewayErrorRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear gateway logs(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXGatewayLogPolicies.Clear)]
        public async Task<ActionResult<ClearGatewayLogsResponse>> Post(ClearGatewayLogs request)
        {
            await _gatewayErrorRepository.ClearAsync(User, request.Gateways);
            return new ClearGatewayLogsResponse();
        }

        /// <summary>
        /// Close gateway logs(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXGatewayLogPolicies.Close)]
        public async Task<ActionResult<CloseGatewayLogResponse>> Post(CloseGatewayLog request)
        {
            await _gatewayErrorRepository.CloseAsync(User, request.Logs);
            return new CloseGatewayLogResponse();
        }
    }
}
