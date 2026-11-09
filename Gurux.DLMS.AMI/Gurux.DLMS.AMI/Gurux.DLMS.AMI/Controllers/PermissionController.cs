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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Lists the shared application permission catalog for access management.</summary>
[ApiController, Route("api/Permission"), Authorize]
public sealed class PermissionController(IDbConnectionFactory database, IAuthorizationService authorization) : ControllerBase
{
    /// <summary>Lists notification source permissions available to the selected user.</summary>
    [HttpGet("NotificationSources")]
    public async Task<ActionResult<GXPermission[]>> NotificationSources(CancellationToken cancellationToken, string? userId = null)
    {
        await authorization.EnsureAnyPolicyAsync(User, GXNotificationPolicies.View, GXNotificationPolicies.Add, GXNotificationPolicies.Edit,
            GXUserPolicies.View, GXUserPolicies.Edit);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
        if (!string.IsNullOrWhiteSpace(userId) && userId != currentUserId)
        {
            await authorization.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
        }
        userId = string.IsNullOrWhiteSpace(userId) ? currentUserId : userId;
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        return await GXNotificationPermissions.ReadAsync(connection, userId, cancellationToken);
    }

    /// <summary>Performs the get operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpGet]
    public async Task<ActionResult<GXPermission[]>> Get(CancellationToken cancellationToken)
    {
        await authorization.EnsureAnyPolicyAsync(User, GXUserPolicies.View, GXUserPolicies.Add, GXUserPolicies.Edit,
            GXRolePolicies.View, GXRolePolicies.Add, GXRolePolicies.Edit);
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        var query = GXSelectArgs.SelectAll<GXPermission>();
        query.OrderBy.Add<GXPermission>(p => p.Name!);
        return (await connection.SelectAsync<GXPermission>(query, cancellationToken)).ToArray();
    }
}
