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

/// <summary>Coordinates agent lifecycle, validated configuration and device task execution.</summary>
public partial class GXAgentWorker
{
    /// <summary>Serializes initialization, startup and shutdown transitions.</summary>
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    /// <summary>Indicates successful initialization of worker-owned services.</summary>
    private bool _initialized;
    /// <summary>Prevents new work after shutdown begins.</summary>
    private bool _stopping;
    /// <summary>Indicates that owned resources have been released.</summary>
    private bool _disposed;
    /// <summary>Shared task representing the current startup operation.</summary>
    private Task? _startupTask;
    /// <summary>Shared task representing the current shutdown operation.</summary>
    private Task? _stopTask;
    /// <summary>Registration forwarding host cancellation into worker shutdown.</summary>
    private CancellationTokenRegistration _hostCancellation;
    /// <summary>Atomically published worker lifecycle state.</summary>
    private int _workerState = (int)AgentWorkerState.Created;
    /// <summary>Atomically published server connectivity state.</summary>
    private int _connectionState = (int)AgentConnectionState.Disconnected;

    /// <inheritdoc />
    public AgentWorkerState State => (AgentWorkerState)Volatile.Read(ref _workerState);
    /// <inheritdoc />
    public AgentConnectionState ConnectionState => (AgentConnectionState)Volatile.Read(ref _connectionState);

    /// <summary>Publishes the server connection state while preserving rejected authentication state.</summary>
    /// <param name="value">New configuration or state value to publish.</param>
    private void SetConnectionState(AgentConnectionState value) => Volatile.Write(ref _connectionState,
        (int)(_cancellationToken.IsCancellationRequested ? AgentConnectionState.Disconnected : value));

    /// <summary>Transitions the worker to a faulted state unless shutdown already owns its lifecycle.</summary>
    private void SetFaulted()
    {
        while (State is AgentWorkerState.Initialized or AgentWorkerState.Starting or AgentWorkerState.Running or AgentWorkerState.RestartPending)
        {
            var previous = State;
            if (previous is AgentWorkerState.Stopping or AgentWorkerState.Stopped or AgentWorkerState.Disposed) {
                return;
            }
            if (Interlocked.CompareExchange(ref _workerState, (int)AgentWorkerState.Faulted, (int)previous) == (int)previous) {
                return;
            }
        }
    }

    /// <summary>Publishes that a verified application release awaits host update handoff.</summary>
    private void MarkRestartPending()
    {
        var previous = State;
        while (previous is AgentWorkerState.Starting or AgentWorkerState.Running)
        {
            var actual = (AgentWorkerState)Interlocked.CompareExchange(ref _workerState, (int)AgentWorkerState.RestartPending, (int)previous);
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
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_initialized || _stopping)
                {
                throw new InvalidOperationException("Initialize a new worker instance before starting. A stopped worker cannot be restarted.");
            }
            RuntimeOptions.Validate();
            if (_startupTask == null)
            {
                Volatile.Write(ref _workerState, (int)AgentWorkerState.Starting);
                _hostCancellation = cancellationToken.Register(() => _lifetime.Cancel());
                _startupTask = RunStartupAsync();
            }
            startup = _startupTask;
        }
        finally { _lifecycleGate.Release(); }
        await startup.WaitAsync(cancellationToken);
    }

    /// <summary>Starts worker services and establishes server notification connectivity.</summary>
    /// <returns>A task representing this operation: Starts worker services and establishes server notification connectivity.</returns>
    private async Task RunStartupAsync()
    {
        try
        {
            await StartCoreAsync();
            Interlocked.CompareExchange(ref _workerState, (int)(_restartPending ? AgentWorkerState.RestartPending : AgentWorkerState.Running), (int)AgentWorkerState.Starting);
        }
        catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
        {
            Interlocked.CompareExchange(ref _workerState, (int)AgentWorkerState.Stopping, (int)AgentWorkerState.Starting);
            SetConnectionState(AgentConnectionState.Disconnected);
            throw;
        }
        catch
        {
            SetFaulted();
            SetConnectionState(AgentConnectionState.Disconnected);
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
        await _lifecycleGate.WaitAsync(CancellationToken.None);
        try
        {
            if (_disposed) {
                return;
            }
            if (_stopTask == null)
            {
                _stopping = true;
                Volatile.Write(ref _workerState, (int)AgentWorkerState.Stopping);
                _lifetime.Cancel();
                _stopTask = StopAfterStartupAsync();
            }
            shutdown = _stopTask;
        }
        finally { _lifecycleGate.Release(); }
        await shutdown.WaitAsync(cancellationToken);
    }

    /// <summary>Waits for startup to settle before stopping and releasing worker services.</summary>
    /// <returns>A task representing this operation: Waits for startup to settle before stopping and releasing worker services.</returns>
    private async Task StopAfterStartupAsync()
    {
        if (_startupTask != null)
        {
            try { await _startupTask; }
            catch { /* The startup caller receives the original failure. Partial startup still needs cleanup. */ }
        }
        try
        {
            await StopCoreAsync();
            Volatile.Write(ref _workerState, (int)AgentWorkerState.Stopped);
        }
        catch
        {
            Volatile.Write(ref _workerState, (int)AgentWorkerState.Faulted);
            throw;
        }
        finally { SetConnectionState(AgentConnectionState.Disconnected); }
    }

    /// <summary>Stops the worker and releases its own resources. Does not dispose the host's provider or notifications.</summary>
    /// <returns>An asynchronous operation that completes after shutdown and worker-owned resource disposal.</returns>
    /// <remarks>Disposal waits for safe task-result persistence without a timeout. Repeated calls do not dispose resources twice.</remarks>
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _lifecycleGate.WaitAsync(CancellationToken.None);
        try
        {
            if (_disposed) {
                return;
            }
            _disposed = true;
            Volatile.Write(ref _workerState, (int)AgentWorkerState.Disposed);
            _hostCancellation.Dispose();
            _lifetime.Dispose();
            _closing.Dispose();
            _newTask.Dispose();
            _settingsGate.Dispose();
            _updateGate.Dispose();
            _meterQueueGate.Dispose();
            _taskClaimGate.Dispose();
            _taskRequestGate.Dispose();
            _runtimeMetrics.Dispose();
            Client.Dispose();
            _services = null;
            _hostNotifications = null;
        }
        finally { _lifecycleGate.Release(); }
    }

}
