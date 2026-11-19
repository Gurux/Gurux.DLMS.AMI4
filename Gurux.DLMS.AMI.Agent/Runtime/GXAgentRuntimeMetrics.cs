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

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Instance-owned, low-cardinality measurements for the meter execution pipeline.</summary>
internal sealed class GXAgentRuntimeMetrics : IDisposable
{
    /// <summary>Publishes instruments describing this component's runtime activity.</summary>
    private readonly Meter _meter = new("Gurux.DLMS.AMI.Agent.Runtime", "1.0");
    /// <summary>Records the delay between queuing and starting device work.</summary>
    private readonly Histogram<double> _queueWait;
    /// <summary>Records elapsed time spent reading devices.</summary>
    private readonly Histogram<double> _readDuration;
    /// <summary>Records elapsed time spent executing tasks.</summary>
    private readonly Histogram<double> _taskDuration;
    /// <summary>Records elapsed time spent on HTTP requests.</summary>
    private readonly Histogram<double> _httpDuration;
    /// <summary>Tracks completion of work or input for this component.</summary>
    private readonly Counter<long> _completed;
    /// <summary>Tracks active connections owned by this component.</summary>
    private int _connections;
    /// <summary>Number of device reads currently in progress.</summary>
    private int _reads;
    /// <summary>Number of work items waiting for execution.</summary>
    private int _queued;

    /// <summary>Creates queue, connection, device and HTTP duration instruments.</summary>
    internal GXAgentRuntimeMetrics()
    {
        _meter.CreateObservableGauge("agent.connections.active", () => Volatile.Read(ref _connections));
        _meter.CreateObservableGauge("agent.reads.active", () => Volatile.Read(ref _reads));
        _meter.CreateObservableGauge("agent.queue.pending", () => Volatile.Read(ref _queued));
        _queueWait = _meter.CreateHistogram<double>("agent.queue.wait", "s");
        _readDuration = _meter.CreateHistogram<double>("agent.reads.duration", "s");
        _taskDuration = _meter.CreateHistogram<double>("agent.tasks.duration", "s");
        _httpDuration = _meter.CreateHistogram<double>("agent.http.duration", "s");
        _completed = _meter.CreateCounter<long>("agent.tasks.completed");
    }

    /// <summary>Increments the number of active device connections.</summary>
    internal void ConnectionOpened() => Interlocked.Increment(ref _connections);
    /// <summary>Decrements the number of active device connections.</summary>
    internal void ConnectionClosed() => Interlocked.Decrement(ref _connections);
    /// <summary>Increments the number of queued device reads.</summary>
    internal void Queued() => Interlocked.Increment(ref _queued);
    /// <summary>Removes a device read from the queue measurement.</summary>
    internal void QueueRemoved() => Interlocked.Decrement(ref _queued);

    /// <summary>Records queue wait time and marks a device read as active.</summary>
    /// <param name="queuedAt">Monotonic timestamp at which the device read was queued.</param>
    /// <returns>The monotonic timestamp marking the beginning of the active device read.</returns>
    internal long ReadStarted(long queuedAt)
    {
        Interlocked.Increment(ref _reads);
        if (queuedAt != 0)
        {
            _queueWait.Record(Stopwatch.GetElapsedTime(queuedAt).TotalSeconds);
        }
        return Stopwatch.GetTimestamp();
    }

    /// <summary>Records read duration and releases the active read measurement.</summary>
    /// <param name="started">Monotonic timestamp at which the measured operation started.</param>
    /// <param name="outcome">Bounded diagnostic outcome label recorded with the duration.</param>
    internal void ReadFinished(long started, string outcome)
    {
        Interlocked.Decrement(ref _reads);
        _readDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("outcome", outcome));
    }

    /// <summary>Records the duration and outcome of a completed device task.</summary>
    /// <param name="started">Monotonic timestamp at which the measured operation started.</param>
    /// <param name="outcome">Bounded diagnostic outcome label recorded with the duration.</param>
    internal void TaskFinished(long started, string outcome)
    {
        var tag = new KeyValuePair<string, object?>("outcome", outcome);
        _taskDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tag);
        _completed.Add(1, tag);
    }

    /// <summary>Records the duration and outcome of an HTTP request.</summary>
    /// <param name="started">Monotonic timestamp at which the measured operation started.</param>
    /// <param name="outcome">Bounded diagnostic outcome label recorded with the duration.</param>
    internal void HttpFinished(long started, string outcome) => _httpDuration.Record(
        Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("outcome", outcome));

    /// <inheritdoc/>
    public void Dispose() => _meter.Dispose();
}
