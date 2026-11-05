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
internal sealed class GXRawNotificationService(IServiceProvider services, ILogger<GXRawNotificationService> logger, GXAgentWorker worker) : IHostedService
{
    private GXRawNotificationHandler? handler;
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    internal Guid? ScriptId { get; private set; }
    private sealed class ScriptSession(GXAmiScript runner, GXScriptRunArgs args) : IDisposable
    {
        internal readonly GXAmiScript Runner = runner;
        internal readonly GXScriptRunArgs Args = args;
        public void Dispose() => Args.AssemblyLoadContext?.Unload();
    }
    private readonly ConcurrentDictionary<(Guid Receiver, Guid Connection, Guid Lease), Lazy<Task<ScriptSession>>> sessions = new();
    private static (Guid, Guid, Guid) Key(GXRawNotification n) => (n.Agent?.Id ?? Guid.Empty, n.ConnectionId, n.LeaseToken ?? Guid.Empty);

    public async Task StartAsync(CancellationToken token)
    {
        await lifecycle.WaitAsync(token);
        try
        {
            if (handler != null)
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
            handler = new GXRawNotificationHandler(worker.client, worker.Notification, worker.RuntimeOptions.Id, settings.Threads, async (record, cancellation) =>
            {
                cancellation.ThrowIfCancellationRequested();
                var pending = sessions.GetOrAdd(Key(record), _ => new Lazy<Task<ScriptSession>>(async () =>
                {
                    // Capture only once for this lease: a hot update applies to the next connection.
                    var snapshot = worker.Scripts.Current(method)!;
                    return new ScriptSession(new GXAmiScript(services) { Sender = new GXAgent { Id = worker.RuntimeOptions.Id }, DefaultDeviceTemplate = settings.DefaultDeviceTemplate is Guid template ? new GXDeviceTemplate { Id = template } : null }, await worker.Scripts.CreateRunArgsAsync(snapshot, [record], cancellation));
                }));
                var current = await pending.Value;
                current.Args.Parameters = [record];
                // Await the returned script Task too: RunAsync returns reflection's result as an object.
                var result = await current.Runner.RunAsync(current.Args);
                if (result is Task task)
                {
                    await task;
                }
                else if (result is ValueTask valueTask)
                {
                    await valueTask;
                }

                cancellation.ThrowIfCancellationRequested();
            }, logger, connectionFinished: record =>
            {
                if (sessions.TryRemove(Key(record), out var pending) && pending.IsValueCreated && pending.Value.IsCompletedSuccessfully)
                {
                    pending.Value.Result.Dispose();
                }
            });
            await handler.StartAsync(token);
        }
        finally
        {
            lifecycle.Release();
        }
    }

    internal void Signal() => handler?.Signal();

    public async Task StopAsync(CancellationToken token)
    {
        await lifecycle.WaitAsync(token);
        try
        {
            if (handler == null)
            {
                return;
            }

            var current = handler;
            handler = null;
            ScriptId = null;
            await current.StopAsync(token);
            await current.DisposeAsync();
        }
        finally
        {
            lifecycle.Release();
        }
    }
}
