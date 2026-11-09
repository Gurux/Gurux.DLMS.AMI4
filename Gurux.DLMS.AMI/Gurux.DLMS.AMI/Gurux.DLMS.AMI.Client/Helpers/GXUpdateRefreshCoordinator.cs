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

using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Serializes a view's notification reads and rechecks pending changes against its loaded data.</summary>
public sealed class GXUpdateRefreshCoordinator(
    Func<IReadOnlyList<GXTarget>, bool> needsRefresh, Func<Task> refresh) : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<(string Type, string? Source, string? Id, string? Version), GXTarget> _pending = [];
    private Task? _running;
    private bool _disposed;

    /// <summary>Queues changes; overlapping callers share one task including any required follow-up read.</summary>
    public Task RequestAsync(IEnumerable<GXTarget> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        TaskCompletionSource? completion = null;
        Task result;
        lock (_gate)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }
            foreach (var update in updates)
            {
                _pending[(update.Type, update.Source, update.Id, update.ConcurrencyStamp)] = update;
            }

            if (_pending.Count == 0)
            {
                return _running ?? Task.CompletedTask;
            }
            if (_running == null)
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _running = completion.Task;
            }
            result = _running;
        }
        if (completion != null)
        {
            _ = RunAsync(completion);
        }
        return result;
    }

    private async Task RunAsync(TaskCompletionSource completion)
    {
        GXTarget[] batch = [];
        try
        {
            // Combine notifications dispatched in the same UI turn without a timer delay.
            await Task.Yield();
            while (true)
            {
                lock (_gate)
                {
                    if (_disposed || _pending.Count == 0)
                    {
                        _running = null;
                        completion.TrySetResult();
                        return;
                    }
                    batch = [.. _pending.Values];
                    _pending.Clear();
                }
                // Check actual loaded versions on every pass. Receipt alone never acknowledges a version.
                if (needsRefresh(batch))
                {
                    await refresh();
                }
            }
        }
        catch (Exception error)
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    foreach (var update in batch)
                    {
                        _pending[(update.Type, update.Source, update.Id, update.ConcurrencyStamp)] = update;
                    }
                }
                _running = null;
                completion.TrySetException(error);
            }
        }
    }

    /// <summary>Prevents further reads and discards pending events when the view is removed.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pending.Clear();
        }
    }
}
