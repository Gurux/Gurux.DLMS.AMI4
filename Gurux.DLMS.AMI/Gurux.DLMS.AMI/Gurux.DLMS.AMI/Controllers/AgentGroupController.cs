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
    /// This controller is used to handle the agent groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentGroupController(
        IAgentGroupRepository AgentGroupRepository) : ControllerBase
    {
        private readonly IAgentGroupRepository _AgentGroupRepository = AgentGroupRepository;

        /// <summary>
        /// Get agent group information.
        /// </summary>
        /// <param name="id">Agent group id.</param>
        /// <returns>Agent group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXAgentGroupPolicies.View)]
        public async Task<ActionResult<GetAgentGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetAgentGroupResponse()
            {
                Item = (await _AgentGroupRepository.ReadAsync(id, null, cancellationToken))
            };
        }

        /// <summary>
        /// Update agent group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAgentGroupPolicies.Add)]
        public async Task<ActionResult<AddAgentGroupResponse>> Post(AddAgentGroup request, CancellationToken cancellationToken = default)
        {
            if (request.AgentGroups?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            var ids = await _AgentGroupRepository.UpsertAsync(request.AgentGroups, null, cancellationToken);
            return new AddAgentGroupResponse() { Ids = ids };
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
            ListAgentGroupsResponse ret = new();
            await _AgentGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete agent groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAgentGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveAgentGroupResponse>> Post(RemoveAgentGroup request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _AgentGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveAgentGroupResponse();
        }
    }
}
