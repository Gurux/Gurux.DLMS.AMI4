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

/// <summary>Coordinates agent lifecycle, validated configuration and device task execution.</summary>
public partial class GXAgentWorker
{
    /// <summary>Latest validated configuration snapshot published to the worker.</summary>
    private GXAgentOptionsSnapshot? _configuration;
    /// <summary>Serializes changes to queued meter work.</summary>
    private readonly SemaphoreSlim _meterQueueGate = new(1, 1);

    /// <inheritdoc />
    public GXAgentOptionsSnapshot Configuration => Volatile.Read(ref _configuration)
        ?? throw new InvalidOperationException("Initialize the worker before reading configuration.");

    /// <summary>Gets an independent editable copy of the active configuration.</summary>
    public GXAgentOptions Options => Configuration.ToOptions();

    // Published values are read-only inside the worker. Every writer stages a detached
    // copy and publishes the complete validated snapshot through PublishConfiguration.
    /// <summary>Gets the current validated worker configuration without copying it.</summary>
    internal GXAgentOptions RuntimeOptions => Volatile.Read(ref _configuration)?.RuntimeValue
        ?? throw new InvalidOperationException("The worker configuration is unavailable before initialization.");

    /// <summary>Validates and atomically publishes a detached configuration snapshot.</summary>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="requireAgentId">Whether validation requires completed agent registration.</param>
    private void PublishConfiguration(GXAgentOptions options, bool requireAgentId = true)
    {
        var snapshot = GXAgentOptionsSnapshot.Capture(options, requireAgentId);
        Volatile.Write(ref _configuration, snapshot);
    }

    /// <summary>Creates the bounded device read queue with the configured parallelism.</summary>
    /// <param name="threads">Maximum number of concurrent meter reads.</param>
    /// <returns>A bounded action block executing device work with the requested parallelism.</returns>
    private ActionBlock<GXActionBlock> CreateMeterReads(int threads) => new(
        async action =>
        {
            try
            { await ReadMeter(action); }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested) { }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, threads),
            BoundedCapacity = Math.Max(1, threads)
        });

    /// <summary>Replaces the device read queue after draining work admitted to its predecessor.</summary>
    /// <param name="threads">Maximum number of concurrent meter reads.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <returns>A task representing this operation: Replaces the device read queue after draining work admitted to its predecessor.</returns>
    private async Task ConfigureMeterReadsAsync(int threads, GXAgentOptions? options = null)
    {
        await _meterQueueGate.WaitAsync(_cancellationToken);
        try
        {
            _meterReads.Complete();
            await _meterReads.Completion;
            _cancellationToken.ThrowIfCancellationRequested();
            if (options != null)
                {
                PublishConfiguration(options);
            }
            _meterReads = CreateMeterReads(threads);
        }
        finally { _meterQueueGate.Release(); }
    }

    /// <summary>Admits device work to the current queue while maintaining queue metrics.</summary>
    /// <param name="action">Queued device work or repository action being processed.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Admits device work to the current queue while maintaining queue metrics.</returns>
    internal async Task QueueMeterReadAsync(GXActionBlock action, CancellationToken token)
    {
        action.QueuedAt = Stopwatch.GetTimestamp();
        Interlocked.Exchange(ref action.QueueCounted, 1);
        _runtimeMetrics.Queued();
        try
        {
            await _meterQueueGate.WaitAsync(token);
            try
            {
                if (!await _meterReads.SendAsync(action, token))
                {
                    throw new InvalidOperationException("The meter read queue did not accept the task batch.");
                }
            }
            finally { _meterQueueGate.Release(); }
        }
        catch
        {
            if (Interlocked.Exchange(ref action.QueueCounted, 0) != 0)
            {
                _runtimeMetrics.QueueRemoved();
            }
            throw;
        }
    }
    /// <summary>Compares transport configuration through its stable JSON representation.</summary>
    /// <param name="previous">Previously published settings used for comparison.</param>
    /// <param name="candidate">Prepared script version to publish.</param>
    /// <returns>Whether both settings values have identical JSON representations.</returns>
    /// <typeparam name="T">Configuration value type compared through its JSON representation.</typeparam>
    private static bool SameSettings<T>(T previous, T candidate) =>
        JsonSerializer.Serialize(previous) == JsonSerializer.Serialize(candidate);

    /// <summary>Creates and validates worker options from the server agent registration.</summary>
    /// <param name="agent">Server agent registration used to update local configuration.</param>
    /// <returns>A detached, validated worker configuration reflecting the server registration.</returns>
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

    /// <summary>Publishes server configuration and restarts affected transport services when requested.</summary>
    /// <param name="agent">Server agent registration used to update local configuration.</param>
    /// <param name="restartServices">Whether transport services must restart after applying settings.</param>
    /// <returns>A task representing this operation: Publishes server configuration and restarts affected transport services when requested.</returns>
    private async Task ApplyAgentOptionsAsync(GXAgent agent, bool restartServices)
    {
        await _settingsGate.WaitAsync(_cancellationToken);
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
                await service.StopAsync(_cancellationToken);
                await service.StartAsync(_cancellationToken);
            }
            if (rawChanged)
            {
                var service = _services.GetRequiredService<GXRawNotificationService>();
                await service.StopAsync(_cancellationToken);
                await service.StartAsync(_cancellationToken);
            }
            if (listenerChanged)
            {
                var service = _services.GetRequiredService<GXAutoConnectService>();
                await service.StopAsync(_cancellationToken);
                await service.StartAsync(_cancellationToken);
            }
            if (gatewayChanged)
            {
                var service = _services.GetRequiredService<GXGatewayService>();
                await service.StopAsync(_cancellationToken);
                await service.StartAsync(_cancellationToken);
            }
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            if (published && restartServices)
                {
                SetFaulted();
            }
            throw;
        }
        finally { _settingsGate.Release(); }
    }

    /// <summary>Publishes agent registration or version changes as a new configuration snapshot.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    /// <param name="version">Application release version being verified or published.</param>
    /// <returns>A task representing this operation: Publishes agent registration or version changes as a new configuration snapshot.</returns>
    private async Task PublishIdentityAsync(Guid? id = null, string? version = null)
    {
        await _settingsGate.WaitAsync(_cancellationToken);
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
        finally { _settingsGate.Release(); }
    }
}
