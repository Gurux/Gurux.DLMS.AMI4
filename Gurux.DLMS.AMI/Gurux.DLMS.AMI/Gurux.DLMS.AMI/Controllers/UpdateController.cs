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

using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Scheduler;
using Gurux.DLMS.AMI.Update;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides update API endpoints.</summary>
/// <remarks>Initializes a new instance of <see cref="UpdateController"/>.</remarks>
/// <param name="store">The store.</param>
/// <param name="runner">The runner.</param>
[ApiController]
[Route("api/Update")]
[Authorize(Policy = GXUpdatePolicies.Admin)]
public sealed class UpdateController(GXUpdateStatusStore store, GXBackgroundTaskRunner runner) : ControllerBase
{
    /// <summary>Reads update notification preferences.</summary>
    [HttpGet("Settings")]
    public async Task<GXUpdateSettingsResponse> Settings([FromServices] GXUpdateSettingsStore settings,
        [FromServices] GXUpdateEmailNotifier notifier, CancellationToken cancellationToken)
        => new() { Settings = await settings.ReadAsync(cancellationToken), EmailSenderAvailable = notifier.EmailSenderAvailable };

    /// <summary>Saves update notification preferences.</summary>
    [HttpPost("Settings")]
    public async Task<IActionResult> SaveSettings([FromBody] UpdateSettings value,
        [FromServices] GXUpdateSettingsStore settings, CancellationToken cancellationToken)
    {
        try
        {
            await settings.SaveAsync(value, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { exception.Message });
        }
        return NoContent();
    }

    readonly GXUpdateStatusStore store = store;
    readonly GXBackgroundTaskRunner runner = runner;

    /// <summary>Performs the list operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpGet("List")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<GXAvailableUpdatesResponse> List(CancellationToken cancellationToken = default) => store.ReadAsync(cancellationToken);

    /// <summary>Performs the check operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpPost("Check")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<GXAvailableUpdatesResponse> Check(CancellationToken cancellationToken = default)
    {
        var runId = await runner.RunAsync(GXBackgroundTaskIds.Update, User, cancellationToken);
        var response = await store.ReadAsync(cancellationToken);
        response.RunId = runId;
        return response;
    }
}
