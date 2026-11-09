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

using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Queues confirmations for the current browser's shared dialog host.</summary>
public sealed class GXConfirmationService : IGXAmiConfirmation
{
    public sealed record Request(Guid Id, string Title, string Message, AmiConfirmationOptions Options);
    private sealed record Pending(Request Request, TaskCompletionSource<AmiConfirmationResult> Completion);
    private readonly Lock gate = new();
    private readonly Queue<Pending> pending = new();
    private bool attached;
    public event Action? Changed;
    public Request? Current { get { lock (gate) { return pending.TryPeek(out var item) ? item.Request : null; } } }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        return (await ConfirmAsync(title, message, new AmiConfirmationOptions())).Confirm;
    }

    public Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options)
        => ConfirmAsync(title, message, options, CancellationToken.None);

    /// <summary>Removes the request if the operation that opened it is canceled.</summary>
    public Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        Task<AmiConfirmationResult> result;
        var id = Guid.NewGuid();
        lock (gate)
        {
            if (!attached)
            {
                return Task.FromResult(new AmiConfirmationResult(false));
            }
            var completion = new TaskCompletionSource<AmiConfirmationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var snapshot = options with { Items = options.Items?.ToArray() };
            pending.Enqueue(new(new(id, title, message, snapshot), completion));
            result = completion.Task;
        }
        Changed?.Invoke();
        return cancellationToken.CanBeCanceled ? AwaitRequestAsync(result, id, cancellationToken) : result;
    }

    private async Task<AmiConfirmationResult> AwaitRequestAsync(Task<AmiConfirmationResult> result, Guid id, CancellationToken cancellationToken)
    {
        try { return await result.WaitAsync(cancellationToken); }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                bool removed = false;
                lock (gate)
                {
                    foreach (var item in pending.ToArray())
                    {
                        pending.Dequeue();
                        if (item.Request.Id == id)
                        {
                            removed = true;
                            item.Completion.TrySetResult(new AmiConfirmationResult(false));
                        }
                        else {
                            pending.Enqueue(item);
                        }
                    }
                }
                if (removed)
                {
                    Changed?.Invoke();
                }
            }
        }
    }

    public IDisposable Attach()
    {
        lock (gate)
        {
            if (attached)
            {
                throw new InvalidOperationException("A confirmation dialog host is already attached.");
            }
            attached = true;
        }
        return new Attachment(this);
    }

    public void Complete(Guid id, bool approved, bool delete = false)
    {
        Pending item;
        lock (gate)
        {
            if (!pending.TryPeek(out item!) || item.Request.Id != id)
            {
                return;
            }
            pending.Dequeue();
        }
        item.Completion.TrySetResult(new AmiConfirmationResult(approved, approved && item.Request.Options.AllowDelete && delete));
        Changed?.Invoke();
    }

    private void Detach()
    {
        lock (gate)
        {
            attached = false;
            while (pending.TryDequeue(out var item))
            {
                item.Completion.TrySetResult(new AmiConfirmationResult(false));
            }
        }
    }

    private sealed class Attachment(GXConfirmationService owner) : IDisposable
    {
        private GXConfirmationService? current = owner;
        public void Dispose() => Interlocked.Exchange(ref current, null)?.Detach();
    }
}
