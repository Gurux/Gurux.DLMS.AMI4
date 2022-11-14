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
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the agent errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentLogController : ControllerBase
    {
        private readonly IAgentLogRepository _agentErrorRepository;

        public AgentLogController(IAgentLogRepository agentErrorRepository)
        {
            _agentErrorRepository = agentErrorRepository;
        }

        /// <summary>
        /// Get agent error information.
        /// </summary>
        /// <param name="id">Agent error id.</param>
        /// <returns>Agent error.</returns>
        [HttpGet]
        [Authorize(Policy = GXAgentLogPolicies.View)]
        public async Task<ActionResult<GXAgentLog>> Get(Guid id)
        {
            return await _agentErrorRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Add agent Error(s).
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXAgentLogPolicies.Add)]
        public async Task<ActionResult<AddAgentLogResponse>> Post(AddAgentLog request)
        {
            await _agentErrorRepository.AddAsync(User, request.Logs);
            AddAgentLogResponse response = new AddAgentLogResponse();
            return response;
        }

        /// <summary>
        /// List agent Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXAgentLogPolicies.View)]
        public async Task<ActionResult<ListAgentLogsResponse>> Post(
            ListAgentLogs request, 
            CancellationToken cancellationToken)
        {
            ListAgentLogsResponse response = new ListAgentLogsResponse();
            await _agentErrorRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear agent error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXAgentLogPolicies.Clear)]
        public async Task<ActionResult<ClearAgentLogsResponse>> Post(ClearAgentLogs request)
        {
            await _agentErrorRepository.ClearAsync(User, request.Agents);
            return new ClearAgentLogsResponse();
        }

        /// <summary>
        /// Close agent error(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXAgentLogPolicies.Close)]
        public async Task<ActionResult<CloseAgentLogResponse>> Post(CloseAgentLog request)
        {
            await _agentErrorRepository.CloseAsync(User, request.Logs);
            return new CloseAgentLogResponse();
        }
    }
}
