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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Lists stored Profile Generic rows and coordinates agent collection of missing history.</summary>
/// <param name="repository">Repository that validates access, tracks collection intervals and stores profile rows.</param>
[ApiController, Route("api/ProfileHistory")]
public sealed class ProfileHistoryController(ProfileHistoryRepository repository) : ControllerBase
{
    /// <summary>Returns stored profile rows and coverage without scheduling a meter read.</summary>
    /// <param name="request">Buffer attribute, half-open time range and requested result page.</param>
    /// <param name="token">Token used to cancel the request.</param>
    /// <returns>Matching stored rows, total row count and covered, missing and unavailable intervals.</returns>
    /// <param name="readAll">Whether all stored history and every meter-buffer row are requested.</param>
    [HttpPost("List"), Authorize(Policy = GXValuePolicies.View)]
    public Task<GXProfileHistoryResponse> List(GXProfileHistoryRequest request, CancellationToken token,
        [FromHeader(Name = "X-Gurux-Profile-Read-All")] bool readAll = false)
        => repository.ListAsync(request, false, token, readAll);

    /// <summary>Returns stored profile history and queues collection of intervals that need reading.</summary>
    /// <param name="request">Buffer attribute, half-open time range, paging and optional forced reread.</param>
    /// <param name="token">Token used to cancel the request.</param>
    /// <returns>Current history and coverage, including identifiers of any queued collection tasks.</returns>
    /// <param name="readAll">Whether all stored history and every meter-buffer row are requested.</param>
    [HttpPost("Read"), Authorize(Policy = GXValuePolicies.View), Authorize(Policy = GXTaskPolicies.Add)]
    public Task<GXProfileHistoryResponse> Read(GXProfileHistoryRequest request, CancellationToken token,
        [FromHeader(Name = "X-Gurux-Profile-Read-All")] bool readAll = false)
        => repository.ListAsync(request, true, token, readAll);

    /// <summary>Clears all stored rows, the latest buffer value and collection coverage for one accessible profile.</summary>
    /// <param name="request">The persisted Profile Generic buffer attribute to clear.</param>
    /// <param name="token">Cancels the clear operation before its transaction commits.</param>
    /// <returns>An HTTP success response after history and coverage are cleared together.</returns>
    [HttpPost("Clear"), Authorize(Policy = GXDevicePolicies.Edit)]
    public async Task<IActionResult> Clear(GXProfileHistoryRequest request, CancellationToken token)
    {
        await repository.ClearAsync(request.AttributeId, token);
        return Ok();
    }

    /// <summary>Creates or resumes bounded read intervals for an agent's claimed profile task.</summary>
    /// <param name="request">Identifiers of the claimed task and its Profile Generic buffer attribute.</param>
    /// <param name="token">Token used to cancel interval preparation.</param>
    /// <param name="bufferAvailableFrom">Optional oldest captured time verified against stable meter-buffer boundaries.</param>
    /// <param name="entriesInUse">Optional stable Entries In Use count from the meter.</param>
    /// <param name="bufferNewest">The newest captured timestamp in the collected complete meter buffer.</param>
    /// <returns>The time intervals the agent must read under the current task claim.</returns>
    [HttpPost("Begin"), Authorize(Policy = GXTaskPolicies.Edit)]
    public Task<GXProfileBeginResponse> Begin(GXProfileBeginRequest request, CancellationToken token,
        [FromHeader(Name = "X-Gurux-Profile-Oldest")] DateTimeOffset? bufferAvailableFrom = null,
        [FromHeader(Name = "X-Gurux-Profile-Entries")] uint? entriesInUse = null,
        [FromHeader(Name = "X-Gurux-Profile-Newest")] DateTimeOffset? bufferNewest = null)
        => repository.BeginAsync(request, token, bufferAvailableFrom, entriesInUse, bufferNewest);

    /// <summary>Stores an agent's completed profile interval and its returned rows.</summary>
    /// <param name="request">Claimed interval, task identifier, completion state and captured rows.</param>
    /// <param name="token">Token used to cancel persistence.</param>
    /// <returns>An HTTP success response after the repository accepts the result.</returns>
    /// <remarks>Repeated completed submissions are accepted without storing duplicate rows.</remarks>
    [HttpPost("Commit"), Authorize(Policy = GXValuePolicies.Add)]
    public async Task<IActionResult> Commit(GXProfileCommit request, CancellationToken token)
    {
        await repository.CommitAsync(request, token);
        return Ok();
    }
}
