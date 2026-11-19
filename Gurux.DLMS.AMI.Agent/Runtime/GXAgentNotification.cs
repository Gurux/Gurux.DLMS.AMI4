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
    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly Lock _gate = new();
    /// <summary>Subscriptions grouped by their notification identifier.</summary>
    private readonly Dictionary<string, List<Action>> _handlers = new(StringComparer.Ordinal);

    /// <summary>Subscribes to one update type. Dispose the result when its service stops.</summary>
    /// <param name="type">Protocol data type or notification category processed by this operation.</param>
    /// <param name="handler">Callback invoked when the subscribed notification is published.</param>
    /// <returns>A subscription whose disposal removes the callback.</returns>
    internal IDisposable On(string type, Action handler)
    {
        lock (_gate)
        {
            if (!_handlers.TryGetValue(type, out var listeners))
            {
                _handlers[type] = listeners = [];
            }

            listeners.Add(handler);
        }
        return new Subscription(() =>
        {
            lock (_gate)
            {
                if (_handlers.TryGetValue(type, out var listeners))
                {
                    listeners.Remove(handler);
                    if (listeners.Count == 0)
                    {
                        _handlers.Remove(type);
                    }
                }
            }
        });
    }

    /// <summary>Wakes each event type once per server batch, including mixed task and raw events.</summary>
    /// <param name="updates">Identifiers whose subscribers must be notified of changed data.</param>
    internal void Publish(IEnumerable<GXTarget> updates)
    {
        foreach (var type in updates.Select(n => n.Type).Distinct(StringComparer.Ordinal))
        {
            Action[] listeners;
            lock (_gate)
            {
                listeners = _handlers.TryGetValue(type, out var values) ? [.. values] : [];
            }

            foreach (var listener in listeners)
            {
                listener();
            }
        }
    }

    /// <summary>Removes a notification callback when its owner releases the subscription.</summary>
    /// <param name="remove">Callback that unregisters the notification subscription.</param>
    private sealed class Subscription(Action remove) : IDisposable
    {
        /// <summary>Removes this subscription when disposal first occurs.</summary>
        private Action? _dispose = remove;
        /// <inheritdoc/>
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
