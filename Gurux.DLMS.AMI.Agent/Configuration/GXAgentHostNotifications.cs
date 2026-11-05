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
    private readonly object gate = new();
    private readonly Channel<bool> signal = Channel.CreateBounded<bool>(1);
    private GXAgentHostChange pending;
    private bool disposed;

    /// <inheritdoc />
    public void NotifyVersionAvailable() => Notify(GXAgentHostChange.VersionAvailable);
    /// <inheritdoc />
    public void NotifySettingsChanged() => Notify(GXAgentHostChange.SettingsChanged);

    /// <inheritdoc />
    public void NotifyAuthenticationRequired() => Notify(GXAgentHostChange.AuthenticationRequired);

    private void Notify(GXAgentHostChange change)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            pending |= change;
            signal.Writer.TryWrite(true);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GXAgentHostChange> WaitForChangeAsync(CancellationToken cancellationToken = default)
    {
        while (await signal.Reader.WaitToReadAsync(cancellationToken))
        {
            lock (gate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(disposed, this);
                if (signal.Reader.TryRead(out _))
                {
                    var result = pending;
                    pending = GXAgentHostChange.None;
                    return result;
                }
            }
        }
        throw new ObjectDisposedException(nameof(GXAgentHostNotifications));
    }

    /// <summary>Releases waiting consumers. The host must stop its worker before disposal.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                {
                return;
            }
            disposed = true;
            signal.Writer.TryComplete();
        }
    }
}
