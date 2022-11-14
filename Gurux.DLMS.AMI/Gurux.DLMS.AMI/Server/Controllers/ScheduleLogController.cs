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

using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Threading;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the schedule errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleLogController : ControllerBase
    {
        private readonly IScheduleLogRepository _scheduleLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleLogController(IScheduleLogRepository scheduleLogRepository)
        {
            _scheduleLogRepository = scheduleLogRepository;
        }

        /// <summary>
        /// Get schedule log information.
        /// </summary>
        /// <param name="id">Schedule log id.</param>
        /// <returns>Schedule log.</returns>
        [HttpGet]
        [Authorize(Policy = GXScheduleLogPolicies.View)]
        public async Task<ActionResult<GXScheduleLog>> Get(Guid id)
        {
            return await _scheduleLogRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Add schedule log.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXScheduleLogPolicies.Add)]
        public async Task<ActionResult<AddScheduleLogResponse>> Post(AddScheduleLog request)
        {
            await _scheduleLogRepository.AddAsync(User, request.Logs);
            AddScheduleLogResponse response = new AddScheduleLogResponse();
            return response;
        }

        /// <summary>
        /// List schedule logs.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScheduleLogPolicies.View)]
        public async Task<ActionResult<ListScheduleLogsResponse>> Post(
            ListScheduleLogs request, 
            CancellationToken cancellationToken)
        {
            ListScheduleLogsResponse response = new ListScheduleLogsResponse();
            await _scheduleLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear schedule error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXScheduleLogPolicies.Clear)]
        public async Task<ActionResult<ClearScheduleLogsResponse>> Post(ClearScheduleLogs request)
        {
            await _scheduleLogRepository.ClearAsync(User, request.Schedules);
            return new ClearScheduleLogsResponse();
        }

        /// <summary>
        /// Close schedule error(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXScheduleLogPolicies.Close)]
        public async Task<ActionResult<CloseScheduleLogResponse>> Post(CloseScheduleLog request)
        {
            await _scheduleLogRepository.CloseAsync(User, request.Logs);
            return new CloseScheduleLogResponse();
        }
    }
}
