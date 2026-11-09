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
    /// This controller is used to handle the agent settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentPropertiesController(IAgentPropertiesRepository agentSettingRepository) : ControllerBase
    {
        private readonly IAgentPropertiesRepository _agentSettingRepository = agentSettingRepository;

        /// <summary>
        /// Get agent settings.
        /// </summary>
        /// <param name="name">Agent settings name.</param>
        /// <param name="moduleId">Module ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="agentId">Optional settings owner; another agent requires administrator access.</param>
        /// <returns>Agent settings.</returns>
        [HttpGet]
        [Authorize(Policy = GXAgentPropertiesPolicies.View)]
        public async Task<ActionResult<GetAgentProperties>> Get(string agentId, string name, string moduleId, CancellationToken cancellationToken = default)
        {
            return new GetAgentProperties()
            {
                Item = await _agentSettingRepository.ReadAsync(agentId, name, moduleId, null, cancellationToken)
            };
        }

        /// <summary>
        /// Get agent settings.
        /// </summary>
        /// <param name="request">List agent settings parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of agent settings.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXAgentPropertiesPolicies.View)]
        public async Task<ActionResult<ListAgentPropertiesResponse>> Post(
            ListAgentProperties request,
            CancellationToken cancellationToken)
        {
            ListAgentPropertiesResponse ret = new();
            await _agentSettingRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update agent settings.
        /// </summary>
        /// <param name="settings">Updated agent settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost("Update")]
        [HttpPost("Add")]
        [Authorize(Policy = GXAgentPropertiesPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateAgentProperties settings, CancellationToken cancellationToken = default)
        {
            await _agentSettingRepository.UpsertAsync(settings.Properties, null, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Delete agent settings.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAgentPropertiesPolicies.Delete)]
        public async Task<ActionResult> Post(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _agentSettingRepository.DeleteAsync(ids, cancellationToken);
            return Ok();
        }
    }
}
