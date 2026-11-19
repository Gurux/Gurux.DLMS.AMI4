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

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Owns incoming connections until their asynchronous work and cleanup finish.</summary>
/// <param name="capacity">Maximum number of tracked concurrent connection sessions.</param>
/// <param name="lifetime">Cancellation lifetime shared by owned work.</param>
/// <param name="reportError">Records failures encountered while aborting connection work.</param>
internal sealed class GXConnectionTasks(int capacity, CancellationToken lifetime, Action<Exception> reportError)
{
    /// <summary>Owns connection cancellation, cleanup and completion tracking.</summary>
    /// <param name="lifetime">Cancellation lifetime shared by owned work.</param>
    /// <param name="abort">Interrupts the underlying transport when the connection is canceled.</param>
    /// <param name="owner">Optional ownership identity used to group connection work.</param>
    private sealed class Connection(CancellationToken lifetime, Action abort, string? owner)
    {
        /// <summary>Cancels work associated with the registered connection.</summary>
        internal readonly CancellationTokenSource Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        /// <summary>Completes when the registered connection finishes and releases ownership.</summary>
        internal readonly TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Optional identity used to group or stop related connections.</summary>
        internal readonly string? Owner = owner;
        /// <summary>Shared asynchronous abort operation for the connection.</summary>
        private Task? _abortTask;
        /// <summary>Runs the connection's abort callback once and reports callback failures.</summary>
        /// <param name="reportError">Records failures encountered while aborting connection work.</param>
        /// <returns>A task representing this operation: Runs the connection's abort callback once and reports callback failures.</returns>
        internal Task AbortAsync(Action<Exception> reportError)
        {
            lock (this)
                return _abortTask ??= Task.Run(() =>
                {
                    try
                    { abort(); }
                    catch (Exception error) { reportError(error); }
                });
        }
    }
    /// <summary>Tracks active connections owned by this component.</summary>
    private readonly Dictionary<string, Connection> _connections = [];
    /// <summary>Prevents new work after shutdown begins.</summary>
    private bool _stopping;
    /// <summary>Gets the number of tracked device connections.</summary>
    internal int Count { get { lock (_connections) return _connections.Count; } }

    /// <summary>Admits a uniquely identified connection while capacity and lifecycle state permit it.</summary>
    /// <param name="key">Identity of the tracked connection.</param>
    /// <param name="run">Runs connection work within its cancellation lifetime.</param>
    /// <param name="abort">Interrupts the underlying transport when the connection is canceled.</param>
    /// <param name="owner">Optional ownership identity used to group connection work.</param>
    /// <returns>Whether the connection was admitted without exceeding capacity or replacing an existing identity.</returns>
    internal bool TryStart(string key, Func<CancellationToken, Task> run, Action abort, string? owner = null)
    {
        Connection connection;
        Connection? previous = null;
        lock (_connections)
        {
            if (_stopping || lifetime.IsCancellationRequested || _connections.ContainsKey(key))
                {
                return false;
            }
            if (owner != null)
            {
                var owned = _connections.Values.Where(c => c.Owner == owner).ToArray();
                // At most one replacement may wait for its predecessor to finish cleanup.
                if (owned.Length > 1)
                    {
                    return false;
                }
                previous = owned.SingleOrDefault();
            }
            if (previous == null && _connections.Count >= Math.Max(1, capacity))
                {
                return false;
            }
            connection = new(lifetime, abort, owner);
            _connections.Add(key, connection);
        }
        if (previous != null)
            {
            Cancel(previous);
        }
        _ = Task.Run(async () =>
        {
            using var cancellation = connection.Cancellation.Token.Register(() => { _ = connection.AbortAsync(reportError); });
            try
            {
                if (previous != null)
                    {
                    await previous.Finished.Task;
                }
                connection.Cancellation.Token.ThrowIfCancellationRequested();
                await run(connection.Cancellation.Token);
            }
            catch (OperationCanceledException) when (connection.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { reportError(error); }
            finally
            {
                await connection.AbortAsync(reportError);
                lock (_connections)
                {
                    _connections.Remove(key);
                    connection.Cancellation.Dispose();
                    connection.Finished.TrySetResult();
                }
            }
        });
        return true;
    }

    /// <summary>Requests connection cancellation while leaving cleanup to its tracked task.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    private static void Cancel(Connection connection)
    {
        try
        { connection.Cancellation.Cancel(); }
        catch (ObjectDisposedException) { /* The session already completed. */ }
    }

    /// <summary>Cancels the tracked connection with the supplied transport identity.</summary>
    /// <param name="key">Identity of the tracked connection.</param>
    internal void Disconnect(string key)
    {
        Connection? connection;
        lock (_connections)
            _connections.TryGetValue(key, out connection);
        if (connection != null)
            {
            Cancel(connection);
        }
    }

    /// <summary>Stops accepting work and waits for admitted operations to finish.</summary>
    /// <returns>A task representing this operation: Stops accepting work and waits for admitted operations to finish.</returns>
    internal Task StopAsync()
    {
        Connection[] pending;
        lock (_connections)
        {
            _stopping = true;
            pending = [.. _connections.Values];
        }
        foreach (var connection in pending)
            {
            Cancel(connection);
        }
        return Task.WhenAll(pending.Select(c => c.Finished.Task));
    }

    /// <summary>Waits for a signal or terminal state within the configured cancellation lifetime.</summary>
    /// <param name="handle">Synchronization handle signaling that work may proceed.</param>
    /// <param name="timeout">Maximum duration to wait before checking for further work.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Waits for a signal or terminal state within the configured cancellation lifetime.</returns>
    internal static async Task WaitAsync(WaitHandle handle, TimeSpan timeout, CancellationToken token)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wait = ThreadPool.RegisterWaitForSingleObject(handle, (_, _) => completion.TrySetResult(), null, timeout, true);
        using var cancellation = token.Register(() => completion.TrySetCanceled(token));
        try
        { await completion.Task; }
        finally { wait.Unregister(null); }
    }
}
