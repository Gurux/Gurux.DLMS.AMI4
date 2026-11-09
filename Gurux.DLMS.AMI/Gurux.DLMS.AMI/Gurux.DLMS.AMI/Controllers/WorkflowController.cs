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
    /// This controller is used to handle workflows.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController(IWorkflowRepository workflowrRepository) : ControllerBase
    {
        private readonly IWorkflowRepository _workflowrRepository = workflowrRepository;

        /// <summary>Preview matching and execution identity without evaluating any scripts.</summary>
        [HttpPost("Preview")]
        [Authorize(Policy = GXWorkflowPolicies.View)]
        public async Task<ActionResult<PreviewWorkflowResponse>> Preview(PreviewWorkflow request,
            [FromServices] Gurux.DLMS.AMI.Db.IDbConnectionFactory factory,
            [FromServices] IAuthorizationService authorization, CancellationToken cancellationToken = default)
        {
            await using var connection = await factory.GetConnectionAsync(cancellationToken);
            return await Gurux.DLMS.AMI.Server.Internal.GXWorkflowPreview.PreviewAsync(
                connection, authorization, User, request, cancellationToken);
        }

        /// <summary>
        /// Get workflow information.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <returns>Workflow information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXWorkflowPolicies.View)]
        public async Task<ActionResult<GetWorkflowResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetWorkflowResponse()
            {
                Item = await _workflowrRepository.ReadAsync(id, false, null, cancellationToken)
            };
        }

        /// <summary>
        /// List Workflows.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXWorkflowPolicies.View)]
        public async Task<ActionResult<ListWorkflowsResponse>> Post(ListWorkflows request, CancellationToken cancellationToken = default)
        {
            ListWorkflowsResponse ret = new();
            await _workflowrRepository.ListAsync(request, ret, false, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Workflow.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXWorkflowPolicies.Add)]
        public async Task<ActionResult<UpdateWorkflowResponse>> Post(UpdateWorkflow request, CancellationToken cancellationToken = default)
        {
            UpdateWorkflowResponse ret = new()
            {
                Ids = await _workflowrRepository.UpsertAsync(request.Workflows, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes workflows specified in the request.
        /// </summary>
        /// <param name="request">Contains workflow IDs to delete and a flag for soft deletion.</param>
        /// <returns>A response indicating the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXWorkflowPolicies.Delete)]
        public async Task<ActionResult<RemoveWorkflowResponse>> Post(RemoveWorkflow request, CancellationToken cancellationToken = default)
        {
            await _workflowrRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveWorkflowResponse();
        }

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <remarks>
        /// This can be used for testing the workflow.
        /// </remarks>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Run")]
        [Authorize(Policy = GXWorkflowPolicies.Run)]
        public async Task<ActionResult> Run(Guid id, CancellationToken cancellationToken = default)
        {
            await _workflowrRepository.RunAsync(id, cancellationToken);
            return Ok();
        }
    }
}
