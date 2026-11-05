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
using Gurux.Common;

#pragma warning disable CS0067
public sealed class ProbeMedia : IGXMedia, IDisposable
{
    public static ProbeMedia? Last;
    public static Action<ProbeMedia>? Opening;
    public ProbeMedia() => Last = this;
    public int Opens, Closes, Disposals, Receives, Writes;
    private byte[]? reply;
    public int LastWaitTime;
    public bool BlockReceive;
    private readonly ManualResetEventSlim closed = new(false);
    public string Name => "probe";
    public TraceLevel Trace { get; set; }
    public bool IsOpen { get; private set; }
    public string MediaType => typeof(ProbeMedia).FullName!;
    public bool Enabled => true;
    public string Settings { get; set; } = "";
    public object Synchronous { get; } = new();
    public bool IsSynchronous => false;
    public ulong BytesSent => 0;
    public ulong BytesReceived => 0;
    public object Eop { get; set; } = null!;
    public int ConfigurableSettings { get; set; }
    public object Tag { get; set; } = null!;
    public IGXMediaContainer MediaContainer { get; set; } = null!;
    public object SyncRoot => Synchronous;
    public event ReceivedEventHandler? OnReceived;
    public event Gurux.Common.ErrorEventHandler? OnError;
    public event MediaStateChangeEventHandler? OnMediaStateChange;
    public event ClientConnectedEventHandler? OnClientConnected;
    public event ClientDisconnectedEventHandler? OnClientDisconnected;
    public event TraceEventHandler? OnTrace;
    public void Copy(object target) { }
    public void Open() { Opens++; IsOpen = true; Opening?.Invoke(this); }
    public void Close() { Closes++; IsOpen = false; closed.Set(); }
    public void Send(object data, string receiver)
    {
        if (data is byte[] request && request.Length > 10 && request[8] == 0xC1)
        {
            Writes++;
            reply = [0, 1, 0, 1, 0, 16, 0, 4, 0xC5, 1, request[10], 0];
        }
    }
    public bool Receive<T>(ReceiveParameters<T> args)
    {
        Receives++; LastWaitTime = args.WaitTime;
        if (BlockReceive) { closed.Wait(args.WaitTime <= 0 ? Timeout.Infinite : args.WaitTime); return false; }
        if (typeof(T) == typeof(byte[]) && reply != null) { args.Reply = (T)(object)reply; reply = null; }
        return true;
    }
    public void ResetSynchronousBuffer() { }
    public void ResetByteCounters() { }
    public void Validate() { }
    public void Dispose() { Disposals++; closed.Dispose(); }
}
#pragma warning restore CS0067
