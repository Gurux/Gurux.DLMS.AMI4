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
    /// This controller is used to handle the component views.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentViewGroupController(
        IComponentViewGroupRepository ComponentViewGroupRepository) : ControllerBase
    {
        private readonly IComponentViewGroupRepository _componentViewGroupRepository = ComponentViewGroupRepository;

        /// <summary>
        /// Get component view group information.
        /// </summary>
        /// <param name="id">ComponentView group id.</param>
        /// <returns>ComponentView group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXComponentViewGroupPolicies.View)]
        public async Task<ActionResult<GetComponentViewGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetComponentViewGroupResponse()
            {
                Item = await _componentViewGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update component view group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXComponentViewGroupPolicies.Add)]
        public async Task<ActionResult<AddComponentViewGroupResponse>> Post(AddComponentViewGroup request, CancellationToken cancellationToken = default)
        {
            if (request.ComponentViewGroups?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _componentViewGroupRepository.UpsertAsync(request.ComponentViewGroups, null, cancellationToken);
            return new AddComponentViewGroupResponse()
            {
                Ids = [.. request.ComponentViewGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List component view groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXComponentViewGroupPolicies.View)]
        public async Task<ActionResult<ListComponentViewGroupsResponse>> Post(
            ListComponentViewGroups request,
            CancellationToken cancellationToken)
        {
            ListComponentViewGroupsResponse ret = new();
            await _componentViewGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// List component view groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXComponentViewGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveComponentViewGroupResponse>> Post(RemoveComponentViewGroup request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _componentViewGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveComponentViewGroupResponse();
        }
    }
}
