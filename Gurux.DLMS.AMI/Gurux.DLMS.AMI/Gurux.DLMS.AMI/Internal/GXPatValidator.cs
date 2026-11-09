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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using System.Security.Claims;
using Gurux.DLMS.AMI.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Validates the revocable metadata of an already cryptographically validated PAT.</summary>
internal static class GXPatValidator
{
    // Only the PAT bearer scheme uses this validator. Signed exp remains required
    // and structurally valid; the mandatory metadata lookup below enforces the
    // current expiration so owners can extend a PAT without changing its value.
    internal static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters parameters)
        => expires.HasValue && (!notBefore.HasValue ||
            (notBefore.Value <= expires.Value && notBefore.Value <= DateTime.UtcNow.Add(parameters.ClockSkew)));

    internal static bool IsConsumed(GXDbConnection connection, DateTime consumedTime)
        // The MSSQL ORM maps DateTime.MinValue to SQL datetime's minimum.
        => consumedTime != default && !(connection.DatabaseType == Gurux.Service.Orm.Common.Enums.DatabaseType.MSSQL &&
            consumedTime == new DateTime(1753, 1, 1));

    internal static string SelectScheme(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            (context.Request.Path.StartsWithSegments("/guruxami") && !string.IsNullOrEmpty(context.Request.Query["access_token"]))
            ? IdentityConstants.BearerScheme : IdentityConstants.ApplicationScheme;
    }

    internal static Task ReadHubTokenAsync(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(accessToken) && context.Request.Path.StartsWithSegments("/guruxami"))
            {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }

    internal static async Task<bool> IsActiveAsync(GXDbConnection connection, string? id, string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(userId)) {
            return false;
        }
        var metadata = await connection.SingleOrDefaultAsync<GXPersistedGrants>(
            GXSelectArgs.SelectAll<GXPersistedGrants>(g => g.Id == id && g.SubjectId == userId &&
                g.Type == "reference_token" && g.ClientId == "Gurux.DLMS.AMI.ServerAPI"), cancellationToken);
        // Database providers may return UTC timestamps with an unspecified Kind.
        var now = DateTime.UtcNow;
        if (metadata == null || IsConsumed(connection, metadata.ConsumedTime) || metadata.CreationTime > now || metadata.Expiration <= now)
            {
            return false;
        }
        return await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!,
            u => u.Id == userId && u.Removed == null && u.IsApproved == true), cancellationToken) != null;
    }

    internal static async Task ValidateBearerAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var userId = UserId(principal);
        var database = context.HttpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
        await using var connection = await database.GetConnectionAsync(context.HttpContext.RequestAborted);
        if (!await IsActiveAsync(connection, principal?.FindFirst("jti")?.Value, userId, context.HttpContext.RequestAborted))
        {
            context.Fail("Personal access token is no longer active.");
            return;
        }
        GXTokenPermissionValidator.Restrict(principal!, await UserPermissions.ReadEffectiveAsync(connection, userId!),
            await UserPermissions.ReadRolesAsync(connection, userId!));
        // SignalR must follow the current database deadline, rather than closing
        // an extended token's connection at its original signed exp. The PAT hub
        // filter checks metadata on invocation and the tracker checks every 5s.
        context.Properties.ExpiresUtc = null;
    }

    internal static string? UserId(ClaimsPrincipal? principal) =>
        principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal?.FindFirst("sub")?.Value;
}
