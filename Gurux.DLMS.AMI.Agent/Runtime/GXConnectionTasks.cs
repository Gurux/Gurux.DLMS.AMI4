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
internal sealed class GXConnectionTasks(int capacity, CancellationToken lifetime, Action<Exception> reportError)
{
    private sealed class Connection(CancellationToken lifetime, Action abort, string? owner)
    {
        internal readonly CancellationTokenSource Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        internal readonly TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly string? Owner = owner;
        private Task? abortTask;
        internal Task AbortAsync(Action<Exception> reportError)
        {
            lock (this)
                return abortTask ??= Task.Run(() =>
                {
                    try
                    { abort(); }
                    catch (Exception error) { reportError(error); }
                });
        }
    }
    private readonly Dictionary<string, Connection> connections = [];
    private bool stopping;
    internal int Count { get { lock (connections) return connections.Count; } }

    internal bool TryStart(string key, Func<CancellationToken, Task> run, Action abort, string? owner = null)
    {
        Connection connection;
        Connection? previous = null;
        lock (connections)
        {
            if (stopping || lifetime.IsCancellationRequested || connections.ContainsKey(key))
                {
                return false;
            }
            if (owner != null)
            {
                var owned = connections.Values.Where(c => c.Owner == owner).ToArray();
                // At most one replacement may wait for its predecessor to finish cleanup.
                if (owned.Length > 1)
                    {
                    return false;
                }
                previous = owned.SingleOrDefault();
            }
            if (previous == null && connections.Count >= Math.Max(1, capacity))
                {
                return false;
            }
            connection = new(lifetime, abort, owner);
            connections.Add(key, connection);
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
                lock (connections)
                {
                    connections.Remove(key);
                    connection.Cancellation.Dispose();
                    connection.Finished.TrySetResult();
                }
            }
        });
        return true;
    }

    private static void Cancel(Connection connection)
    {
        try
        { connection.Cancellation.Cancel(); }
        catch (ObjectDisposedException) { /* The session already completed. */ }
    }

    internal void Disconnect(string key)
    {
        Connection? connection;
        lock (connections)
            connections.TryGetValue(key, out connection);
        if (connection != null)
            {
            Cancel(connection);
        }
    }

    internal Task StopAsync()
    {
        Connection[] pending;
        lock (connections)
        {
            stopping = true;
            pending = [.. connections.Values];
        }
        foreach (var connection in pending)
            {
            Cancel(connection);
        }
        return Task.WhenAll(pending.Select(c => c.Finished.Task));
    }

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
