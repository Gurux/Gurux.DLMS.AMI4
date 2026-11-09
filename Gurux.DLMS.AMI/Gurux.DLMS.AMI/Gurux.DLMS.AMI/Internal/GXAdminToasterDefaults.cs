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

using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Persists the administrator's initial system-error toaster subscription.</summary>
internal static class GXAdminToasterDefaults
{
    public static async Task<List<(string Owner, Guid Id, bool Added)>> EnsureAsync(GXDbConnection connection, IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var added = new List<(string Owner, Guid Id, bool Added)>();
        var type = await connection.SingleOrDefaultAsync<GXNotificationType>(transaction,
            GXSelectArgs.SelectAll<GXNotificationType>(t => t.Code == "TOASTER" && t.Enabled == true && t.Removed == null), cancellationToken);
        if (type == null) {
            return added;
        }

        var admins = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
        admins.Joins.AddInnerJoin<GXUser, GXUserRole>(u => u.Id!, r => r.UserId!);
        admins.Joins.AddInnerJoin<GXUserRole, GXRole>(r => r.RoleId!, r => r.Id);
        admins.Where.And<GXRole>(r => r.NormalizedName == "ADMIN" && r.Removed == null);
        foreach (var admin in await connection.SelectAsync<GXUser>(transaction, admins, cancellationToken))
        {
            // Stable identity also preserves edited, disabled and soft-deleted defaults.
            var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("admin-toaster-system-log-error:" + admin.Id)).AsSpan(0, 16));
            var existing = await connection.SingleOrDefaultAsync<GXNotification>(transaction,
                GXSelectArgs.SelectAll<GXNotification>(n => n.Id == id), cancellationToken);
            if (existing != null)
            {
                // Repair only the previous generated default, preserving user customizations.
                var notificationType = new GXNotificationType { Id = type.Id };
                if (existing.Removed == null && existing.Name == "System log errors" &&
                    await connection.SingleOrDefaultAsync<GXNotification>(transaction,
                        GXSelectArgs.Select<GXNotification>(n => n.Id, n => n.Id == id && n.Type == notificationType), cancellationToken) != null)
                {
                    var sources = GXSelectArgs.SelectAll<GXNotificationSource>();
                    sources.Joins.AddInnerJoin<GXNotificationSource, GXNotificationSourceMapping>(s => s.Id, m => m.SourceId);
                    sources.Where.And<GXNotificationSourceMapping>(m => m.NotificationId == id);
                    var selected = await connection.SelectAsync<GXNotificationSource>(transaction, sources, cancellationToken);
                    if (selected.Count == 1 && selected[0].Source == "system-log.error")
                    {
                        existing.Name = type.Name;
                        existing.Updated = DateTimeOffset.UtcNow;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(existing, n => new { n.Name, n.Updated }), cancellationToken);
                        await GXNotificationSourceStorage.ReplaceAsync(connection, transaction, id, [GXSystemLogPolicies.Add], cancellationToken);
                        added.Add((admin.Id!, id, false));
                    }
                }
                continue;
            }
            var notification = new GXNotification(type.Name)
            {
                Id = id,
                Creator = new GXUser { Id = admin.Id },
                Type = new GXNotificationType { Id = type.Id },
                TraceLevel = TraceLevel.Error,
                CreationTime = DateTimeOffset.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            await connection.InsertAsync(transaction, GXInsertArgs.Insert(notification), cancellationToken);
            await GXNotificationSourceStorage.ReplaceAsync(connection, transaction, id,
                [GXSystemLogPolicies.Add], cancellationToken);
            added.Add((admin.Id!, id, true));
        }
        return added;
    }
}
