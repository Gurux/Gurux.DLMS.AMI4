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
    /// <summary>Immutable dialog content and options presented by the shared confirmation host.</summary>
    /// <param name="Id">Identifier assigned to this confirmation request.</param>
    /// <param name="Title">Localized title presented by the confirmation host.</param>
    /// <param name="Message">Localized message presented by the confirmation host.</param>
    /// <param name="Options">Snapshot of the confirmation display and result options.</param>
    public sealed record Request(Guid Id, string Title, string Message, AmiConfirmationOptions Options);
    /// <summary>Queued dialog content paired with the completion awaited by its caller.</summary>
    /// <param name="Request">Dialog content queued for the confirmation host.</param>
    /// <param name="Completion">Completion source resolved when the host responds or detaches.</param>
    private sealed record Pending(Request Request, TaskCompletionSource<AmiConfirmationResult> Completion);
    /// <summary>Protects the confirmation queue and exclusive host attachment.</summary>
    private readonly Lock _gate = new();
    /// <summary>Requests awaiting display or a result in their submission order.</summary>
    private readonly Queue<Pending> _pending = new();
    /// <summary>Whether a dialog host is available to display queued requests.</summary>
    private bool _attached;
    /// <summary>Raised when the visible request or queue changes.</summary>
    public event Action? Changed;
    /// <summary>The first pending request, or null when the queue is empty.</summary>
    public Request? Current { get { lock (_gate) { return _pending.TryPeek(out var item) ? item.Request : null; } } }

    /// <summary>Queues a confirmation and waits for the host&apos;s response, returning rejection when no host is attached.</summary>
    /// <param name="title">Localized dialog title supplied by the caller.</param>
    /// <param name="message">Localized message displayed to the user.</param>
    /// <returns>True when the user confirms the request; otherwise false.</returns>
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        return (await ConfirmAsync(title, message, new AmiConfirmationOptions())).Confirm;
    }

    /// <summary>Queues a confirmation and waits for the host&apos;s response, returning rejection when no host is attached.</summary>
    /// <param name="title">Localized dialog title supplied by the caller.</param>
    /// <param name="message">Localized message displayed to the user.</param>
    /// <param name="options">Dialog display options and any permitted delete selection.</param>
    /// <returns>A task containing the user&apos;s confirmation choice and any allowed delete selection.</returns>
    public Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options)
        => ConfirmAsync(title, message, options, CancellationToken.None);

    /// <summary>Removes the request if the operation that opened it is canceled.</summary>
    /// <param name="title">Localized dialog title supplied by the caller.</param>
    /// <param name="message">Localized message displayed to the user.</param>
    /// <param name="options">Dialog display options and any permitted delete selection.</param>
    /// <param name="cancellationToken">Token that cancels this caller&apos;s operation or wait.</param>
    /// <exception cref="OperationCanceledException">The caller cancels the request or its wait.</exception>
    /// <returns>A task containing the user&apos;s confirmation choice and any allowed delete selection.</returns>
    public Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        Task<AmiConfirmationResult> result;
        var id = Guid.NewGuid();
        lock (_gate)
        {
            if (!_attached)
            {
                return Task.FromResult(new AmiConfirmationResult(false));
            }
            var completion = new TaskCompletionSource<AmiConfirmationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var snapshot = options with { Items = options.Items?.ToArray() };
            _pending.Enqueue(new(new(id, title, message, snapshot), completion));
            result = completion.Task;
        }
        Changed?.Invoke();
        return cancellationToken.CanBeCanceled ? AwaitRequestAsync(result, id, cancellationToken) : result;
    }

    /// <summary>Waits for a queued result and removes the corresponding request if the caller cancels.</summary>
    /// <param name="result">Task completed by the host when the queued request receives a response.</param>
    /// <param name="id">Identifier of the queued confirmation request.</param>
    /// <param name="cancellationToken">Token that cancels this caller&apos;s operation or wait.</param>
    /// <exception cref="OperationCanceledException">The caller cancels the request or its wait.</exception>
    /// <returns>The host&apos;s response when the caller&apos;s wait completes successfully.</returns>
    private async Task<AmiConfirmationResult> AwaitRequestAsync(Task<AmiConfirmationResult> result, Guid id, CancellationToken cancellationToken)
    {
        try { return await result.WaitAsync(cancellationToken); }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                bool removed = false;
                lock (_gate)
                {
                    foreach (var item in _pending.ToArray())
                    {
                        _pending.Dequeue();
                        if (item.Request.Id == id)
                        {
                            removed = true;
                            item.Completion.TrySetResult(new AmiConfirmationResult(false));
                        }
                        else {
                            _pending.Enqueue(item);
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

    /// <summary>Attaches the sole confirmation host for the current browser scope.</summary>
    /// <returns>A registration that rejects pending requests when disposed.</returns>
    /// <exception cref="InvalidOperationException">A confirmation host is already attached.</exception>
    public IDisposable Attach()
    {
        lock (_gate)
        {
            if (_attached)
            {
                throw new InvalidOperationException(Properties.Resources.ConfirmationDialogHostAlreadyAttached);
            }
            _attached = true;
        }
        return new Attachment(this);
    }

    /// <summary>Completes the currently displayed request when its identifier matches and advances the queue.</summary>
    /// <param name="id">Identifier of the queued confirmation request.</param>
    /// <param name="approved">Whether the user approved the displayed request.</param>
    /// <param name="delete">Whether the user selected deletion; ignored unless approved and allowed.</param>
    public void Complete(Guid id, bool approved, bool delete = false)
    {
        Pending item;
        lock (_gate)
        {
            if (!_pending.TryPeek(out item!) || item.Request.Id != id)
            {
                return;
            }
            _pending.Dequeue();
        }
        item.Completion.TrySetResult(new AmiConfirmationResult(approved, approved && item.Request.Options.AllowDelete && delete));
        Changed?.Invoke();
    }

    /// <summary>Removes the host and rejects all requests still waiting in the queue.</summary>
    private void Detach()
    {
        lock (_gate)
        {
            _attached = false;
            while (_pending.TryDequeue(out var item))
            {
                item.Completion.TrySetResult(new AmiConfirmationResult(false));
            }
        }
    }

    /// <summary>Idempotent registration that detaches the confirmation host when disposed.</summary>
    /// <param name="owner">Confirmation service whose host registration is represented.</param>
    private sealed class Attachment(GXConfirmationService owner) : IDisposable
    {
        /// <summary>Host owner retained until this registration is disposed for the first time.</summary>
        private GXConfirmationService? _current = owner;
        /// <summary>Detaches the host once even when the registration is disposed repeatedly.</summary>
        public void Dispose() => Interlocked.Exchange(ref _current, null)?.Detach();
    }
}
