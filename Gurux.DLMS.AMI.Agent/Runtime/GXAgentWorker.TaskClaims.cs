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

using System.Collections.Concurrent;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Coordinates agent lifecycle, validated configuration and device task execution.</summary>
public partial class GXAgentWorker
{
    /// <summary>Serializes claims of server tasks for connected meters.</summary>
    private readonly SemaphoreSlim _taskClaimGate = new(1, 1);
    // Network claims stay serialized; session lifecycle never waits on HTTP while holding its state gate.
    /// <summary>Serializes retrieval of available server tasks.</summary>
    private readonly SemaphoreSlim _taskRequestGate = new(1, 1);
    /// <summary>Active task sessions indexed by meter identifier.</summary>
    private readonly Dictionary<Guid, MeterTaskSession> _connectedMeters = [];

    /// <summary>Deduplicates tasks and owns the work queue for one connected meter.</summary>
    private sealed class MeterTaskSession
    {
        /// <summary>Deduplicates claimed tasks by task identity and lease time.</summary>
        private readonly ConcurrentDictionary<(Guid, DateTimeOffset?), GXTask> _known = new();
        /// <summary>Tasks waiting to be consumed by the connected meter.</summary>
        internal readonly ConcurrentQueue<GXTask> Pending = new();
        /// <summary>Completes when the meter task session closes.</summary>
        internal readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Indicates that the meter session is closing and cannot accept tasks.</summary>
        internal bool Closing;
        /// <summary>Adds newly claimed meter tasks while suppressing duplicate task lease identities.</summary>
        /// <param name="tasks">Newly claimed device tasks to admit to the session.</param>
        internal void Add(IEnumerable<GXTask> tasks)
        {
            foreach (var task in tasks)
                {
                if (_known.TryAdd((task.Id, task.Start), task))
                    {
                    Pending.Enqueue(task);
                }
            }
        }
        /// <summary>Drains tasks currently pending in the connected meter session.</summary>
        /// <returns>Pending device tasks removed from this session's queue.</returns>
        internal IEnumerable<GXTask> Tasks()
        {
            foreach (var task in _known.Values)
                {
                yield return task;
            }
        }
    }

    /// <summary>Claims available server tasks and groups them into executable device work.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Device work containing claimed tasks, or null when no claim can be admitted.</returns>
    private async Task<GXActionBlock?> ClaimPolledTasksAsync(CancellationToken token)
    {
        await _taskRequestGate.WaitAsync(token);
        try
        {
            if (!CanClaimTasks)
                {
                return null;
            }
            var response = await Client.PostAsJson<GetNextTaskResponse>("/api/Task/Next",
                new GetNextTask { AgentId = RuntimeOptions.Id }, token);
            var tasks = response?.Tasks?.ToArray();
            if (tasks == null || tasks.Length == 0)
                {
                return null;
            }
            // A successful claim belongs to this worker even if cancellation arrives now.
            await _taskClaimGate.WaitAsync(CancellationToken.None);
            try
            {
                if (tasks[0].TargetDevice is Guid id && _connectedMeters.TryGetValue(id, out var session) && !session.Closing)
                {
                    session.Add(tasks);
                    return null;
                }
                return new GXActionBlock { Tasks = tasks, NewTask = _newTask };
            }
            finally { _taskClaimGate.Release(); }
        }
        finally { _taskRequestGate.Release(); }
    }

    /// <summary>Registers a connected meter session after safely closing any previous owner.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    /// <param name="session">Task session owned by the connected meter.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Whether the new session remains the current registered owner of the meter.</returns>
    private async Task<bool> RegisterMeterSessionAsync(Guid id, MeterTaskSession session, CancellationToken token)
    {
        while (true)
        {
            Task closing;
            await _taskClaimGate.WaitAsync(token);
            try
            {
                if (!_connectedMeters.TryGetValue(id, out var previous))
                {
                    _connectedMeters.Add(id, session);
                    _runtimeMetrics.ConnectionOpened();
                    return true;
                }
                if (!previous.Closing)
                {
                    previous.Add(session.Tasks());
                    return false;
                }
                closing = previous.Closed.Task;
            }
            finally { _taskClaimGate.Release(); }
            await closing.WaitAsync(token);
        }
    }

    /// <summary>Claims additional server tasks for an already connected meter.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    /// <param name="session">Task session owned by the connected meter.</param>
    /// <param name="action">Queued device work or repository action being processed.</param>
    /// <param name="listener">Whether the session belongs to an inbound listener connection.</param>
    /// <returns>A task representing this operation: Claims additional server tasks for an already connected meter.</returns>
    private async Task CheckConnectedMeterTasksAsync(Guid id, MeterTaskSession session, GXActionBlock action, bool listener)
    {
        await _taskRequestGate.WaitAsync(action.CancellationToken);
        try
        {
            if (!CanClaimTasks)
            {
                await CloseEmptySessionAsync(session);
                return;
            }
            var response = await Client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", new GetNextTask
            {
                AgentId = RuntimeOptions.Id,
                DeviceId = id,
                GatewayId = action.GatewayId,
                Listener = listener,
                WaitTime = 0
            }, action.CancellationToken);
            var tasks = response?.Tasks?.ToArray() ?? [];
            if (tasks.Any(t => t.TargetDevice != id))
                {
                throw new InvalidOperationException("The server returned a task for a different meter on an active connection.");
            }
            await _taskClaimGate.WaitAsync(CancellationToken.None);
            try
            {
                session.Add(tasks);
                if (session.Pending.IsEmpty)
                {
                    session.Closing = true;
                }
            }
            finally { _taskClaimGate.Release(); }
        }
        finally { _taskRequestGate.Release(); }
    }

    /// <summary>Closes a meter session that has no pending work after its final claim check.</summary>
    /// <param name="session">Task session owned by the connected meter.</param>
    /// <returns>A task representing this operation: Closes a meter session that has no pending work after its final claim check.</returns>
    private async Task CloseEmptySessionAsync(MeterTaskSession session)
    {
        await _taskClaimGate.WaitAsync(CancellationToken.None);
        try
        {
            if (session.Pending.IsEmpty)
            {
                session.Closing = true;
            }
        }
        finally { _taskClaimGate.Release(); }
    }

    /// <summary>Stops new task admission and returns work remaining in the meter session.</summary>
    /// <param name="session">Task session owned by the connected meter.</param>
    /// <returns>Tasks remaining after admission to the meter session has stopped.</returns>
    private async Task<GXTask[]> SealMeterSessionAsync(MeterTaskSession session)
    {
        await _taskClaimGate.WaitAsync(CancellationToken.None);
        try
        {
            session.Closing = true;
            return [.. session.Tasks().Where(t => t.Ready == null)];
        }
        finally { _taskClaimGate.Release(); }
    }
    /// <summary>Removes the meter session only if it still owns the registered device identity.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    /// <param name="session">Task session owned by the connected meter.</param>
    /// <returns>A task representing this operation: Removes the meter session only if it still owns the registered device identity.</returns>
    private async Task UnregisterMeterSessionAsync(Guid id, MeterTaskSession session)
    {
        await _taskClaimGate.WaitAsync(CancellationToken.None);
        try
        {
            if (_connectedMeters.TryGetValue(id, out var current) && ReferenceEquals(current, session))
                {
                _connectedMeters.Remove(id);
                _runtimeMetrics.ConnectionClosed();
            }
            session.Closed.TrySetResult();
        }
        finally { _taskClaimGate.Release(); }
    }
}
