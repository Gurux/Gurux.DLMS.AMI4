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

/// <summary>Reads user change history, restores saved entity versions and removes expired history.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ChangeHistoryController(IChangeHistoryRepository history) : ControllerBase
{
    /// <summary>List accessible history items without JSON snapshot payloads.</summary>
    [HttpPost("List")]
    [Authorize(Policy = GXChangeHistoryPolicies.View)]
    public async Task<ActionResult<ListChangeHistoryItemsResponse>> List(ListChangeHistoryItems request, CancellationToken cancellationToken)
    {
        var response = new ListChangeHistoryItemsResponse();
        await history.ListAsync(request, response, cancellationToken);
        return response;
    }

    /// <summary>Read an accessible history item and the target's current revision.</summary>
    [HttpGet]
    [Authorize(Policy = GXChangeHistoryPolicies.View)]
    public async Task<ActionResult<GetChangeHistoryResponse>> Get(Guid id, CancellationToken cancellationToken)
        => new GetChangeHistoryResponse
        {
            Item = await history.ReadAsync(id, cancellationToken: cancellationToken),
            CurrentRevision = await history.GetCurrentRevisionAsync(id, cancellationToken)
        };

    /// <summary>Restore a saved entity state as a new latest revision.</summary>
    [HttpPost("Restore")]
    [Authorize(Policy = GXChangeHistoryPolicies.Restore)]
    public async Task<ActionResult<RestoreChangeHistoryItemResponse>> Restore(RestoreChangeHistoryItem request, CancellationToken cancellationToken)
    {
        if (request.HistoryItemId == Guid.Empty || request.ExpectedRevision < 0)
        {
            return BadRequest("A history item identifier and expected current revision are required.");
        }

        try
        {
            return new RestoreChangeHistoryItemResponse { Id = await history.RestoreAsync(request.HistoryItemId, request.ExpectedRevision, cancellationToken) };
        }
        catch (InvalidOperationException error)
        {
            return Conflict(new ProblemDetails { Title = "The saved version could not be restored.", Detail = error.Message });
        }
    }
    /// <summary>Permanently delete history recorded before a cutoff, preserving entity revision clocks.</summary>
    [HttpPost("DeleteOlderThan")]
    [Authorize(Roles = GXRoles.Admin, Policy = GXChangeHistoryPolicies.Delete)]
    public async Task<ActionResult<DeleteChangeHistoryOlderThanResponse>> DeleteOlderThan(
        DeleteChangeHistoryOlderThan request, CancellationToken cancellationToken)
    {
        if (request.Cutoff is not DateTimeOffset cutoff)
        {
            return BadRequest("A history cutoff date is required.");
        }

        return new DeleteChangeHistoryOlderThanResponse
        {
            Count = await history.DeleteOlderThanAsync(cutoff, cancellationToken)
        };
    }
}
