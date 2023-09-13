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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the gateway information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayController : ControllerBase
    {
        private readonly IGatewayRepository _gatewayRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gatewayRepository">Gateway repository interface.</param>
        public GatewayController(IGatewayRepository gatewayRepository)
        {
            _gatewayRepository = gatewayRepository;
        }

        /// <summary>
        /// Get gateway information.
        /// </summary>
        /// <param name="id">Gateway id.</param>
        /// <returns>Gateway information.</returns>
        [HttpGet]
        [Authorize(Policy = GXGatewayPolicies.View)]
        public async Task<ActionResult<GetGatewayResponse>> Get(Guid id)
        {
            return new GetGatewayResponse()
            {
                Item = await _gatewayRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List gateways.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXGatewayPolicies.View)]
        public async Task<ActionResult<ListGatewaysResponse>> Post(
            ListGateways request,
            CancellationToken cancellationToken)
        {
            ListGatewaysResponse ret = new ListGatewaysResponse();
            await _gatewayRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }       

        /// <summary>
        /// Update Gateway.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXGatewayPolicies.Add)]
        public async Task<ActionResult<UpdateGatewayResponse>> Post(UpdateGateway request)
        {
            if (request.Gateways == null || !request.Gateways.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateGatewayResponse ret = new UpdateGatewayResponse();
            ret.GatewayIds = await _gatewayRepository.UpdateAsync(User, request.Gateways);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXGatewayPolicies.Delete)]
        public async Task<ActionResult<RemoveGatewayResponse>> Post(RemoveGateway request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _gatewayRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveGatewayResponse();
        }

        /// <summary>
        /// Update gateway status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXGatewayPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateGatewayStatus request)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _gatewayRepository.UpdateStatusAsync(User, request.Id, request.Status);
            return Ok();
        }            
    }
}
