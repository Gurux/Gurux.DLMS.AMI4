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
    /// This controller is used to handle the schedule groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleGroupController(
        IScheduleGroupRepository ScheduleGroupRepository) : ControllerBase
    {
        private readonly IScheduleGroupRepository _ScheduleGroupRepository = ScheduleGroupRepository;

        /// <summary>
        /// Get schedule group information.
        /// </summary>
        /// <param name="id">Schedule group id.</param>
        /// <returns>Schedule group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXScheduleGroupPolicies.View)]
        public async Task<ActionResult<GetScheduleGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetScheduleGroupResponse()
            {
                Item = await _ScheduleGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update schedule group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScheduleGroupPolicies.Add)]
        public async Task<ActionResult<AddScheduleGroupResponse>> Post(AddScheduleGroup request, CancellationToken cancellationToken = default)
        {
            await _ScheduleGroupRepository.UpsertAsync(request.ScheduleGroups, null, cancellationToken);
            return new AddScheduleGroupResponse()
            {
                Ids = [.. request.ScheduleGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List schedule groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScheduleGroupPolicies.View)]
        public async Task<ActionResult<ListScheduleGroupsResponse>> Post(
            ListScheduleGroups request,
            CancellationToken cancellationToken)
        {
            ListScheduleGroupsResponse ret = new();
            await _ScheduleGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the remove schedule group request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXScheduleGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveScheduleGroupResponse>> Post(RemoveScheduleGroup request, CancellationToken cancellationToken = default)
        {
            await _ScheduleGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveScheduleGroupResponse();
        }
    }
}
