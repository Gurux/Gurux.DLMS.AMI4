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

using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Hubs;

/// <summary>Loads server notification settings before notification delivery starts.</summary>
public sealed class GXServerNotificationConfiguration(IDbConnectionFactory factory, NotificationCache cache) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        await ReloadAsync(connection, cache, cancellationToken);
    }

    internal static bool IsConfiguration(string? name) =>
        string.Equals(name, GXConfigurations.Notification, StringComparison.OrdinalIgnoreCase);

    internal static async Task ReloadAsync(GXDbConnection connection, NotificationCache cache, CancellationToken cancellationToken)
    {
        // Select identities independently of the database collation; read only the selected settings.
        var identities = await connection.SelectAsync<GXConfiguration>(
            GXSelectArgs.Select<GXConfiguration>(c => new { c.Id, c.Name }, c => c.Enabled), cancellationToken);
        var selected = identities.FirstOrDefault(c => string.Equals(c.Name, GXConfigurations.Notification, StringComparison.OrdinalIgnoreCase));
        var configuration = selected == null ? null : await connection.SingleOrDefaultAsync<GXConfiguration>(
            GXSelectArgs.SelectAll<GXConfiguration>(c => c.Id == selected.Id), cancellationToken);
        Apply(cache, configuration);
        cache.ConfigureSources(await connection.SelectAsync<Gurux.DLMS.AMI.Shared.DTOs.Notification.GXNotificationSource>(
            GXSelectArgs.SelectAll<Gurux.DLMS.AMI.Shared.DTOs.Notification.GXNotificationSource>(), cancellationToken));
    }

    /// <summary>Applies persisted settings without changing recipient authorization.</summary>
    public static void Apply(NotificationCache cache, GXConfiguration? configuration)
    {
        cache.Configure(configuration?.Enabled != false && !string.IsNullOrWhiteSpace(configuration?.Settings)
            ? JsonSerializer.Deserialize<PerformanceSettings>(configuration.Settings) ?? new() : new());
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
