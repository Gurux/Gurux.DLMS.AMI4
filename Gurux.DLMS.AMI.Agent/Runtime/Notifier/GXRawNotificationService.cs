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

using System.Collections.Concurrent;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier;

/// <summary>Runs the configured raw parser independently of the meter notification listener.</summary>
/// <param name="services">Dependency registrations used to initialize the worker.</param>
/// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
/// <param name="worker">Agent runtime being registered or reconfigured.</param>
internal sealed class GXRawNotificationService(IServiceProvider services, ILogger<GXRawNotificationService> logger, GXAgentWorker worker) : IHostedService
{
    /// <summary>Owns the active leased-notification processing workers.</summary>
    private GXRawNotificationHandler? _handler;
    /// <summary>Serializes service startup and shutdown operations.</summary>
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    /// <summary>Gets the script identifier currently configured for notification processing.</summary>
    internal Guid? ScriptId { get; private set; }
    /// <summary>Retains script instance and arguments for one leased raw notification stream.</summary>
    /// <param name="scripts">Owns isolated script contexts and their release lifecycle.</param>
    /// <param name="runner">Loaded script instance retained for the raw stream.</param>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    private sealed class ScriptSession(GXAgentScripts scripts, GXAmiScript runner, GXScriptRunArgs args) : IDisposable
    {
        /// <summary>Loaded script instance retained for a notification stream.</summary>
        internal readonly GXAmiScript Runner = runner;
        /// <summary>Script arguments retained across records in the same notification stream.</summary>
        internal readonly GXScriptRunArgs Args = args;
        /// <inheritdoc/>
        public void Dispose() => scripts.ReleaseRunArgs(Args);
    }
    /// <summary>Tracks active connections or script sessions until their work completes.</summary>
    private readonly ConcurrentDictionary<(Guid Receiver, Guid Connection, Guid Lease), Lazy<Task<ScriptSession>>> _sessions = new();
    /// <summary>Builds the receiver, connection and lease identity used to share script sessions.</summary>
    /// <param name="n">Raw record whose receiver, connection and lease identify the session.</param>
    /// <returns>The receiver, connection and lease identifiers that uniquely own a script session.</returns>
    private static (Guid, Guid, Guid) Key(GXRawNotification n) => (n.Agent?.Id ?? Guid.Empty, n.ConnectionId, n.LeaseToken ?? Guid.Empty);

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken token)
    {
        await _lifecycle.WaitAsync(token);
        try
        {
            if (_handler != null)
            {
                return;
            }

            var settings = worker.RuntimeOptions.RawNotificationSettings;
            if (settings?.Enabled != true)
            {
                return;
            }

            if (settings.Threads < 1 || settings.ScriptMethod is not Guid methodId || methodId == Guid.Empty)
            {
                logger.LogError("Raw notification processing requires a parser script and a positive worker count.");
                return; // Do not claim and discard work when no parser is configured.
            }

            var method = await worker.Scripts.LoadAsync(methodId, [typeof(GXRawNotification)], token);
            ScriptId = method.Script!.Id;
            _handler = new GXRawNotificationHandler(worker.Client, worker.Notification, worker.RuntimeOptions.Id, settings.Threads, async (record, cancellation) =>
            {
                cancellation.ThrowIfCancellationRequested();
                var pending = _sessions.GetOrAdd(Key(record), _ => new Lazy<Task<ScriptSession>>(async () =>
                {
                    // Capture only once for this lease: a hot update applies to the next connection.
                    var snapshot = worker.Scripts.Current(method)!;
                    return new ScriptSession(worker.Scripts, new GXAmiScript(services) { Sender = new GXAgent { Id = worker.RuntimeOptions.Id }, DefaultDeviceTemplate = settings.DefaultDeviceTemplate is Guid template ? new GXDeviceTemplate { Id = template } : null }, await worker.Scripts.CreateRunArgsAsync(snapshot, [record], cancellation));
                }));
                var current = await pending.Value;
                current.Args.Parameters = [record];
                await worker.Scripts.RunPreparedAsync(current.Runner, current.Args, cancellation);

                cancellation.ThrowIfCancellationRequested();
            }, logger, connectionFinished: record =>
            {
                if (_sessions.TryRemove(Key(record), out var pending) && pending.IsValueCreated && pending.Value.IsCompletedSuccessfully)
                {
                    pending.Value.Result.Dispose();
                }
            }, canProcess: () => worker.Scripts.CanExecute);
            await _handler.StartAsync(token);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>Wakes raw notification processing when server work becomes available.</summary>
    internal void Signal() => _handler?.Signal();

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken token)
    {
        await _lifecycle.WaitAsync(token);
        try
        {
            if (_handler == null)
            {
                return;
            }

            var current = _handler;
            _handler = null;
            ScriptId = null;
            await current.StopAsync(token);
            await current.DisposeAsync();
        }
        finally
        {
            _lifecycle.Release();
        }
    }
}
