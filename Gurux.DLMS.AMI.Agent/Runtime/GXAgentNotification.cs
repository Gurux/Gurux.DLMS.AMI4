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

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Routes server update types to disposable agent event subscriptions.</summary>
internal sealed class GXAgentNotification
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, List<Action>> handlers = new(StringComparer.Ordinal);

    /// <summary>Subscribes to one update type. Dispose the result when its service stops.</summary>
    internal IDisposable On(string type, Action handler)
    {
        lock (gate)
        {
            if (!handlers.TryGetValue(type, out var listeners))
            {
                handlers[type] = listeners = [];
            }

            listeners.Add(handler);
        }
        return new Subscription(() =>
        {
            lock (gate)
            {
                if (handlers.TryGetValue(type, out var listeners))
                {
                    listeners.Remove(handler);
                    if (listeners.Count == 0)
                    {
                        handlers.Remove(type);
                    }
                }
            }
        });
    }

    /// <summary>Wakes each event type once per server batch, including mixed task and raw events.</summary>
    internal void Publish(IEnumerable<GXTarget> updates)
    {
        foreach (var type in updates.Select(n => n.Type).Distinct(StringComparer.Ordinal))
        {
            Action[] listeners;
            lock (gate)
            {
                listeners = handlers.TryGetValue(type, out var values) ? [.. values] : [];
            }

            foreach (var listener in listeners)
            {
                listener();
            }
        }
    }

    private sealed class Subscription(Action remove) : IDisposable
    {
        private Action? dispose = remove;
        public void Dispose() => Interlocked.Exchange(ref dispose, null)?.Invoke();
    }
}
