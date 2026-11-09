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
    /// This controller is used to handle trigger groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class TriggerGroupController(
        ITriggerGroupRepository TriggerGroupRepository) : ControllerBase
    {
        private readonly ITriggerGroupRepository _TriggerGroupRepository = TriggerGroupRepository;

        /// <summary>
        /// Get trigger group information.
        /// </summary>
        /// <param name="id">Trigger group id.</param>
        /// <returns>Trigger group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXTriggerGroupPolicies.View)]
        public async Task<ActionResult<GetTriggerGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetTriggerGroupResponse()
            {
                Item = await _TriggerGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update trigger group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXTriggerGroupPolicies.Add)]
        public async Task<ActionResult<AddTriggerGroupResponse>> Post(AddTriggerGroup request, CancellationToken cancellationToken = default)
        {
            if (request.TriggerGroups?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _TriggerGroupRepository.UpsertAsync(request.TriggerGroups, null, cancellationToken);
            return new AddTriggerGroupResponse()
            {
                Ids = [.. request.TriggerGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List trigger groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTriggerGroupPolicies.View)]
        public async Task<ActionResult<ListTriggerGroupsResponse>> Post(
            ListTriggerGroups request,
            CancellationToken cancellationToken)
        {
            ListTriggerGroupsResponse ret = new();
            await _TriggerGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the remove trigger group request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXTriggerGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveTriggerGroupResponse>> Post(RemoveTriggerGroup request, CancellationToken cancellationToken = default)
        {
            await _TriggerGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveTriggerGroupResponse();
        }
    }
}
