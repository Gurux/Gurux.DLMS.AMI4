//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
                return;
            disposed = true;
            signal.Writer.TryComplete();
        }
    }
}
