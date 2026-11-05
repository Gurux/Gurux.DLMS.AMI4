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

using System.Security.Cryptography;
using System.Text;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Process lifetime lock. The empty file stays in place to avoid an unlink/reopen race.</summary>
internal sealed class GXAgentInstanceLock : IDisposable
{
    private Action? release;
    private GXAgentInstanceLock(Action release) => this.release = release;

    internal static GXAgentInstanceLock Acquire(string settingsPath)
    {
        string path = Path.GetFullPath(settingsPath) + ".lock";
        GXAgentSettingsStore.EnsurePrivateDirectory(Path.GetDirectoryName(path)!);
        try
        { var lease = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); return new(lease.Dispose); }
        catch (IOException error) { throw new InvalidOperationException("Cannot acquire agent instance lock; another agent may already use these settings or identity.", error); }
    }

    internal static GXAgentInstanceLock AcquireIdentity(string address, Guid id)
    {
        var uri = new Uri(address);
        string normalized = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);
        string server = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        var ready = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exit = new ManualResetEventSlim();
        // Mutex ownership is thread-affine. A dedicated owner thread lets the async host
        // release the machine-wide lock safely from any continuation thread.
        var owner = new Thread(() =>
        {
            Mutex? mutex = null;
            bool acquired = false;
            try
            {
                mutex = new Mutex(false, "Gurux.AmiAgent." + server + "." + id.ToString("N"),
                    new NamedWaitHandleOptions { CurrentUserOnly = false, CurrentSessionOnly = false });
                try { acquired = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) {
                    throw new InvalidOperationException("Another process already owns this agent identity.");
                }
                ready.TrySetResult(null);
                exit.Wait();
            }
            catch (Exception error) { ready.TrySetResult(error); }
            finally { if (acquired) {
                mutex!.ReleaseMutex();
            } mutex?.Dispose(); }
        }) { IsBackground = true, Name = "Agent identity lock" };
        owner.Start();
        var error = ready.Task.GetAwaiter().GetResult();
        if (error != null)
        {
            owner.Join(); exit.Dispose();
            throw new InvalidOperationException("Cannot acquire machine-wide agent identity lock; another agent may already be running.", error);
        }
        return new(() => { exit.Set(); owner.Join(); exit.Dispose(); });
    }

    public void Dispose() => Interlocked.Exchange(ref release, null)?.Invoke();
}
