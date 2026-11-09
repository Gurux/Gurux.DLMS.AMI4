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

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Manages server-wide notification source delivery settings.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class NotificationSourceController(INotificationSourceRepository repository) : ControllerBase
{
    /// <summary>Lists registered sources with filtering, ordering and paging.</summary>
    [HttpPost("List")]
    [Authorize(Policy = GXConfigurationPolicies.View)]
    public async Task<ActionResult<ListNotificationSourcesResponse>> List(ListNotificationSources request,
        CancellationToken cancellationToken = default)
    {
        var response = new ListNotificationSourcesResponse();
        await repository.ListAsync(request, response, cancellationToken);
        return response;
    }

    /// <summary>Enables or disables delivery for selected registered sources.</summary>
    [HttpPost("SetEnabled")]
    [Authorize(Policy = GXConfigurationPolicies.Edit)]
    public async Task<ActionResult<SetNotificationSourcesEnabledResponse>> SetEnabled(SetNotificationSourcesEnabled request,
        CancellationToken cancellationToken = default)
    {
        if (request.Ids == null || !request.Ids.Any()) {
            return BadRequest("Notification source identifiers are required.");
        }
        await repository.SetEnabledAsync(request.Ids, request.Enabled, cancellationToken);
        return new SetNotificationSourcesEnabledResponse();
    }
}
