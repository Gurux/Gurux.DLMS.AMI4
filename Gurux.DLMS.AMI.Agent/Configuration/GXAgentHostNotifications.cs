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

using System.Threading.Channels;

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Host-owned asynchronous change signals. Dispose after stopping the worker.</summary>
public sealed class GXAgentHostNotifications : IGXAgentHostNotifications, IDisposable
{
    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly object _gate = new();
    /// <summary>Wakes the consumer when new work or host changes are available.</summary>
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(1);
    /// <summary>Host changes accumulated until consumed by the notification reader.</summary>
    private AgentHostChange _pending;
    /// <summary>Indicates that owned resources have been released.</summary>
    private bool _disposed;

    /// <inheritdoc />
    public void NotifyVersionAvailable() => Notify(AgentHostChange.VersionAvailable);
    /// <inheritdoc />
    public void NotifySettingsChanged() => Notify(AgentHostChange.SettingsChanged);

    /// <inheritdoc />
    public void NotifyAuthenticationRequired() => Notify(AgentHostChange.AuthenticationRequired);

    /// <summary>Accumulates host changes and wakes the single notification consumer.</summary>
    /// <param name="change">Host changes to accumulate for the notification consumer.</param>
    private void Notify(AgentHostChange change)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending |= change;
            _signal.Writer.TryWrite(true);
        }
    }

    /// <inheritdoc />
    public async ValueTask<AgentHostChange> WaitForChangeAsync(CancellationToken cancellationToken = default)
    {
        while (await _signal.Reader.WaitToReadAsync(cancellationToken))
        {
            lock (_gate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_signal.Reader.TryRead(out _))
                {
                    var result = _pending;
                    _pending = AgentHostChange.None;
                    return result;
                }
            }
        }
        throw new ObjectDisposedException(nameof(GXAgentHostNotifications));
    }

    /// <summary>Releases waiting consumers. The host must stop its worker before disposal.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                {
                return;
            }
            _disposed = true;
            _signal.Writer.TryComplete();
        }
    }
}
