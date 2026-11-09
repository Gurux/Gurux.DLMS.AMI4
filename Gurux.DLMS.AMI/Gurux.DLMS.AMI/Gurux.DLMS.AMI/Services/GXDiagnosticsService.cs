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
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Loads host audit settings and publishes metadata-only timings without collecting snapshots.</summary>
internal sealed class GXDiagnosticsService(IDbConnectionFactory factory,
    IConfigurationRepository configuration, ILogger<GXDiagnosticsService> logger, IGXAmiEvents? events = null) : IHostedService, IGXAmiDiagnosticsSource, IDisposable
{
    private StatisticSettings _settings = new();
    public StatisticSettings Settings => Volatile.Read(ref _settings);
    public int PerformanceSnapshotIntervalSeconds => Settings.PerformanceSnapshotIntervalSeconds;
    public event Action<int>? IntervalChanged;
    public event Action<string, byte, TimeSpan>? RequestCompleted;

    public async Task<ClaimsPrincipal?> GetPersistenceUserAsync(CancellationToken cancellationToken)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        return await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        configuration.Updated += OnConfigurationUpdated;
        try
        {
            var admin = await GetPersistenceUserAsync(cancellationToken);
            if (admin != null)
            {
                OnConfigurationUpdated(await configuration.ListAsync(admin,
                    new ListConfiguration { Filter = new GXConfiguration { Name = GXConfigurations.Statistic } },
                    cancellationToken: cancellationToken));
            }
        }
        catch
        {
            configuration.Updated -= OnConfigurationUpdated;
            throw;
        }
    }

    private void OnConfigurationUpdated(IEnumerable<GXConfiguration> configurations)
    {
        foreach (var item in configurations)
        {
            if (item.Name != GXConfigurations.Statistic || item.Settings == null)
            {
                continue;
            }
            try
            {
                var settings = JsonSerializer.Deserialize<StatisticSettings>(item.Settings) ?? new();
                var previous = Interlocked.Exchange(ref _settings, settings);
                if (previous.PerformanceSnapshotIntervalSeconds != settings.PerformanceSnapshotIntervalSeconds)
                {
                    IntervalChanged?.Invoke(settings.PerformanceSnapshotIntervalSeconds);
                }
            }
            catch (JsonException error)
            {
                logger.LogError(error, "Invalid statistics configuration.");
            }
        }
    }

    internal async Task RecordAsync(string? userId, string target, CrudAction action,
        int statusCode, TimeSpan elapsed, CancellationToken cancellationToken)
    {
        // Preserve existing consumers while newer modules use the common metadata event.
        Record(target, action, elapsed);
        await (events ?? AmiEventHub.None).PublishAsync(
            new AmiRequestCompleted(userId, target, (byte)action, statusCode, elapsed), cancellationToken);
    }

    internal void Record(string target, CrudAction action, TimeSpan elapsed)
    {
        if (PerformanceSnapshotIntervalSeconds <= 0)
        {
            return;
        }
        // Module errors must not affect successful requests or other diagnostics listeners.
        var callbacks = RequestCompleted;
        if (callbacks != null)
        {
            foreach (Action<string, byte, TimeSpan> callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback(target, (byte)action, elapsed);
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Failed to record request timing.");
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        configuration.Updated -= OnConfigurationUpdated;
        return Task.CompletedTask;
    }

    public void Dispose() => configuration.Updated -= OnConfigurationUpdated;
}
