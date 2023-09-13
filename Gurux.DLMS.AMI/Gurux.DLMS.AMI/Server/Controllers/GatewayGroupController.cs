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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the gateway groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayGroupController : ControllerBase
    {
        private readonly IGatewayGroupRepository _GatewayGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayGroupController(
            IGatewayGroupRepository GatewayGroupRepository)
        {
            _GatewayGroupRepository = GatewayGroupRepository;
        }

        /// <summary>
        /// Get gateway group information.
        /// </summary>
        /// <param name="id">Gateway group id.</param>
        /// <returns>Gateway group.</returns>
        [HttpGet]
        [Authorize(Policy = GXGatewayGroupPolicies.View)]
        public async Task<ActionResult<GetGatewayGroupResponse>> Get(Guid id)
        {
            return new GetGatewayGroupResponse()
            {
                Item = (await _GatewayGroupRepository.ReadAsync(User, id))
            };
        }

        /// <summary>
        /// Update gateway group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXGatewayGroupPolicies.Add)]
        public async Task<ActionResult<AddGatewayGroupResponse>> Post(AddGatewayGroup request)
        {
            await _GatewayGroupRepository.UpdateAsync(User, request.GatewayGroups);
            return new AddGatewayGroupResponse() { Ids = request.GatewayGroups.Select(s => s.Id).ToArray() };
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
            ListGatewayGroupsResponse ret = new ListGatewayGroupsResponse();
            await _GatewayGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXGatewayGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveGatewayGroupResponse>> Post(RemoveGatewayGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _GatewayGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveGatewayGroupResponse();
        }
    }
}
