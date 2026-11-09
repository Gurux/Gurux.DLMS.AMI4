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

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>A reschedulable, serial asynchronous timer with an awaited shutdown.</summary>
internal sealed class GXAsyncTimer : IDisposable
{
    private readonly Func<CancellationToken, Task> _callback;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _stopping = new();
    private readonly SemaphoreSlim _changed = new(0, 1);
    private readonly object _gate = new();
    private readonly Task _loop;
    private DateTimeOffset _due = DateTimeOffset.MaxValue;
    private TimeSpan _period;
    private bool _disposed;

    public GXAsyncTimer(Func<CancellationToken, Task> callback, ILogger logger)
    {
        _callback = callback;
        _logger = logger;
        // Never inherit a request's HTTP context when configuration starts a timer.
        if (ExecutionContext.IsFlowSuppressed()) {
            _loop = Task.Run(RunAsync);
        }
        else
        {
            using (ExecutionContext.SuppressFlow()) _loop = Task.Run(RunAsync);
        }
    }

    public void Change(TimeSpan due, TimeSpan period)
    {
        lock (_gate)
        {
            if (_disposed) {
                return;
            }
            _due = due == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow + due;
            _period = period;
            if (_changed.CurrentCount == 0) {
                _changed.Release();
            }
        }
    }

    private async Task RunAsync()
    {
        var token = _stopping.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                TimeSpan delay;
                lock (_gate)
                {
                    delay = _due == DateTimeOffset.MaxValue ? Timeout.InfiniteTimeSpan : _due - DateTimeOffset.UtcNow;
                    if (delay < TimeSpan.Zero && delay != Timeout.InfiniteTimeSpan) {
                        delay = TimeSpan.Zero;
                    }
                    // SemaphoreSlim has an Int32 millisecond timeout limit.
                    if (delay.TotalMilliseconds > int.MaxValue) {
                        delay = TimeSpan.FromMilliseconds(int.MaxValue);
                    }
                }
                if (await _changed.WaitAsync(delay, token)) {
                    continue;
                }
                lock (_gate)
                {
                    if (_due > DateTimeOffset.UtcNow) {
                        continue;
                    }
                    _due = _period > TimeSpan.Zero ? DateTimeOffset.UtcNow + _period : DateTimeOffset.MaxValue;
                }
                try { await _callback(token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Scheduled background work failed."); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        await _loop.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) {
                return;
            }
            _disposed = true;
        }
        _stopping.Cancel();
        // Waiters own these objects until the callback and loop have returned.
    }
}
