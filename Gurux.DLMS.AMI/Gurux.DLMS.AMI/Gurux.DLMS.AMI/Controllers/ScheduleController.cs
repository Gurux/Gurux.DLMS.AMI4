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
    /// This controller is used to handle the schedule.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController(IScheduleRepository schedulerRepository) : ControllerBase
    {
        private readonly IScheduleRepository _schedulerRepository = schedulerRepository;

        /// <summary>
        /// Get schedule information.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <returns>Schedule information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXSchedulePolicies.View)]
        public async Task<ActionResult<GetScheduleResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetScheduleResponse()
            {
                Item = await _schedulerRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List Schedules.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXSchedulePolicies.View)]
        public async Task<ActionResult<ListSchedulesResponse>> Post(
            ListSchedules request,
            CancellationToken cancellationToken)
        {
            ListSchedulesResponse ret = new();
            await _schedulerRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Schedule.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXSchedulePolicies.Add)]
        public async Task<ActionResult<UpdateScheduleResponse>> Post(UpdateSchedule request, CancellationToken cancellationToken = default)
        {
            UpdateScheduleResponse ret = new()
            {
                Ids = await _schedulerRepository.UpsertAsync(request.Schedules, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes schedules based on the provided IDs and delete option.
        /// </summary>
        /// <param name="request">The request containing schedule IDs and a flag indicating whether to perform a soft delete.</param>
        /// <returns>An action result containing the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXSchedulePolicies.Delete)]
        public async Task<ActionResult<RemoveScheduleResponse>> Post(RemoveSchedule request, CancellationToken cancellationToken = default)
        {
            await _schedulerRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveScheduleResponse();
        }

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <remarks>
        /// This can be used for testing the schedule.
        /// </remarks>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Run")]
        [Authorize(Policy = GXWorkflowPolicies.Edit)]
        public async Task<ActionResult> Run(Guid id, CancellationToken cancellationToken = default)
        {
            await _schedulerRepository.RunAsync(id, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Get module settings for the schedule.
        /// </summary>
        [HttpPost("GetModuleSettings")]
        [Authorize(Policy = GXSchedulePolicies.View)]
        public async Task<ActionResult<GetModuleSettingsResponse>> Post(
            GetModuleSettings request, CancellationToken cancellationToken = default)
        {
            GetModuleSettingsResponse ret = new()
            {
                Item = await _schedulerRepository.GetModuleSettingsAsync(request.Item, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Update module settings to the schedule.
        /// </summary>
        [HttpPost("UpdateModuleSettings")]
        [Authorize(Policy = GXSchedulePolicies.View)]
        public async Task<ActionResult<UpdateModuleSettingsResponse>> Post(
            UpdateModuleSettings request, CancellationToken cancellationToken = default)
        {
            UpdateModuleSettingsResponse ret = new();
            await _schedulerRepository.UpdateModuleSettingsAsync(request.Item, cancellationToken);
            return ret;
        }
    }
}
