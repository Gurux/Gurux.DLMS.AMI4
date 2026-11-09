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
using Gurux.DLMS.AMI.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Update;
using Gurux.Updater.Enums;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the agent information.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    /// <param name="agentRepository">Agent repository interface.</param>
    /// <param name="cron">Cron task interface.</param>
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController(IAgentRepository agentRepository, IGXBackgroundService cron) : ControllerBase
    {
        private readonly IAgentRepository _agentRepository = agentRepository;
        private readonly IGXBackgroundService _cron = cron;

        /// <summary>
        /// Get agent information.
        /// </summary>
        /// <param name="id">Agent id.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Agent information.</returns>
        [HttpGet]
        [Authorize(Policy = GXAgentPolicies.View)]
        public async Task<ActionResult<GetAgentResponse>> Get(Guid id,
            CancellationToken cancellationToken = default)
        {
            return new GetAgentResponse()
            {
                Item = await _agentRepository.ReadAsync(id, null, cancellationToken)
            };
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
            ListAgentsResponse ret = new();
            await _agentRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// List agents.
        /// </summary>
        [HttpPost("Installers")]
        [Authorize(Policy = GXAgentPolicies.View)]
        public async Task<ActionResult<ListAgentInstallersResponse>> Post(ListAgentInstallers request,
            CancellationToken cancellationToken)
        {
            ListAgentInstallersResponse ret = new();
            await _agentRepository.ListInstallersAsync(request, false, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Agent.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAgentPolicies.Add)]
        public async Task<ActionResult<UpdateAgentResponse>> Post(UpdateAgent request, CancellationToken cancellationToken = default)
        {
            if (request.Agents == null || !request.Agents.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            if (request.Agents.Any(a => a.Id == Guid.Empty && !a.Template.GetValueOrDefault(false)) &&
                await Gurux.DLMS.AMI.Hubs.GuruxAMiHub.AgentConnections.IsTokenInUseAsync(User.FindFirst("jti")?.Value))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Agent connection is already in use",
                    Detail = "The agent connection is already in use. Use a separate access token for another agent."
                });
            }
            UpdateAgentResponse ret = new()
            {
                AgentIds = await _agentRepository.UpsertAsync(request.Agents, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>Handles the remove agent request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAgentPolicies.Delete)]
        public async Task<ActionResult<RemoveAgentResponse>> Post(RemoveAgent request, CancellationToken cancellationToken)
        {
            await _agentRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveAgentResponse();
        }

        /// <summary>
        /// Update agent status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXAgentPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateAgentStatus request, CancellationToken cancellationToken = default)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _agentRepository.UpdateStatusAsync(request.Id,
                ServerHelpers.GetRemoteIpAddress(Request, HttpContext), request.Status, request.Data, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Download the agent version.
        /// </summary>
        [HttpPost("Download")]
        [Authorize(Policy = GXAgentPolicies.Add)]
        public async Task<ActionResult<DownloadAgentResponse>> Post(DownloadAgent request,
            CancellationToken cancellationToken = default)
        {
            string? version = null;
            bool requestedVersion = !string.IsNullOrEmpty(request.Agent?.UpdateVersion);
            if (requestedVersion)
            {
                if (!GXReleaseVersionComparer.TryNormalize(request.Agent!.UpdateVersion, out var number))
                {
                    return BadRequest("Invalid agent version.");
                }
                version = number;
            }
            // Resolve current catalog assets and compatibility even when an installer URL is cached.
            var catalogClient = HttpContext.RequestServices.GetRequiredService<GXCatalogClient>();
            var options = HttpContext.RequestServices.GetRequiredService<CatalogUpdaterOptions>();
            var catalog = await catalogClient.GetAsync(cancellationToken);
            var product = catalog.Items.SingleOrDefault(item => item.Id == "Gurux.DLMS.AMI.Agent" && item.Type == CatalogProductType.Agent);
            List<string> urls = [];
            if (product != null)
            {
                var pattern = options.AssetPatterns.GetValueOrDefault(product.Id) ?? options.DefaultAgentAssetPattern;
                var releases = await GXCatalogReleaseSelector.SelectAsync(product, pattern);
                var release = requestedVersion
                    ? releases.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Url) &&
                        GXReleaseVersionComparer.Instance.Compare(item.Number, version) == 0)
                    : GXCatalogReleaseSelector.Latest(releases, options.IncludePrereleases);
                if (!string.IsNullOrWhiteSpace(release?.Url))
                {
                    urls.Add(release.Url);
                }
            }
            if (urls.Count == 0)
            {
                return NotFound("No compatible downloadable agent release was found in the configured catalog.");
            }
            if (request.Agent != null && request.Agent.Id != Guid.Empty)
            {
                await _agentRepository.UpdateStatusAsync(request.Agent.Id,
                    ServerHelpers.GetRemoteIpAddress(Request, HttpContext),
                    AgentStatus.Downloading, null, cancellationToken);
            }
            return new DownloadAgentResponse() { Urls = [.. urls] };
        }

        /// <summary>
        /// Install agent version.
        /// </summary>
        [HttpPost("Install")]
        [Authorize(Policy = GXAgentPolicies.Edit)]
        public async Task<ActionResult<InstallAgentResponse>> Post(InstallAgent request, CancellationToken cancellationToken = default)
        {
            if (request.Agents == null || !request.Agents.Any())
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _agentRepository.UpgradeAsync(request.Agents, cancellationToken);
            return new InstallAgentResponse();
        }

        /// <summary>
        /// Check are there new agent versions available.
        /// </summary>
        [HttpPost("Check")]
        [Authorize(Policy = GXAgentPolicies.Edit)]
        public async Task<ActionResult<CheckModuleResponse>> Post(
            CheckModule request, CancellationToken cancellationToken = default)
        {
            _ = request;
            var runId = await _cron.StartAsync(GXBackgroundTaskIds.AgentUpdate, User, cancellationToken);
            return new CheckModuleResponse { RunId = runId };
        }
    }
}
