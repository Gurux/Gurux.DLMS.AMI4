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
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Pages.Agent;
using System;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the agent information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly IAgentRepository _agentRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="agentRepository">Agent repository interface.</param>
        public AgentController(IAgentRepository agentRepository)
        {
            _agentRepository = agentRepository;
        }

        /// <summary>
        /// Get agent information.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <returns>Agent information.</returns>
        [HttpGet]
        [Authorize(Policy = GXAgentPolicies.View)]
        public async Task<ActionResult<GXAgent>> Get(Guid id)
        {
            return await _agentRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// List agents.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXAgentPolicies.View)]
        public async Task<ActionResult<ListAgentsResponse>> Post(
            ListAgents request,
            CancellationToken cancellationToken)
        {
            ListAgentsResponse ret = new ListAgentsResponse();
            await _agentRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// List agents.
        /// </summary>
        [HttpPost("Installers")]
        [Authorize(Policy = GXAgentPolicies.View)]
        public async Task<ActionResult<ListAgentInstallersResponse>> Post(ListAgentInstallers request)
        {
            ListAgentInstallersResponse ret = new ListAgentInstallersResponse();
            await _agentRepository.ListInstallersAsync(User, request, ret);
            return ret;
        }

        /// <summary>
        /// Update Agent.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAgentPolicies.Add)]
        public async Task<ActionResult<UpdateAgentResponse>> Post(UpdateAgent request)
        {
            if (request.Agents == null || !request.Agents.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateAgentResponse ret = new UpdateAgentResponse();
            ret.AgentIds = await _agentRepository.UpdateAsync(User, request.Agents);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXAgentPolicies.Delete)]
        public async Task<ActionResult<AgentDeleteResponse>> Post(AgentDelete request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _agentRepository.DeleteAsync(User, request.Ids);
            return new AgentDeleteResponse();
        }

        /// <summary>
        /// Update agent status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXAgentPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateAgentStatus request)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _agentRepository.UpdateStatusAsync(User, request.Id, request.Status);
            return Ok();
        }

        /// <summary>
        /// Download the agent version.
        /// </summary>
        [HttpPost("Download")]
        [Authorize(Policy = GXAgentPolicies.Add)]
        public async Task<ActionResult<DownloadAgentResponse>> Post(DownloadAgent request, CancellationToken token)
        {
            Version? version = null;
            var installedAgents = await _agentRepository.ListInstallersAsync(User, null, null);
            if (request.Agent == null || request.Agent.Id == Guid.Empty ||
                string.IsNullOrEmpty(request.Agent.UpdateVersion))
            {
                //Find the latest version if version is not given.
                foreach (var agent in installedAgents)
                {
                    foreach (var it in agent.Versions)
                    {
                        if (!string.IsNullOrEmpty(it.Number))
                        {
                            if (version == null || version < new Version(it.Number))
                            {
                                version = new Version(it.Number);
                            }
                        }
                    }
                }
            }
            else
            {
                version = new Version(request.Agent.UpdateVersion);
            }
            List<string> urls = new List<string>();
            foreach (var agent in installedAgents)
            {
                foreach (var it in agent.Versions)
                {
                    if (!string.IsNullOrEmpty(it.Url) && it.Number != null && new Version(it.Number) == version)
                    {
                        urls.Add(it.Url);
                        break;
                    }
                }
            }
            if (request.Agent != null)
            {
                await _agentRepository.UpdateStatusAsync(User, request.Agent.Id, AgentStatus.Downloading);
            }
            return new DownloadAgentResponse() { Urls = urls.ToArray() };
        }
    }
}
