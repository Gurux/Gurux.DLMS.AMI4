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

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect;

/// <summary>A connection owns its wake-up queue. Replacing or closing it ends its wait.</summary>
/// <param name="owner">Optional ownership identity used to group connection work.</param>
/// <param name="id">Identity of the agent, device or registered work being processed.</param>
internal sealed class GXTaskRegistration(GXTaskNotification owner, Guid id) : IDisposable
{
    /// <summary>Wakes the consumer when new work or host changes are available.</summary>
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropWrite, AllowSynchronousContinuations = false });
    /// <summary>Gets or sets the identity associated with this registered work or stream.</summary>
    internal Guid Id { get; } = id;
    /// <summary>Atomically marks a registration as closed to prevent duplicate completion.</summary>
    private int _closed;
    /// <summary>Signals task availability for the selected registration.</summary>
    internal void Set() => _signal.Writer.TryWrite(true);
    /// <summary>Closes notification input and wakes consumers so their waits can finish.</summary>
    internal void Close()
    {
        Interlocked.Exchange(ref _closed, 1);
        _signal.Writer.TryComplete();
    }
    /// <summary>Waits for a signal or terminal state within the configured cancellation lifetime.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Whether this registration remains open after a signal or periodic timeout.</returns>
    internal async Task<bool> WaitAsync(CancellationToken token)
    {
        if (Volatile.Read(ref _closed) != 0)
            {
            return false;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        try
        {
            await _signal.Reader.ReadAsync(timeout.Token);
            return Volatile.Read(ref _closed) == 0;
        }
        catch (ChannelClosedException) { return false; }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return Volatile.Read(ref _closed) == 0; }
    }
    /// <inheritdoc/>
    public void Dispose() => owner.Unregister(this);
}
