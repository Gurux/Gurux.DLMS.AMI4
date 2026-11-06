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

using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker.AutoConnect;
using Gurux.DLMS.AMI.Agent.Worker.Notifier;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Gurux.DLMS.AMI.Agent.Worker;

public partial class GXAgentWorker
{
    private GXAgentOptionsSnapshot? configuration;
    private readonly SemaphoreSlim meterQueueGate = new(1, 1);

    /// <inheritdoc />
    public GXAgentOptionsSnapshot Configuration => Volatile.Read(ref configuration)
        ?? throw new InvalidOperationException("Initialize the worker before reading configuration.");

    /// <summary>Gets an independent editable copy of the active configuration.</summary>
    public GXAgentOptions Options => Configuration.ToOptions();

    // Published values are read-only inside the worker. Every writer stages a detached
    // copy and publishes the complete validated snapshot through PublishConfiguration.
    internal GXAgentOptions RuntimeOptions => Volatile.Read(ref configuration)?.RuntimeValue
        ?? throw new InvalidOperationException("The worker configuration is unavailable before initialization.");

    private void PublishConfiguration(GXAgentOptions options, bool requireAgentId = true)
    {
        var snapshot = GXAgentOptionsSnapshot.Capture(options, requireAgentId);
        Volatile.Write(ref configuration, snapshot);
    }

    private ActionBlock<GXActionBlock> CreateMeterReads(int threads) => new(
        async action =>
        {
            try
            { await ReadMeter(action); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, threads),
            BoundedCapacity = Math.Max(1, threads)
        });

    private async Task ConfigureMeterReadsAsync(int threads, GXAgentOptions? options = null)
    {
        await meterQueueGate.WaitAsync(cancellationToken);
        try
        {
            _meterReads.Complete();
            await _meterReads.Completion;
            cancellationToken.ThrowIfCancellationRequested();
            if (options != null)
                {
                PublishConfiguration(options);
            }
            _meterReads = CreateMeterReads(threads);
        }
        finally { meterQueueGate.Release(); }
    }

    internal async Task QueueMeterReadAsync(GXActionBlock action, CancellationToken token)
    {
        action.QueuedAt = Stopwatch.GetTimestamp();
        Interlocked.Exchange(ref action.QueueCounted, 1);
        runtimeMetrics.Queued();
        try
        {
            await meterQueueGate.WaitAsync(token);
            try
            {
                if (!await _meterReads.SendAsync(action, token))
                {
                    throw new InvalidOperationException("The meter read queue did not accept the task batch.");
                }
            }
            finally { meterQueueGate.Release(); }
        }
        catch
        {
            if (Interlocked.Exchange(ref action.QueueCounted, 0) != 0)
            {
                runtimeMetrics.QueueRemoved();
            }
            throw;
        }
    }
    private static bool SameSettings<T>(T previous, T candidate) =>
        JsonSerializer.Serialize(previous) == JsonSerializer.Serialize(candidate);

    private GXAgentOptions ValidatedOptions(GXAgent agent)
    {
        var candidate = Configuration.ToOptions();
        T? Read<T>(string? json, T? previous) => string.IsNullOrEmpty(json) ? previous : JsonSerializer.Deserialize<T>(json);
        candidate.TraceLevel = agent.TraceLevel ?? candidate.TraceLevel;
        candidate.SerialPort = agent.SerialPort;
        candidate.ReaderSettings = Read(agent.ReaderSettings, candidate.ReaderSettings);
        candidate.ListenerSettings = Read(agent.ListenerSettings, candidate.ListenerSettings);
        candidate.NotifySettings = Read(agent.NotifySettings, candidate.NotifySettings);
        candidate.RawNotificationSettings = Read(agent.RawNotificationSettings, candidate.RawNotificationSettings);
        candidate.GatewaySettings = Read(agent.GatewaySettings, candidate.GatewaySettings);
        // Validate download identifiers too, without claiming the new runtime version before installation.
        var runningVersion = candidate.Version;
        candidate.Version = agent.UpdateVersion ?? runningVersion;
        candidate.Validate();
        candidate.Version = runningVersion;
        if (!string.IsNullOrEmpty(candidate.SerialPort) && candidate.ReaderSettings != null)
            {
            candidate.ReaderSettings.Threads = 1;
        }
        return candidate;
    }

    private async Task ApplyAgentOptionsAsync(GXAgent agent, bool restartServices)
    {
        await settingsGate.WaitAsync(cancellationToken);
        bool published = false;
        try
        {
            var previous = Configuration.ToOptions();
            var candidate = ValidatedOptions(agent);
            if (SameSettings(previous, candidate))
                {
                return;
            }
            var notifyChanged = !SameSettings(previous.NotifySettings, candidate.NotifySettings);
            var rawChanged = !SameSettings(previous.RawNotificationSettings, candidate.RawNotificationSettings);
            var listenerChanged = !SameSettings(previous.ListenerSettings, candidate.ListenerSettings);
            var gatewayChanged = !SameSettings(previous.GatewaySettings, candidate.GatewaySettings);
            if ((previous.ReaderSettings?.Threads ?? 1) != (candidate.ReaderSettings?.Threads ?? 1))
                {
                await ConfigureMeterReadsAsync(candidate.ReaderSettings?.Threads ?? 1, candidate);
            }
            else
                {
                PublishConfiguration(candidate);
            }
            published = true;
            _hostNotifications?.NotifySettingsChanged();
            _logger?.LogInformation("Agent settings updated: {Settings}", Configuration);
            if (!restartServices || _services == null)
                {
                return;
            }
            if (notifyChanged)
            {
                var service = _services.GetRequiredService<GXNotifyService>();
                await service.StopAsync(cancellationToken);
                await service.StartAsync(cancellationToken);
            }
            if (rawChanged)
            {
                var service = _services.GetRequiredService<GXRawNotificationService>();
                await service.StopAsync(cancellationToken);
                await service.StartAsync(cancellationToken);
            }
            if (listenerChanged)
            {
                var service = _services.GetRequiredService<GXAutoConnectService>();
                await service.StopAsync(cancellationToken);
                await service.StartAsync(cancellationToken);
            }
            if (gatewayChanged)
            {
                var service = _services.GetRequiredService<GXGatewayService>();
                await service.StopAsync(cancellationToken);
                await service.StartAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            if (published && restartServices)
                {
                SetFaulted();
            }
            throw;
        }
        finally { settingsGate.Release(); }
    }

    private async Task PublishIdentityAsync(Guid? id = null, string? version = null)
    {
        await settingsGate.WaitAsync(cancellationToken);
        try
        {
            var candidate = Configuration.ToOptions();
            if (id.HasValue)
                {
                candidate.Id = id.Value;
            }
            if (version != null)
                {
                candidate.Version = version;
            }
            PublishConfiguration(candidate);
        }
        finally { settingsGate.Release(); }
    }
}
