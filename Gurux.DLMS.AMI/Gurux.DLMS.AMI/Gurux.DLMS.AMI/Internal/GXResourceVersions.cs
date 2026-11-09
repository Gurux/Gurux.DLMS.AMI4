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
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Public resource versions without exposing administrative configuration data.</summary>
internal sealed class GXResourceVersions(IDbConnectionFactory factory)
{
    public async Task TouchAppearanceAsync(byte type, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; ++attempt)
        {
            try
            {
                await TouchAppearanceCoreAsync(type, cancellationToken);
                return;
            }
            catch (GXDatabaseException ex) when (ex.ErrorType == DatabaseErrorType.Deadlock && attempt < 2)
            {
                // Retry the version transaction, not the resource mutation that already succeeded.
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private async Task TouchAppearanceCoreAsync(byte type, CancellationToken cancellationToken)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        if (connection.Connection.State != ConnectionState.Open)
        {
            await connection.Connection.OpenAsync(cancellationToken);
        }
        // The JSON contains independent category versions. Protect its read-modify-write.
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Appearance));
        var args = GXSelectArgs.Select<GXConfiguration>(c => new { c.Id, c.Settings }, c => c.Name == name);
        var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(transaction, args, cancellationToken);
        if (configuration != null)
        {
            var settings = string.IsNullOrEmpty(configuration.Settings) ? new AppearanceSettings()
                : JsonSerializer.Deserialize<AppearanceSettings>(configuration.Settings) ?? new AppearanceSettings();
            settings.ImageUpdated ??= settings.Updated ?? DateTimeOffset.MinValue;
            settings.Updated = DateTimeOffset.UtcNow;
            if (type == (byte)ResourceType.Image)
            {
                settings.ImageUpdated = settings.Updated;
            }
            else if (type == (byte)ResourceType.Theme)
            {
                settings.ThemeUpdated = settings.Updated;
            }
            else if (type == (byte)ResourceType.Iconpack)
            {
                settings.IconpackUpdated = settings.Updated;
            }

            configuration.Settings = JsonSerializer.Serialize(settings);
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(configuration, c => c.Settings!), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> AppearanceAsync(byte type, CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        var name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Appearance));
        var args = GXSelectArgs.Select<GXConfiguration>(c => c.Settings!, c => c.Name == name);
        var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(args, cancellationToken);
        var settings = string.IsNullOrEmpty(configuration?.Settings) ? null
            : JsonSerializer.Deserialize<AppearanceSettings>(configuration.Settings);
        return type switch
        {
            (byte)ResourceType.Theme => settings?.ThemeUpdated,
            (byte)ResourceType.Iconpack => settings?.IconpackUpdated,
            _ => settings?.ImageUpdated ?? settings?.Updated
        };
    }

    public async Task TouchTextsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        var name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Language));
        var args = GXSelectArgs.Select<GXConfiguration>(c => new { c.Id, c.Updated }, c => c.Name == name);
        var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(args, cancellationToken);
        if (configuration == null)
        {
            return;
        }

        configuration.Updated = DateTimeOffset.UtcNow;
        // Preserve the language settings array.
        await connection.UpdateAsync(GXUpdateArgs.Update(configuration, c => c.Updated!), cancellationToken);
    }

    public async Task<DateTimeOffset?> TextsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        var name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Language));
        var args = GXSelectArgs.Select<GXConfiguration>(c => c.Updated!, c => c.Name == name);
        return (await connection.SingleOrDefaultAsync<GXConfiguration>(args, cancellationToken))?.Updated;
    }
}
