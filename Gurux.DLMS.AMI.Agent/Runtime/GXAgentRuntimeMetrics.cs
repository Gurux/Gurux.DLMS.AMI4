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
    private readonly Meter meter = new("Gurux.DLMS.AMI.Agent.Runtime", "1.0");
    private readonly Histogram<double> queueWait;
    private readonly Histogram<double> readDuration;
    private readonly Histogram<double> taskDuration;
    private readonly Histogram<double> httpDuration;
    private readonly Counter<long> completed;
    private int connections;
    private int reads;
    private int queued;

    internal GXAgentRuntimeMetrics()
    {
        meter.CreateObservableGauge("agent.connections.active", () => Volatile.Read(ref connections));
        meter.CreateObservableGauge("agent.reads.active", () => Volatile.Read(ref reads));
        meter.CreateObservableGauge("agent.queue.pending", () => Volatile.Read(ref queued));
        queueWait = meter.CreateHistogram<double>("agent.queue.wait", "s");
        readDuration = meter.CreateHistogram<double>("agent.reads.duration", "s");
        taskDuration = meter.CreateHistogram<double>("agent.tasks.duration", "s");
        httpDuration = meter.CreateHistogram<double>("agent.http.duration", "s");
        completed = meter.CreateCounter<long>("agent.tasks.completed");
    }

    internal void ConnectionOpened() => Interlocked.Increment(ref connections);
    internal void ConnectionClosed() => Interlocked.Decrement(ref connections);
    internal void Queued() => Interlocked.Increment(ref queued);
    internal void QueueRemoved() => Interlocked.Decrement(ref queued);

    internal long ReadStarted(long queuedAt)
    {
        Interlocked.Increment(ref reads);
        if (queuedAt != 0)
        {
            queueWait.Record(Stopwatch.GetElapsedTime(queuedAt).TotalSeconds);
        }
        return Stopwatch.GetTimestamp();
    }

    internal void ReadFinished(long started, string outcome)
    {
        Interlocked.Decrement(ref reads);
        readDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("outcome", outcome));
    }

    internal void TaskFinished(long started, string outcome)
    {
        var tag = new KeyValuePair<string, object?>("outcome", outcome);
        taskDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tag);
        completed.Add(1, tag);
    }

    internal void HttpFinished(long started, string outcome) => httpDuration.Record(
        Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("outcome", outcome));

    public void Dispose() => meter.Dispose();
}
