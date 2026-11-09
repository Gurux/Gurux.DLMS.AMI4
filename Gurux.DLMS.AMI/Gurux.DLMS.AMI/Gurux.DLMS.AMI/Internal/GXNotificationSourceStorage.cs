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

using System.ComponentModel.DataAnnotations;
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Loads and replaces normalized notification sources in the caller's transaction.</summary>
internal static class GXNotificationSourceStorage
{
    public static List<string> Validate(IEnumerable<string>? sources)
    {
        var names = (sources ?? []).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || name != name.Trim() ||
                name.Contains(',') || name.Contains(';') || name.Any(char.IsControl))
                {
                throw new ValidationException("Each notification source must be a nonempty name of at most 128 characters without delimiters, surrounding whitespace or control characters.");
            }
        }
        return names;
    }

    public static async Task LoadAsync(GXDbConnection connection, IEnumerable<GXNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        var items = notifications.ToArray();
        if (items.Length == 0) {
            return;
        }
        var ids = items.Select(n => n.Id).Distinct().ToArray();
        var rows = await connection.SelectAsync<GXNotificationSourceMapping>(
            GXSelectArgs.SelectAll<GXNotificationSourceMapping>(s => ids.Contains(s.NotificationId)), cancellationToken);
        var sourceIds = rows.Select(s => s.SourceId).Distinct().ToArray();
        var sources = sourceIds.Length == 0 ? [] : await connection.SelectAsync<GXNotificationSource>(
            GXSelectArgs.SelectAll<GXNotificationSource>(s => sourceIds.Contains(s.Id)), cancellationToken);
        var names = sources.ToDictionary(s => s.Id, s => s.Source);
        var groups = rows.ToLookup(s => s.NotificationId);
        foreach (var item in items)
            // A module can be removed between the two reads; omit its deleted definitions.
            {
            item.Sources = groups[item.Id].Where(s => names.ContainsKey(s.SourceId))
                .Select(s => names[s.SourceId]).Order(StringComparer.Ordinal).ToList();
        }
    }

    public static async Task ReplaceAsync(GXDbConnection connection, IDbTransaction transaction,
        Guid notificationId, IEnumerable<string>? sources, CancellationToken cancellationToken = default)
    {
        var names = Validate(sources);
        var rows = new List<GXNotificationSourceMapping>();
        foreach (var name in names)
        {
            // Selecting an existing module source never changes its owner.
            var source = await connection.SingleOrDefaultAsync<GXNotificationSource>(transaction,
                GXSelectArgs.SelectAll<GXNotificationSource>(s => s.Source == name), cancellationToken);
            if (source == null)
            {
                var prefix = name.Split('.')[0];
                var resource = await connection.SingleOrDefaultAsync<GXNotificationSource>(transaction,
                    GXSelectArgs.SelectAll<GXNotificationSource>(s => s.Source == prefix), cancellationToken);
                if (resource == null)
                    {
                    throw new ValidationException($"Notification source '{name}' has not been registered by a producer.");
                }
                source = await RegisterAsync(connection, transaction, name, resource.ModuleId, cancellationToken);
            }
            rows.Add(new GXNotificationSourceMapping { NotificationId = notificationId, SourceId = source.Id });
        }
        await connection.DeleteAsync(transaction,
            GXDeleteArgs.Delete<GXNotificationSourceMapping>(s => s.NotificationId == notificationId), cancellationToken);
        if (rows.Count != 0)
            {
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(rows.ToArray()), cancellationToken);
        }
    }

    /// <summary>Registers a source without allowing a different producer to take ownership.</summary>
    public static async Task<GXNotificationSource> RegisterAsync(GXDbConnection connection, IDbTransaction transaction,
        string name, string? moduleId, CancellationToken cancellationToken = default)
    {
        Validate([name]);
        if (moduleId != null && (string.IsNullOrWhiteSpace(moduleId) || moduleId.Length > 64 || moduleId != moduleId.Trim()))
            {
            throw new ValidationException("Invalid notification source module identifier.");
        }
        var source = await connection.SingleOrDefaultAsync<GXNotificationSource>(transaction,
            GXSelectArgs.SelectAll<GXNotificationSource>(s => s.Source == name), cancellationToken);
        if (source != null)
        {
            if (!string.Equals(source.ModuleId, moduleId, StringComparison.Ordinal))
                {
                throw new ValidationException($"Notification source '{name}' already belongs to another producer.");
            }
            return source;
        }
        source = new GXNotificationSource { ModuleId = moduleId, Source = name, Enabled = true };
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(source), cancellationToken);
        return source;
    }

    /// <summary>Deletes a module's source definitions and cascades their subscription mappings.</summary>
    public static Task<int> DeleteModuleAsync(GXDbConnection connection, IDbTransaction transaction,
        string moduleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        return connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXNotificationSource>(s => s.ModuleId == moduleId), cancellationToken);
    }
}
