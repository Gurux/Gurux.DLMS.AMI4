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
    /// This controller is used to handle the agent groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentGroupController : ControllerBase
    {
        private readonly IAgentGroupRepository _AgentGroupRepository;

        public AgentGroupController(
            IAgentGroupRepository AgentGroupRepository)
        {
            _AgentGroupRepository = AgentGroupRepository;
        }

        /// <summary>
        /// Get agent group information.
        /// </summary>
        /// <param name="id">Agent group id.</param>
        /// <returns>Agent group.</returns>
        [HttpGet]
        [Authorize(Policy = GXAgentGroupPolicies.View)]
        public async Task<ActionResult<GXAgentGroup>> Get(Guid id)
        {
            return await _AgentGroupRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Update agent group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAgentGroupPolicies.Add)]
        public async Task<ActionResult<AddAgentGroupResponse>> Post(AddAgentGroup request)
        {
            await _AgentGroupRepository.UpdateAsync(User, request.AgentGroups);
            return new AddAgentGroupResponse() { AgentGroups = request.AgentGroups };
        }

        /// <summary>
        /// List agent groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXAgentGroupPolicies.View)]
        public async Task<ActionResult<ListAgentGroupsResponse>> Post(
            ListAgentGroups request, 
            CancellationToken cancellationToken)
        {
            ListAgentGroupsResponse ret = new ListAgentGroupsResponse();
            await _AgentGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXAgentGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveAgentGroupResponse>> Post(RemoveAgentGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _AgentGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveAgentGroupResponse();
        }
    }
}
