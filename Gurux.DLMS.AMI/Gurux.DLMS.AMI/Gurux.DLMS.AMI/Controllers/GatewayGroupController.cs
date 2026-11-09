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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the gateway groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayGroupController(
        IGatewayGroupRepository GatewayGroupRepository) : ControllerBase
    {
        private readonly IGatewayGroupRepository _GatewayGroupRepository = GatewayGroupRepository;

        /// <summary>
        /// Get gateway group information.
        /// </summary>
        /// <param name="id">Gateway group id.</param>
        /// <returns>Gateway group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXGatewayGroupPolicies.View)]
        public async Task<ActionResult<GetGatewayGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetGatewayGroupResponse()
            {
                Item = (await _GatewayGroupRepository.ReadAsync(id, null, cancellationToken))
            };
        }

        /// <summary>
        /// Update gateway group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXGatewayGroupPolicies.Add)]
        public async Task<ActionResult<AddGatewayGroupResponse>> Post(AddGatewayGroup request, CancellationToken cancellationToken = default)
        {
            await _GatewayGroupRepository.UpsertAsync(request.GatewayGroups, null, cancellationToken);
            return new AddGatewayGroupResponse() { Ids = [.. request.GatewayGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List gateway groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXGatewayGroupPolicies.View)]
        public async Task<ActionResult<ListGatewayGroupsResponse>> Post(
            ListGatewayGroups request,
            CancellationToken cancellationToken)
        {
            ListGatewayGroupsResponse ret = new();
            await _GatewayGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes the specified gateway groups based on provided IDs and delete option.
        /// </summary>
        /// <param name="request">Contains the IDs of the gateway groups to delete and a flag indicating whether to perform a soft delete.</param>
        /// <returns>A response indicating the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXGatewayGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveGatewayGroupResponse>> Post(RemoveGatewayGroup request, CancellationToken cancellationToken = default)
        {
            await _GatewayGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveGatewayGroupResponse();
        }
    }
}
