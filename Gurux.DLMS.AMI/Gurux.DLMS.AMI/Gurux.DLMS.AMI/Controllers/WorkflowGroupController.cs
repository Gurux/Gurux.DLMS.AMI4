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
    /// This controller is used to handle workflow groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowGroupController(
        IWorkflowGroupRepository WorkflowGroupRepository) : ControllerBase
    {
        private readonly IWorkflowGroupRepository _WorkflowGroupRepository = WorkflowGroupRepository;

        /// <summary>
        /// Get workflow group information.
        /// </summary>
        /// <param name="id">Workflow group id.</param>
        /// <returns>Workflow group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXWorkflowGroupPolicies.View)]
        public async Task<ActionResult<GetWorkflowGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetWorkflowGroupResponse()
            {
                Item = await _WorkflowGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update workflow group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXWorkflowGroupPolicies.Edit)]
        public async Task<ActionResult<AddWorkflowGroupResponse>> Post(AddWorkflowGroup request, CancellationToken cancellationToken = default)
        {
            return new AddWorkflowGroupResponse()
            {
                Ids = await _WorkflowGroupRepository.UpsertAsync(request.WorkflowGroups, null, cancellationToken)
            };
        }

        /// <summary>
        /// List workflow groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXWorkflowGroupPolicies.View)]
        public async Task<ActionResult<ListWorkflowGroupsResponse>> Post(
            ListWorkflowGroups? request,
            CancellationToken cancellationToken)
        {
            ListWorkflowGroupsResponse ret = new();
            await _WorkflowGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the remove workflow group request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXWorkflowGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveWorkflowGroupResponse>> Post(RemoveWorkflowGroup request, CancellationToken cancellationToken = default)
        {
            await _WorkflowGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveWorkflowGroupResponse();
        }
    }
}
