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

using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Adapts module-owned user activity to the host's existing log schema and enumeration types.</summary>
internal sealed class GXUserActivityStore(IDbConnectionFactory factory) : IGXAmiUserActivityStore
{
    public async Task WriteAsync(HttpContext context, AmiUserActivity entry, CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }
        var id = ServerHelpers.GetUserId(context.User);
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        var user = await connection.SingleOrDefaultAsync<GXUser>(
            GXSelectArgs.SelectAll<GXUser>(value => value.Id == id), cancellationToken);
        var action = (CrudAction)entry.Action;
        var enumTypes = context.RequestServices.GetRequiredService<IEnumTypeRepository>();
        var log = new GXLog
        {
            TargetType = entry.Target,
            User = user,
            CreationTime = entry.CreationTime,
            Action = action,
            Status = entry.Status,
            Duration = entry.Duration,
            Data = entry.Data,
            Reply = entry.Reply,
            Type = await enumTypes.GetLogTypeAsync(TargetType.UserAction, action.ToString(), cancellationToken)
        };
        await GXLogStore.InsertAsync(connection, "user-action", [log], cancellationToken: cancellationToken);
    }
}
