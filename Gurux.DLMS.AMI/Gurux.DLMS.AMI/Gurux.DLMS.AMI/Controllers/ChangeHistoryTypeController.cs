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

/// <summary>Reads history types and manages their recording settings.</summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ChangeHistoryTypeController(IChangeHistoryTypeRepository repository) : ControllerBase
{
    /// <summary>Reads a history type definition.</summary>
    [HttpGet]
    [Authorize(Policy = GXChangeHistoryPolicies.View)]
    public async Task<ActionResult<GetChangeHistoryTypeResponse>> Get(Guid id, CancellationToken cancellationToken)
        => new GetChangeHistoryTypeResponse { Item = await repository.ReadAsync(id, cancellationToken: cancellationToken) };

    /// <summary>Lists history type definitions without loading history items.</summary>
    [HttpPost("List")]
    [Authorize(Policy = GXChangeHistoryPolicies.View)]
    public async Task<ActionResult<ListChangeHistoryTypesResponse>> List(ListChangeHistoryTypes request, CancellationToken cancellationToken)
    {
        var response = new ListChangeHistoryTypesResponse();
        await repository.ListAsync(request, response, cancellationToken);
        return response;
    }

    /// <summary>Inserts or updates history type settings using stable GUIDs.</summary>
    [HttpPost("Update")]
    [Authorize(Roles = GXRoles.Admin)]
    public async Task<ActionResult<UpdateChangeHistoryTypeResponse>> Update(UpdateChangeHistoryType request, CancellationToken cancellationToken)
        => new UpdateChangeHistoryTypeResponse { Ids = await repository.UpsertAsync(request.ChangeHistoryTypes, cancellationToken: cancellationToken) };

    /// <summary>Deletes history types and their associated history and revision records.</summary>
    [HttpPost("Remove")]
    [Authorize(Roles = GXRoles.Admin)]
    public async Task<ActionResult<RemoveChangeHistoryTypeResponse>> Remove(RemoveChangeHistoryType request, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(request.Ids, cancellationToken);
        return new RemoveChangeHistoryTypeResponse();
    }

    /// <summary>Adds missing default types, or resets selected existing default types while preserving history and revisions.</summary>
    [HttpPost("Regenerate")]
    [Authorize(Roles = GXRoles.Admin)]
    public async Task<ActionResult<RegenerateChangeHistoryTypeResponse>> Regenerate(
        RegenerateChangeHistoryType request, CancellationToken cancellationToken)
    {
        var ids = request.Ids?.ToArray();
        if (ids?.Length > 0)
        {
            await repository.ResetToDefaultsAsync(ids, cancellationToken);
        }
        else
        {
            await repository.EnsureDefaultsAsync(cancellationToken);
        }

        return new RegenerateChangeHistoryTypeResponse();
    }
}
