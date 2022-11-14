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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle workflows.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowRepository _workflowrRepository;
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowController(IWorkflowRepository workflowrRepository)
        {
            _workflowrRepository = workflowrRepository;
        }

        /// <summary>
        /// Get workflow information.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <returns>Workflow information.</returns>
        [HttpGet]
        [Authorize(Policy = GXWorkflowPolicies.View)]
        public async Task<ActionResult<GXWorkflow>> Get(Guid id)
        {
            return await _workflowrRepository.ReadAsync(User, id, false);
        }

        /// <summary>
        /// List Workflows.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXWorkflowPolicies.View)]
        public async Task<ActionResult<ListWorkflowsResponse>> Post(ListWorkflows request)
        {
            ListWorkflowsResponse ret = new();
            await _workflowrRepository.ListAsync(User, request, ret, false);
            return ret;
        }

        /// <summary>
        /// Update Workflow.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXWorkflowPolicies.Add)]
        public async Task<ActionResult<UpdateWorkflowResponse>> Post(UpdateWorkflow request)
        {
            if (request.Workflows == null || request.Workflows.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateWorkflowResponse ret = new()
            {
                WorkflowIds = await _workflowrRepository.UpdateAsync(User, request.Workflows)
            };
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXWorkflowPolicies.Delete)]
        public async Task<ActionResult<DeleteWorkflowResponse>> Post(DeleteWorkflow request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _workflowrRepository.DeleteAsync(User, request.Ids);
            return new DeleteWorkflowResponse();
        }

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <remarks>
        /// This can be used for testing the workflow.
        /// </remarks>
        [HttpGet("Run")]
        [Authorize(Policy = GXWorkflowPolicies.Edit)]
        public async Task<ActionResult> Run(Guid id)
        {
            await _workflowrRepository.RunAsync(User, id);
            return Ok();
        }
    }
}
