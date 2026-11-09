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
    /// This controller is used to handle triggers.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class TriggerController(ITriggerRepository triggerrRepository) : ControllerBase
    {
        private readonly ITriggerRepository _triggerRepository = triggerrRepository;

        /// <summary>
        /// Get trigger information.
        /// </summary>
        /// <param name="id">Trigger id.</param>
        /// <returns>Trigger information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXTriggerPolicies.View)]
        public async Task<ActionResult<GetTriggerResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetTriggerResponse()
            {
                Item = await _triggerRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List Triggers.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTriggerPolicies.View)]
        public async Task<ActionResult<ListTriggersResponse>> Post(
            ListTriggers request,
            CancellationToken cancellationToken)
        {
            ListTriggersResponse ret = new();
            await _triggerRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Trigger.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXTriggerPolicies.Add)]
        public async Task<ActionResult<UpdateTriggerResponse>> Post(UpdateTrigger request, CancellationToken cancellationToken = default)
            => await UpsertAsync(request, cancellationToken);

        /// <summary>Updates an existing trigger using trigger edit permission.</summary>
        [HttpPost("Update")]
        [Authorize(Policy = GXTriggerPolicies.Edit)]
        public async Task<ActionResult<UpdateTriggerResponse>> Update(UpdateTrigger request, CancellationToken cancellationToken = default)
            => await UpsertAsync(request, cancellationToken);

        private async Task<ActionResult<UpdateTriggerResponse>> UpsertAsync(UpdateTrigger request, CancellationToken cancellationToken)
        {
            if (request.Triggers == null || request.Triggers.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateTriggerResponse ret = new()
            {
                Ids = await _triggerRepository.UpsertAsync(request.Triggers, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes triggers specified in the request.
        /// </summary>
        /// <param name="request">Identifiers of the triggers to delete and a flag indicating whether to perform a soft delete.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXTriggerPolicies.Delete)]
        public async Task<ActionResult<RemoveTriggerResponse>> Post(RemoveTrigger request, CancellationToken cancellationToken = default)
        {
            await _triggerRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveTriggerResponse();
        }

        /// <summary>
        /// Add missing registered trigger defaults.
        /// </summary>
        [HttpPost("EnsureDefaults")]
        [Authorize(Policy = GXTriggerPolicies.Add)]
        public async Task<ActionResult<EnsureTriggerDefaultsResponse>> Post(EnsureTriggerDefaults request, CancellationToken cancellationToken = default)
        {
            _ = request;
            await _triggerRepository.EnsureDefaultsAsync(cancellationToken);
            return new EnsureTriggerDefaultsResponse();
        }
    }
}
