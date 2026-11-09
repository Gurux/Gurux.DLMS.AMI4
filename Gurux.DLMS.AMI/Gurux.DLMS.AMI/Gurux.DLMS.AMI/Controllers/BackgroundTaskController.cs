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
using System.Security.Claims;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Read the status of executions started through authorized application endpoints.</summary>
[ApiController]
[Route("api/BackgroundTask")]
[Authorize]
public sealed class BackgroundTaskController(GXBackgroundTaskRunner runner) : ControllerBase
{
    /// <summary>Performs the get task status operation.</summary>
    /// <param name="runId">The run id.</param>
    [HttpGet("Status/{runId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<GXBackgroundTaskStatus> GetTaskStatus(Guid runId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }
        var status = runner.GetTaskStatus(runId);
        if (status == null || (status.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole(GXRoles.Admin)))
        {
            return NotFound();
        }
        return status;
    }
}
