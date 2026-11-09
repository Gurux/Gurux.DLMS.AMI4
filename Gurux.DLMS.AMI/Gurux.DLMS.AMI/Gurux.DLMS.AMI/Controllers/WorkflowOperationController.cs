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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides authorized access to workflow operation state and terminal-operation deletion.</summary>
/// <remarks>Starting and finishing operations are internal repository operations, not HTTP endpoints.</remarks>
/// <param name="workflowOperationRepository">Repository enforcing operation access and lifecycle rules.</param>
[Route("api/[controller]")]
[ApiController]
public sealed class WorkflowOperationController(IWorkflowOperationRepository workflowOperationRepository) : ControllerBase
{
    /// <summary>Lists visible workflow operations using filtering, ordering, and paging.</summary>
    /// <param name="request">Query parameters. AllUsers is effective only for administrators.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The visible page and total matching count before paging.</returns>
    [HttpPost("List")]
    [Authorize(Policy = GXWorkflowOperationPolicies.View)]
    [ProducesResponseType(typeof(ListWorkflowOperationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListWorkflowOperationsResponse>> Post(
        ListWorkflowOperations request, CancellationToken cancellationToken = default)
    {
        if (request == null || request.Index < 0 || request.Count < 0)
            {
            return BadRequest("Paging index and count must be non-negative.");
        }
        try
        {
            ListWorkflowOperationsResponse response = new();
            await workflowOperationRepository.ListAsync(request, response, cancellationToken);
            return response;
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Reads a workflow operation visible to the current caller.</summary>
    /// <param name="id">The operation's nonempty GUID identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The operation, or HTTP 404 if it is missing or not visible.</returns>
    [HttpGet]
    [Authorize(Policy = GXWorkflowOperationPolicies.View)]
    [ProducesResponseType(typeof(GetWorkflowOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetWorkflowOperationResponse>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) {
            return BadRequest("A nonempty workflow operation identifier is required.");
        }
        try
        {
            return new GetWorkflowOperationResponse
            {
                Item = await workflowOperationRepository.ReadAsync(id, null, cancellationToken)
            };
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Permanently deletes selected terminal workflow operations.</summary>
    /// <param name="request">Nonempty operation identifiers to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A deletion acknowledgement, HTTP 404 for missing operations, or HTTP 409 for incompatible state.</returns>
    /// <remarks>Running operations and parents with retained child operations cannot be deleted.</remarks>
    [HttpPost("Delete")]
    [Authorize(Policy = GXWorkflowOperationPolicies.Delete)]
    [ProducesResponseType(typeof(RemoveWorkflowOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RemoveWorkflowOperationResponse>> Post(
        RemoveWorkflowOperation request, CancellationToken cancellationToken = default)
    {
        var ids = request?.Ids?.ToArray();
        if (ids == null || ids.Length == 0 || ids.Contains(Guid.Empty))
            {
            return BadRequest("At least one nonempty workflow operation identifier is required.");
        }
        try
        {
            await workflowOperationRepository.DeleteAsync(ids, cancellationToken);
            return new RemoveWorkflowOperationResponse();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
