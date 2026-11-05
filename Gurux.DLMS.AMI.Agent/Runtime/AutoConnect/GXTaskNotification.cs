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

internal sealed class GXTaskNotification : IGXTaskNotification
{
    private readonly Dictionary<Guid, GXTaskRegistration> registrations = [];

    public GXTaskRegistration Register(Guid id)
    {
        lock (registrations)
        {
            if (registrations.Remove(id, out var previous))
                {
                previous.Close();
            }
            var registration = new GXTaskRegistration(this, id);
            registrations.Add(id, registration);
            return registration;
        }
    }

    public void Set(Guid id)
    {
        lock (registrations)
            if (registrations.TryGetValue(id, out var registration))
                {
                registration.Set();
            }
    }

    public void NotifyAll()
    {
        lock (registrations)
            foreach (var registration in registrations.Values)
                {
                registration.Set();
            }
    }

    public void Close()
    {
        lock (registrations)
        {
            foreach (var registration in registrations.Values)
                {
                registration.Close();
            }
            registrations.Clear();
        }
    }

    internal void Unregister(GXTaskRegistration registration)
    {
        lock (registrations)
        {
            if (registrations.TryGetValue(registration.Id, out var current) && ReferenceEquals(current, registration))
                {
                registrations.Remove(registration.Id);
            }
            registration.Close();
        }
    }
}

/// <summary>A connection owns its wake-up queue. Replacing or closing it ends its wait.</summary>
internal sealed class GXTaskRegistration(GXTaskNotification owner, Guid id) : IDisposable
{
    private readonly Channel<bool> signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropWrite, AllowSynchronousContinuations = false });
    internal Guid Id { get; } = id;
    private int closed;
    internal void Set() => signal.Writer.TryWrite(true);
    internal void Close()
    {
        Interlocked.Exchange(ref closed, 1);
        signal.Writer.TryComplete();
    }
    internal async Task<bool> WaitAsync(CancellationToken token)
    {
        if (Volatile.Read(ref closed) != 0)
            {
            return false;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        try
        {
            await signal.Reader.ReadAsync(timeout.Token);
            return Volatile.Read(ref closed) == 0;
        }
        catch (ChannelClosedException) { return false; }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return Volatile.Read(ref closed) == 0; }
    }
    public void Dispose() => owner.Unregister(this);
}
