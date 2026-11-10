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

using System.Security.Cryptography;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Coordinates reference-only meter password changes and agent verification.</summary>
[ApiController]
[Route("api/PasswordChange")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PasswordChangeController(GXPasswordChangeService service) : ControllerBase
{
    /// <summary>Lists metadata for visible key management.</summary>
    [HttpGet("{managementId:guid}")]
    [Authorize(Policy = GXKeyManagementPolicies.View)]
    public Task<IReadOnlyList<GXPasswordChangeInfo>> List(Guid managementId, CancellationToken token)
        => service.ListAsync(User, managementId, token);

    /// <summary>Queues a fresh credential or recovery from retained material.</summary>
    [HttpPost]
    [Authorize(Policy = GXKeyPermissions.Lifecycle)]
    public async Task<GXPasswordChangeInfo> Create(GXPasswordChangeRequest request, CancellationToken token)
    {
        try
        {
            return await service.CreateAsync(User, request, token);
        }
        finally
        {
            if (request.NewPassword != null)
            {
                CryptographicOperations.ZeroMemory(request.NewPassword);
                request.NewPassword = null;
            }
        }
    }

    /// <summary>Lists selectable meter associations.</summary>
    [HttpGet("Associations/{deviceId:guid}")]
    [Authorize(Policy = GXKeyPermissions.Lifecycle)]
    public Task<IReadOnlyList<GXPasswordAssociation>> Associations(Guid deviceId, CancellationToken token)
        => service.AssociationsAsync(User, deviceId, token);

    /// <summary>Cancels before a physical write may have started.</summary>
    [HttpPost("{id:guid}/Cancel")]
    [Authorize(Policy = GXKeyPermissions.Lifecycle)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken token)
    {
        await service.CancelAsync(User, id, token);
        return Ok();
    }

    /// <summary>Retries a failed task while preserving write uncertainty.</summary>
    [HttpPost("{id:guid}/Retry")]
    [Authorize(Policy = GXKeyPermissions.Lifecycle)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken token)
    {
        await service.RetryAsync(User, id, token);
        return Ok();
    }

    /// <summary>Streams ephemeral credentials to the claiming agent and then clears them.</summary>
    [HttpPost("{id:guid}/Work")]
    [Authorize(Policy = GXTaskPolicies.Edit)]
    public async Task Work(Guid id, GXPasswordChangeClaim request, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        using var work = await service.WorkAsync(User, id, request, token);
        await Response.WriteAsJsonAsync(work, cancellationToken: token);
    }

    /// <summary>Records the claiming agent's write intent.</summary>
    [HttpPost("{id:guid}/Writing")]
    [Authorize(Policy = GXTaskPolicies.Edit)]
    public async Task<IActionResult> Writing(Guid id, GXPasswordChangeClaim request, CancellationToken token)
    {
        await service.MarkWritingAsync(User, id, request, token);
        return Ok();
    }

    /// <summary>Acknowledges a freshly authenticated meter association.</summary>
    [HttpPost("{id:guid}/Verified")]
    [Authorize(Policy = GXTaskPolicies.Edit)]
    public async Task<IActionResult> Verified(Guid id, GXPasswordChangeClaim request, CancellationToken token)
    {
        await service.ConfirmAsync(User, id, request, token);
        return Ok();
    }
}