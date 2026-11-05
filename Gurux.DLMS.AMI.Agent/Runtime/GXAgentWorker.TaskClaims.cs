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

public partial class GXAgentWorker
{
    private readonly SemaphoreSlim taskClaimGate = new(1, 1);
    private readonly Dictionary<Guid, MeterTaskSession> connectedMeters = [];

    private sealed class MeterTaskSession
    {
        private readonly ConcurrentDictionary<(Guid, DateTimeOffset?), GXTask> known = new();
        internal readonly ConcurrentQueue<GXTask> Pending = new();
        internal readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Closing;
        internal void Add(IEnumerable<GXTask> tasks)
        {
            foreach (var task in tasks)
                {
                if (known.TryAdd((task.Id, task.Start), task))
                    {
                    Pending.Enqueue(task);
                }
            }
        }
        internal IEnumerable<GXTask> Tasks()
        {
            foreach (var task in known.Values)
                {
                yield return task;
            }
        }
    }

    private async Task<GXActionBlock?> ClaimPolledTasksAsync(CancellationToken token)
    {
        await taskClaimGate.WaitAsync(token);
        try
        {
            if (!CanClaimTasks)
                {
                return null;
            }
            var response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next",
                new GetNextTask { AgentId = RuntimeOptions.Id }, token);
            var tasks = response?.Tasks?.ToArray();
            if (tasks == null || tasks.Length == 0)
                {
                return null;
            }
            if (tasks[0].TargetDevice is Guid id && connectedMeters.TryGetValue(id, out var session) && !session.Closing)
            {
                session.Add(tasks);
                return null;
            }
            return new GXActionBlock { Tasks = tasks, NewTask = _newTask };
        }
        finally { taskClaimGate.Release(); }
    }

    private async Task<bool> RegisterMeterSessionAsync(Guid id, MeterTaskSession session, CancellationToken token)
    {
        while (true)
        {
            Task closing;
            await taskClaimGate.WaitAsync(token);
            try
            {
                if (!connectedMeters.TryGetValue(id, out var previous))
                {
                    connectedMeters.Add(id, session);
                    return true;
                }
                if (!previous.Closing)
                {
                    previous.Add(session.Tasks());
                    return false;
                }
                closing = previous.Closed.Task;
            }
            finally { taskClaimGate.Release(); }
            await closing.WaitAsync(token);
        }
    }

    private async Task CheckConnectedMeterTasksAsync(Guid id, MeterTaskSession session, GXActionBlock action, bool listener)
    {
        await taskClaimGate.WaitAsync(action.CancellationToken);
        try
        {
            if (!CanClaimTasks)
            {
                if (session.Pending.IsEmpty)
                    {
                    session.Closing = true;
                }
                return;
            }
            var response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", new GetNextTask
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
            session.Add(tasks);
            if (session.Pending.IsEmpty)
                {
                session.Closing = true;
            }
        }
        finally { taskClaimGate.Release(); }
    }

    private async Task<GXTask[]> SealMeterSessionAsync(MeterTaskSession session)
    {
        await taskClaimGate.WaitAsync();
        try
        {
            session.Closing = true;
            return session.Tasks().Where(t => t.Ready == null).ToArray();
        }
        finally { taskClaimGate.Release(); }
    }
    private async Task UnregisterMeterSessionAsync(Guid id, MeterTaskSession session)
    {
        await taskClaimGate.WaitAsync();
        try
        {
            if (connectedMeters.TryGetValue(id, out var current) && ReferenceEquals(current, session))
                {
                connectedMeters.Remove(id);
            }
            session.Closed.TrySetResult();
        }
        finally { taskClaimGate.Release(); }
    }
}
