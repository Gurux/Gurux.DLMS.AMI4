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
internal sealed class GXScriptExecution(TimeSpan timeout, CancellationToken lifetime)
{
    private sealed class Invocation
    {
        internal bool Running;
        internal bool Released;
        internal bool Abandoned;
        internal bool CleanupRunning;
        internal bool CleanupAbandoned;
        internal Task CancellationCallbacks = Task.CompletedTask;
        internal CancellationTokenSource? Cancellation;
    }

    private readonly Lock gate = new();
    private readonly ConditionalWeakTable<GXScriptRunArgs, Invocation> invocations = new();
    private int abandoned;

    internal bool CanExecute
    {
        get
        {
            lock (gate)
            {
                return abandoned == 0;
            }
        }
    }

    internal void CheckAdmission()
    {
        if (!CanExecute)
        {
            throw new InvalidOperationException("A cancelled or timed-out script is still running. New script execution is paused until it finishes or the agent process is restarted.");
        }
    }

    internal async Task<object?> RunAsync(GXScriptRunArgs args, Func<CancellationToken, Task<object?>> invoke, CancellationToken token)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(lifetime, token);
        waiting.Token.ThrowIfCancellationRequested();
        Invocation invocation;
        Task<object?> work;
        lock (gate)
        {
            CheckAdmission();
            invocation = invocations.GetOrCreateValue(args);
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
                        lock (gate)
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
                        lock (gate)
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
                                --abandoned;
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
            lock (gate)
            {
                if (invocation.Running)
                {
                    invocation.Abandoned = true;
                    ++abandoned;
                    // User cancellation callbacks can block. CancelAsync keeps them off the caller.
                    Observe(invocation.CancellationCallbacks = invocation.Cancellation!.CancelAsync());
                }
            }
            // Observe late failures without awaiting non-cooperative code or unloading its context.
            Observe(work);
            throw;
        }
    }

    internal void Release(GXScriptRunArgs args)
    {
        lock (gate)
        {
            var invocation = invocations.GetOrCreateValue(args);
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
                lock (gate)
                {
                    invocation.CleanupRunning = false;
                    if (invocation.CleanupAbandoned)
                    {
                        invocation.CleanupAbandoned = false;
                        --abandoned;
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
                lock (gate)
                {
                    if (invocation.CleanupRunning)
                    {
                        invocation.CleanupAbandoned = true;
                        ++abandoned;
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
    private static void Observe(Task task)
    {
        _ = task.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
