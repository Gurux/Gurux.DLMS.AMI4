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

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the gateway information.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    /// <param name="gatewayRepository">Gateway repository interface.</param>
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayController(IGatewayRepository gatewayRepository) : ControllerBase
    {
        private readonly IGatewayRepository _gatewayRepository = gatewayRepository;

        /// <summary>
        /// Get gateway information.
        /// </summary>
        /// <param name="id">Gateway id.</param>
        /// <returns>Gateway information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXGatewayPolicies.View)]
        public async Task<ActionResult<GetGatewayResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetGatewayResponse()
            {
                Item = await _gatewayRepository.ReadAsync(id, null, cancellationToken)
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
            ListGatewaysResponse ret = new();
            await _gatewayRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Gateway.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXGatewayPolicies.Add)]
        public async Task<ActionResult<UpdateGatewayResponse>> Post(UpdateGateway request, CancellationToken cancellationToken = default)
        {
            if (request.Gateways == null || !request.Gateways.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateGatewayResponse ret = new()
            {
                Ids = await _gatewayRepository.UpsertAsync(request.Gateways, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>Handles the remove gateway request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXGatewayPolicies.Delete)]
        public async Task<ActionResult<RemoveGatewayResponse>> Post(RemoveGateway request, CancellationToken cancellationToken = default)
        {
            await _gatewayRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveGatewayResponse();
        }

        /// <summary>
        /// Update gateway status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXGatewayPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateGatewayStatus request, CancellationToken cancellationToken = default)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _gatewayRepository.UpdateStatusAsync(request.Id, request.Status, cancellationToken);
            return Ok();
        }
    }
}
