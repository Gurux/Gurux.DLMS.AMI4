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

namespace Gurux.DLMS.AMI.Agent.Worker;

public partial class GXAgentWorker
{
    private readonly SemaphoreSlim lifecycleGate = new(1, 1);
    private bool initialized;
    private bool stopping;
    private bool disposed;
    private Task? startupTask;
    private Task? stopTask;
    private CancellationTokenRegistration hostCancellation;
    private int workerState = (int)GXAgentWorkerState.Created;
    private int connectionState = (int)GXAgentConnectionState.Disconnected;

    /// <inheritdoc />
    public GXAgentWorkerState State => (GXAgentWorkerState)Volatile.Read(ref workerState);
    /// <inheritdoc />
    public GXAgentConnectionState ConnectionState => (GXAgentConnectionState)Volatile.Read(ref connectionState);

    private void SetConnectionState(GXAgentConnectionState value) => Volatile.Write(ref connectionState,
        (int)(cancellationToken.IsCancellationRequested ? GXAgentConnectionState.Disconnected : value));

    private void SetFaulted()
    {
        while (State is GXAgentWorkerState.Initialized or GXAgentWorkerState.Starting or GXAgentWorkerState.Running or GXAgentWorkerState.RestartPending)
        {
            var previous = State;
            if (previous is GXAgentWorkerState.Stopping or GXAgentWorkerState.Stopped or GXAgentWorkerState.Disposed) {
                return;
            }
            if (Interlocked.CompareExchange(ref workerState, (int)GXAgentWorkerState.Faulted, (int)previous) == (int)previous) {
                return;
            }
        }
    }

    private void MarkRestartPending()
    {
        var previous = State;
        while (previous is GXAgentWorkerState.Starting or GXAgentWorkerState.Running)
        {
            var actual = (GXAgentWorkerState)Interlocked.CompareExchange(ref workerState, (int)GXAgentWorkerState.RestartPending, (int)previous);
            if (actual == previous) {
                return;
            }
            previous = actual;
        }
    }

    /// <summary>Starts once. Concurrent or repeated starts share the same startup operation.</summary>
    /// <param name="cancellationToken">Cancels startup and the active worker lifetime.</param>
    /// <returns>A task that completes when the shared startup operation finishes.</returns>
    /// <remarks>Only the first call installs a lifetime token; tokens on later calls cancel their own waits.</remarks>
    /// <exception cref="InvalidOperationException">The worker is not initialized or shutdown has been requested.</exception>
    /// <exception cref="ObjectDisposedException">The worker has been disposed.</exception>
    /// <exception cref="GXAgentOptionsValidationException">The configuration is invalid.</exception>
    /// <exception cref="OperationCanceledException">Startup or the caller's wait is canceled.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Task startup;
        await lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!initialized || stopping)
                {
                throw new InvalidOperationException("Initialize a new worker instance before starting. A stopped worker cannot be restarted.");
            }
            RuntimeOptions.Validate();
            if (startupTask == null)
            {
                Volatile.Write(ref workerState, (int)GXAgentWorkerState.Starting);
                hostCancellation = cancellationToken.Register(() => lifetime.Cancel());
                startupTask = RunStartupAsync();
            }
            startup = startupTask;
        }
        finally { lifecycleGate.Release(); }
        await startup.WaitAsync(cancellationToken);
    }

    private async Task RunStartupAsync()
    {
        try
        {
            await StartCoreAsync();
            Interlocked.CompareExchange(ref workerState, (int)(restartPending ? GXAgentWorkerState.RestartPending : GXAgentWorkerState.Running), (int)GXAgentWorkerState.Starting);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Interlocked.CompareExchange(ref workerState, (int)GXAgentWorkerState.Stopping, (int)GXAgentWorkerState.Starting);
            SetConnectionState(GXAgentConnectionState.Disconnected);
            throw;
        }
        catch
        {
            SetFaulted();
            SetConnectionState(GXAgentConnectionState.Disconnected);
            throw;
        }
    }

    /// <summary>Stops once. Cancellation limits the caller's wait; cleanup continues and must finish before disposing host services.</summary>
    /// <param name="cancellationToken">Cancels waiting for shutdown.</param>
    /// <returns>A task that completes when shared cleanup finishes, or is canceled when this caller stops waiting.</returns>
    /// <remarks>
    /// An already canceled token still requests shutdown. Cleanup may wait for storage recovery while task results remain only in memory.
    /// Create a new worker instance for subsequent startup.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The caller's wait is canceled; cleanup continues.</exception>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task shutdown;
        // Even an already canceled caller requests shutdown; its token only cancels waiting.
        await lifecycleGate.WaitAsync();
        try
        {
            if (disposed) {
                return;
            }
            if (stopTask == null)
            {
                stopping = true;
                Volatile.Write(ref workerState, (int)GXAgentWorkerState.Stopping);
                lifetime.Cancel();
                stopTask = StopAfterStartupAsync();
            }
            shutdown = stopTask;
        }
        finally { lifecycleGate.Release(); }
        await shutdown.WaitAsync(cancellationToken);
    }

    private async Task StopAfterStartupAsync()
    {
        if (startupTask != null)
        {
            try { await startupTask; }
            catch { /* The startup caller receives the original failure. Partial startup still needs cleanup. */ }
        }
        try
        {
            await StopCoreAsync();
            Volatile.Write(ref workerState, (int)GXAgentWorkerState.Stopped);
        }
        catch
        {
            Volatile.Write(ref workerState, (int)GXAgentWorkerState.Faulted);
            throw;
        }
        finally { SetConnectionState(GXAgentConnectionState.Disconnected); }
    }

    /// <summary>Stops the worker and releases its own resources. Does not dispose the host's provider or notifications.</summary>
    /// <returns>An asynchronous operation that completes after shutdown and worker-owned resource disposal.</returns>
    /// <remarks>Disposal waits for safe task-result persistence without a timeout. Repeated calls do not dispose resources twice.</remarks>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await lifecycleGate.WaitAsync();
        try
        {
            if (disposed) {
                return;
            }
            disposed = true;
            Volatile.Write(ref workerState, (int)GXAgentWorkerState.Disposed);
            hostCancellation.Dispose();
            lifetime.Dispose();
            _closing.Dispose();
            _newTask.Dispose();
            settingsGate.Dispose();
            updateGate.Dispose();
            meterQueueGate.Dispose();
            taskClaimGate.Dispose();
            taskRequestGate.Dispose();
            runtimeMetrics.Dispose();
            client.Dispose();
            _services = null;
            _hostNotifications = null;
        }
        finally { lifecycleGate.Release(); }
    }

}
