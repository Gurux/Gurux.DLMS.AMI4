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

using System.Runtime.CompilerServices;
using Gurux.DLMS.AMI.Script;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Bounds caller waits while retaining ownership of non-cooperative script code until it actually finishes.</summary>
/// <param name="timeout">Maximum duration to wait before checking for further work.</param>
/// <param name="lifetime">Cancellation lifetime shared by owned work.</param>
internal sealed class GXScriptExecution(TimeSpan timeout, CancellationToken lifetime)
{
    /// <summary>Tracks script completion, cancellation and cleanup after timeout.</summary>
    private sealed class Invocation
    {
        /// <summary>Tracks the script execution task until it finishes.</summary>
        internal bool Running;
        /// <summary>Indicates that ownership of invocation resources has been released.</summary>
        internal bool Released;
        /// <summary>Indicates that the invocation exceeded its allowed lifetime.</summary>
        internal bool Abandoned;
        /// <summary>Tracks cleanup work that is still executing.</summary>
        internal bool CleanupRunning;
        /// <summary>Indicates that cleanup exceeded its allowed lifetime.</summary>
        internal bool CleanupAbandoned;
        /// <summary>Tracks asynchronous callbacks triggered by script cancellation.</summary>
        internal Task CancellationCallbacks = Task.CompletedTask;
        /// <summary>Cancels work associated with the registered connection.</summary>
        internal CancellationTokenSource? Cancellation;
    }

    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly Lock _gate = new();
    /// <summary>Associates script arguments with their tracked invocation state.</summary>
    private readonly ConditionalWeakTable<GXScriptRunArgs, Invocation> _invocations = [];
    /// <summary>Number of script invocations that outlived their allowed execution window.</summary>
    private int _abandoned;

    /// <summary>Gets whether capacity remains for another isolated script execution.</summary>
    internal bool CanExecute
    {
        get
        {
            lock (_gate)
            {
                return _abandoned == 0;
            }
        }
    }

    /// <summary>Rejects new script work while abandoned executions exhaust the execution budget.</summary>
    internal void CheckAdmission()
    {
        if (!CanExecute)
        {
            throw new InvalidOperationException("A cancelled or timed-out script is still running. New script execution is paused until it finishes or the agent process is restarted.");
        }
    }

    /// <summary>Runs an isolated script with bounded execution and asynchronous cleanup after timeout.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    /// <param name="invoke">Executes the script using the supplied cancellation token.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The script return value after bounded execution completes.</returns>
    internal async Task<object?> RunAsync(GXScriptRunArgs args, Func<CancellationToken, Task<object?>> invoke, CancellationToken token)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(lifetime, token);
        waiting.Token.ThrowIfCancellationRequested();
        Invocation invocation;
        Task<object?> work;
        lock (_gate)
        {
            CheckAdmission();
            invocation = _invocations.GetOrCreateValue(args);
            ObjectDisposedException.ThrowIf(invocation.Released, args);
            if (invocation.Running)
            {
                throw new InvalidOperationException("This script context is already executing.");
            }
            invocation.Running = true;
            invocation.CancellationCallbacks = Task.CompletedTask;
            invocation.Cancellation = new CancellationTokenSource();
            var executionToken = invocation.Cancellation.Token;
            args.CancellationToken = executionToken;
            // Offload even synchronous entry points: the caller must be able to stop waiting.
            work = Task.Run(async () =>
            {
                try
                {
                    return await invoke(executionToken).ConfigureAwait(false);
                }
                finally
                {
                    while (true)
                    {
                        Task callbacks;
                        lock (_gate)
                        {
                            callbacks = invocation.CancellationCallbacks;
                        }
                        try
                        {
                            await callbacks.ConfigureAwait(false);
                        }
                        catch
                        {
                            // Callback failure is observed; ownership lasts until every callback returns.
                        }
                        lock (_gate)
                        {
                            // Cancellation may have installed callbacks between the snapshot and this lock.
                            if (!ReferenceEquals(callbacks, invocation.CancellationCallbacks))
                            {
                                continue;
                            }
                            invocation.Running = false;
                            if (invocation.Abandoned)
                            {
                                invocation.Abandoned = false;
                                --_abandoned;
                            }
                            invocation.Cancellation.Dispose();
                            invocation.Cancellation = null;
                            if (invocation.Released)
                            {
                                ScheduleUnload(args, invocation);
                            }
                            break;
                        }
                    }
                }
            });
        }
        try
        {
            return await work.WaitAsync(timeout, waiting.Token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is TimeoutException or OperationCanceledException)
        {
            lock (_gate)
            {
                if (invocation.Running)
                {
                    invocation.Abandoned = true;
                    ++_abandoned;
                    // User cancellation callbacks can block. CancelAsync keeps them off the caller.
                    Observe(invocation.CancellationCallbacks = invocation.Cancellation!.CancelAsync());
                }
            }
            // Observe late failures without awaiting non-cooperative code or unloading its context.
            Observe(work);
            throw;
        }
    }

    /// <summary>Releases script arguments and schedules cleanup after outstanding execution finishes.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    internal void Release(GXScriptRunArgs args)
    {
        lock (_gate)
        {
            var invocation = _invocations.GetOrCreateValue(args);
            if (invocation.Released)
            {
                return;
            }
            invocation.Released = true;
            if (!invocation.Running)
            {
                ScheduleUnload(args, invocation);
            }
        }
    }

    // Unloading raises user callbacks synchronously. Never run them on a caller or under gate.
    /// <summary>Schedules script context cleanup without blocking the caller on abandoned execution.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    /// <param name="invocation">Tracked script execution whose resources require cleanup.</param>
    private void ScheduleUnload(GXScriptRunArgs args, Invocation invocation)
    {
        invocation.CleanupRunning = true;
        var cleanup = Task.Run(() =>
        {
            try
            {
                args.AssemblyLoadContext?.Unload();
            }
            finally
            {
                lock (_gate)
                {
                    invocation.CleanupRunning = false;
                    if (invocation.CleanupAbandoned)
                    {
                        invocation.CleanupAbandoned = false;
                        --_abandoned;
                    }
                }
            }
        });
        _ = MonitorCleanupAsync();

        async Task MonitorCleanupAsync()
        {
            try
            {
                await cleanup.WaitAsync(timeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                lock (_gate)
                {
                    if (invocation.CleanupRunning)
                    {
                        invocation.CleanupAbandoned = true;
                        ++_abandoned;
                    }
                }
                Observe(cleanup);
            }
            catch
            {
                // Awaiting observes a failed user callback; cleanup must never fault the host.
            }
        }
    }
    /// <summary>Observes a background task fault so abandoned script failures do not become unobserved.</summary>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    private static void Observe(Task task)
    {
        _ = task.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
