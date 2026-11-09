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

using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Registration rules read from the current account configuration.</summary>
public sealed record GXAccountRegistrationState(UserCreationType Mode, bool FirstUser)
{
    /// <summary>Allows public registration unless account creation is restricted to administrators.</summary>
    public bool PublicRegistrationAllowed => FirstUser || Mode != UserCreationType.AdminOnly;
    /// <summary>Checks whether the actor may create an account, including initial administrator setup.</summary>
    public bool CanCreate(ClaimsPrincipal? actor) => PublicRegistrationAllowed || actor?.IsInRole(GXRoles.Admin) == true;
    /// <summary>Determines whether a new account can be approved immediately.</summary>
    public bool ApproveNewAccount(ClaimsPrincipal? actor) => FirstUser || Mode != UserCreationType.AdminApprovalRequired || actor?.IsInRole(GXRoles.Admin) == true;
}

/// <summary>Shares registration policy between Identity, API creation and account pages.</summary>
public sealed class GXAccountRegistrationPolicy(IDbConnectionFactory database)
{
    /// <summary>Reads the current account configuration and checks whether initial setup is needed.</summary>
    public async Task<GXAccountRegistrationState> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        return await ReadAsync(connection, cancellationToken);
    }

    internal static async Task<GXAccountRegistrationState> ReadAsync(GXDbConnection connection, CancellationToken cancellationToken = default)
    {
        var firstUser = await connection.SingleOrDefaultAsync<long>(GXSelectArgs.Select<GXUser>(u => GXSql.Count(1)), cancellationToken) == 0;
        var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(
            GXSelectArgs.Select<GXConfiguration>(c => c.Settings!, c => c.Name == GXConfigurations.Account), cancellationToken);
        var mode = UserCreationType.Anyone;
        if (!string.IsNullOrWhiteSpace(configuration?.Settings))
        {
            try
            {
                mode = JsonSerializer.Deserialize<AccountSettings>(configuration.Settings, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.UserCreationType
                    ?? UserCreationType.AdminOnly;
            }
            catch (JsonException)
            {
                // Invalid persisted settings must not open registration.
                mode = UserCreationType.AdminOnly;
            }
        }
        if (!Enum.IsDefined(mode))
        {
            mode = UserCreationType.AdminOnly;
        }
        return new(mode, firstUser);
    }
}
