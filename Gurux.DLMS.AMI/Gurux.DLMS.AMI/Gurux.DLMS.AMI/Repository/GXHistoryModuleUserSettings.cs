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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Authorizes historical module-owned user data and writes it through its owning module API.</summary>
internal sealed class GXHistoryModuleUserSettings(IServiceProvider services, IDbConnectionFactory factory,
    IHttpContextAccessor accessor) : IGXHistoryModuleUserSettings
{
    public async Task<GXUserProperty?> ReadAsync(GXUserProperty row, CancellationToken token)
    {
        var actor = accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        var actorId = ServerHelpers.GetUserId(actor);
        if (actor.Identity?.IsAuthenticated != true || row.User?.Id == null || row.Module?.Id == null ||
            row.User.Id != actorId && !actor.IsInRole(GXRoles.Admin))
        {
            throw new UnauthorizedAccessException("Module user history belongs to another user.");
        }

        await using var db = await factory.GetHistoryConnectionAsync(token);
        var moduleId = row.Module.Id;
        if (await db.SingleOrDefaultAsync<GXModule>(GXSelectArgs.Select<GXModule>(m => m.Id, m => m.Id == moduleId), token) == null)
        {
            throw new GXAmiNotFoundException("The owning module is unavailable.");
        }

        if (!await GXModulePolicyAccess.AllowsAsync(services, actor, moduleId, token))
        {
            throw new UnauthorizedAccessException("The owning module is not accessible.");
        }

        return row;
    }

    public async Task WriteAsync(GXUserProperty row, CancellationToken token)
    {
        _ = await ReadAsync(row, token);
        if (row.Name == null || row.Value == null)
        {
            throw new ArgumentException("A saved module setting name and value are required.");
        }

        JsonElement value;
        try { value = JsonSerializer.Deserialize<JsonElement>(row.Value); }
        catch (JsonException error) { throw new ArgumentException("The saved module setting is not valid JSON.", error); }
        await new GXModuleUserSettings(row.Module!.Id, services).UpsertAsync(row.User!.Id!, row.Name, value, cancellationToken: token);
    }
}
