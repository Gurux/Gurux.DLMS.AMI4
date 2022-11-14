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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle workflow groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowGroupController : ControllerBase
    {
        private readonly IWorkflowGroupRepository _WorkflowGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowGroupController(
            IWorkflowGroupRepository WorkflowGroupRepository)
        {
            _WorkflowGroupRepository = WorkflowGroupRepository;
        }

        /// <summary>
        /// Get workflow group information.
        /// </summary>
        /// <param name="id">Workflow group id.</param>
        /// <returns>Workflow group.</returns>
        [HttpGet]
        [Authorize(Policy = GXWorkflowGroupPolicies.View)]
        public async Task<ActionResult<GXWorkflowGroup>> Get(Guid id)
        {
            return await _WorkflowGroupRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Update workflow group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXWorkflowGroupPolicies.Edit)]
        public async Task<ActionResult<AddWorkflowGroupResponse>> Post(AddWorkflowGroup request)
        {
            if (request.WorkflowGroups == null || request.WorkflowGroups.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _WorkflowGroupRepository.UpdateAsync(User, request.WorkflowGroups);
            return new AddWorkflowGroupResponse() { WorkflowGroups = request.WorkflowGroups };
        }

        /// <summary>
        /// List workflow groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXWorkflowGroupPolicies.View)]
        public async Task<ActionResult<ListWorkflowGroupsResponse>> Post(
            ListWorkflowGroups request,
            CancellationToken cancellationToken)
        {
            ListWorkflowGroupsResponse ret = new ListWorkflowGroupsResponse();
            await _WorkflowGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXWorkflowGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveWorkflowGroupResponse>> Post(RemoveWorkflowGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _WorkflowGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveWorkflowGroupResponse();
        }
    }
}
