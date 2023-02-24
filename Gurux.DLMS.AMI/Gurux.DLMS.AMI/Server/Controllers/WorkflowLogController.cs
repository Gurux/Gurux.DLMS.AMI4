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
    /// This controller is used to handle workflow logs.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowLogController : ControllerBase
    {
        private readonly IWorkflowLogRepository _workflowLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowLogController(IWorkflowLogRepository workflowLogRepository)
        {
            _workflowLogRepository = workflowLogRepository;
        }

        /// <summary>
        /// Get workflow log information.
        /// </summary>
        /// <param name="id">Workflow log id.</param>
        /// <returns>Workflow log.</returns>
        [HttpGet]
        [Authorize(Policy = GXWorkflowLogPolicies.View)]
        public async Task<ActionResult<GetWorkflowLogResponse>> Get(Guid id)
        {
            return new GetWorkflowLogResponse()
            {
                Item = await _workflowLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add workflow log.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXWorkflowLogPolicies.Add)]
        public async Task<ActionResult<AddWorkflowLogResponse>> Post(AddWorkflowLog request)
        {
            if (request.Logs == null || request.Logs.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _workflowLogRepository.AddAsync(User, request.Logs);
            AddWorkflowLogResponse response = new AddWorkflowLogResponse();
            return response;
        }

        /// <summary>
        /// List workflow log.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXWorkflowLogPolicies.View)]
        public async Task<ActionResult<ListWorkflowLogsResponse>> Post(
            ListWorkflowLogs request,
            CancellationToken cancellationToken)
        {
            ListWorkflowLogsResponse response = new ListWorkflowLogsResponse();
            await _workflowLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear workflow log(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXWorkflowLogPolicies.Clear)]
        public async Task<ActionResult<ClearWorkflowLogsResponse>> Post(ClearWorkflowLogs request)
        {
            await _workflowLogRepository.ClearAsync(User, request.Workflows);
            return new ClearWorkflowLogsResponse();
        }

        /// <summary>
        /// Close workflow log(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXWorkflowLogPolicies.Close)]
        public async Task<ActionResult<CloseWorkflowLogResponse>> Post(CloseWorkflowLog request)
        {
            await _workflowLogRepository.CloseAsync(User, request.Logs);
            return new CloseWorkflowLogResponse();
        }
    }
}
