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
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Authorized script template comparison, import and explicit update endpoints.</summary>
[ApiController, Route("api/ScriptCatalog"), Authorize(Policy = GXScriptPolicies.View)]
public sealed class ScriptCatalogController(GXScriptCatalogService scripts) : ControllerBase
{
    /// <summary>Returns verified code for comparison; does not compile or execute it.</summary>
    [HttpPost("Preview")]
    public async Task<ActionResult<ScriptTemplatePreview>> Preview(PreviewScriptTemplate request, CancellationToken token)
    {
        try
        {
            return await scripts.PreviewAsync(request, token);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException error)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid script template request", detail: error.Message);
        }
        catch (InvalidDataException error)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Script template verification failed", detail: error.Message);
        }
        catch (InvalidOperationException error)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Script template conflict", detail: error.Message);
        }
    }

    /// <summary>Imports or replaces one previewed version after permission and conflict checks.</summary>
    [HttpPost("Apply")]
    public async Task<ActionResult<Guid>> Apply(ApplyScriptTemplate request, CancellationToken token)
    {
        try
        {
            return await scripts.ApplyAsync(request, token);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException error)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid script template request", detail: error.Message);
        }
        catch (InvalidDataException error)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Script template verification failed", detail: error.Message);
        }
        catch (InvalidOperationException error)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Script template conflict", detail: error.Message);
        }
    }

    /// <summary>Reads sources retained before updates using normal script/group authorization.</summary>
    [HttpGet("History")]
    public Task<List<Gurux.DLMS.AMI.Shared.DTOs.Script.GXScriptTemplateHistory>> History(Guid scriptId, CancellationToken token)
        => scripts.HistoryAsync(scriptId, token);
}
