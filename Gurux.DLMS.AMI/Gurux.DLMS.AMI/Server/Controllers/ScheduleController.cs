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
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the schedule.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleRepository _schedulerRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleController(IScheduleRepository schedulerRepository)
        {
            _schedulerRepository = schedulerRepository;
        }

        /// <summary>
        /// Get schedule information.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <returns>Schedule information.</returns>
        [HttpGet]
        [Authorize(Policy = GXSchedulePolicies.View)]
        public async Task<ActionResult<GetScheduleResponse>> Get(Guid id)
        {
            return new GetScheduleResponse()
            {
                Item = await _schedulerRepository.ReadAsync(User, id)
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
            ListSchedulesResponse ret = new ListSchedulesResponse();
            await _schedulerRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Schedule.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXSchedulePolicies.Add)]
        public async Task<ActionResult<UpdateScheduleResponse>> Post(UpdateSchedule request)
        {
            if (request.Schedules == null || request.Schedules.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateScheduleResponse ret = new UpdateScheduleResponse();
            ret.ScheduleIds = await _schedulerRepository.UpdateAsync(User, request.Schedules);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXSchedulePolicies.Delete)]
        public async Task<ActionResult<RemoveScheduleResponse>> Post(RemoveSchedule request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _schedulerRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveScheduleResponse();
        }

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <remarks>
        /// This can be used for testing the schedule.
        /// </remarks>
        [HttpGet("Run")]
        [Authorize(Policy = GXWorkflowPolicies.Edit)]
        public async Task<ActionResult> Run(Guid id)
        {
            await _schedulerRepository.RunAsync(User, id);
            return Ok();
        }
    }
}
