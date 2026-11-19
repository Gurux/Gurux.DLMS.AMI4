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

namespace Gurux.DLMS.AMI.Client.Pages.Module;

/// <summary>Owns the cancellation and completion of visible health polling.</summary>
public sealed class ModuleHealthPolling : IAsyncDisposable
{
    /// <summary>Cancellation source of the currently visible health polling loop.</summary>
    private CancellationTokenSource? _cancellation;
    /// <summary>Current and stopping polling tasks retained until their completion is observed.</summary>
    private readonly List<Task> _tasks = [];
    /// <summary>Cancellation sources corresponding to retained polling tasks.</summary>
    private readonly List<CancellationTokenSource> _sources = [];
    /// <summary>Prevents new polling loops after asynchronous disposal starts.</summary>
    private bool _disposed;
    /// <summary>Cancels the previous loop and immediately starts refreshing health every five seconds.</summary>
    /// <param name="refresh">Callback that reloads the displayed data.</param>
    /// <exception cref="ObjectDisposedException">Polling has already been disposed.</exception>
    public void Start(Func<CancellationToken, Task> refresh)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop();
        _cancellation = new CancellationTokenSource();
        for (int pos = _tasks.Count - 1; pos >= 0; --pos)
        {
            if (_tasks[pos].IsCompleted)
            {
                _sources[pos].Dispose();
                _sources.RemoveAt(pos);
                _tasks.RemoveAt(pos);
            }
        }
        _sources.Add(_cancellation);
        _tasks.Add(RunAsync(refresh, _cancellation));
    }
    /// <summary>Cancels the active polling loop without blocking for completion.</summary>
    public void Stop()
    {
        _cancellation?.Cancel();
        _cancellation = null;
    }
    /// <summary>Refreshes health immediately and on five-second ticks until its source is canceled.</summary>
    /// <param name="refresh">Callback that reloads the displayed data.</param>
    /// <param name="source">Cancellation source that owns the lifetime of this polling loop.</param>
    /// <returns>A task that completes when cancellation has stopped the polling loop.</returns>
    private static async Task RunAsync(Func<CancellationToken, Task> refresh, CancellationTokenSource source)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            do
            {
                await refresh(source.Token);
            }
            while (await timer.WaitForNextTickAsync(source.Token));
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
    }
    /// <summary>Cancels polling, awaits all retained loops and disposes their cancellation sources.</summary>
    /// <returns>An operation that completes after polling resources have been released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Stop();
        await Task.WhenAll(_tasks);
        _tasks.Clear();
        foreach (var source in _sources)
        {
            source.Dispose();
        }
        _sources.Clear();
    }
}
