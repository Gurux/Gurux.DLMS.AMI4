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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Thread-safe typed event delivery with detachable, drainable subscriptions.</summary>
public sealed class AmiEventHub : IGXAmiEvents, IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Type, List<Registration>> subscribers = new();
    private readonly HashSet<Registration> registrations = new();
    private readonly AsyncLocal<Frame?> current = new();
    private readonly Action<Exception>? onError;
    private bool disposed;

    /// <summary>A safe event service for contexts without a runtime event hub.</summary>
    public static IGXAmiEvents None { get; } = new NullEvents();

    /// <summary>Creates a hub with optional isolated error reporting.</summary>
    public AmiEventHub(Action<Exception>? onError = null)
    {
        this.onError = onError;
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var registration = new Registration(this, typeof(T), (value, token) => handler((T)value!, token));
            if (!subscribers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Registration>();
                subscribers.Add(typeof(T), handlers);
            }
            handlers.Add(registration);
            registrations.Add(registration);
            return registration;
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Registration[] snapshot;
        lock (gate)
        {
            snapshot = subscribers.TryGetValue(typeof(T), out var handlers) ? handlers.ToArray() : Array.Empty<Registration>();
        }
        foreach (var registration in snapshot)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Func<object?, CancellationToken, Task>? callback;
            Invocation invocation;
            lock (gate)
            {
                callback = registration.Callback;
                if (callback == null)
                {
                    continue;
                }
                invocation = new Invocation();
                registration.Active.Add(invocation);
            }
            await DeliverAsync(registration, invocation, callback, value, cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task DeliverAsync(Registration registration, Invocation invocation,
        Func<object?, CancellationToken, Task> callback, object? value, CancellationToken token)
    {
        var previous = current.Value;
        current.Value = new Frame(invocation, previous);
        try
        {
            await callback(value, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            try
            {
                onError?.Invoke(exception);
            }
            catch (Exception)
            {
                // Reporting must never affect delivery to other subscribers.
            }
        }
        finally
        {
            current.Value = previous;
            lock (gate)
            {
                registration.Active.Remove(invocation);
                if (registration.Callback == null && registration.Active.Count == 0)
                {
                    registrations.Remove(registration);
                }
                invocation.Completion.TrySetResult();
            }
        }
    }

    private void Detach(Registration registration)
    {
        if (registration.EventType != null)
        {
            var handlers = subscribers[registration.EventType];
            handlers.Remove(registration);
            if (handlers.Count == 0)
            {
                subscribers.Remove(registration.EventType);
            }
            registration.EventType = null;
            registration.Callback = null;
        }
        if (registration.Active.Count == 0)
        {
            registrations.Remove(registration);
        }
    }

    private Task Drain(IEnumerable<Registration> items)
    {
        var self = new HashSet<Invocation>();
        for (var frame = current.Value; frame != null; frame = frame.Parent)
        {
            self.Add(frame.Invocation);
        }
        return Task.WhenAll(items.SelectMany(item => item.Active)
            .Where(invocation => !self.Contains(invocation)).Select(invocation => invocation.Completion.Task).ToArray());
    }

    /// <summary>Detaches all subscribers and drains callbacks already in progress.
    /// When called within a callback, its current invocation chain is excluded to avoid self-deadlock.</summary>
    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            disposed = true;
            var snapshot = registrations.ToArray();
            foreach (var registration in snapshot)
            {
                Detach(registration);
            }
            return new ValueTask(Drain(snapshot));
        }
    }

    private sealed class Registration(AmiEventHub owner, Type eventType, Func<object?, CancellationToken, Task> callback) : IDisposable, IAsyncDisposable
    {
        internal Type? EventType = eventType;
        internal Func<object?, CancellationToken, Task>? Callback = callback;
        internal readonly HashSet<Invocation> Active = new();

        public void Dispose()
        {
            lock (owner.gate)
            {
                owner.Detach(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (owner.gate)
            {
                owner.Detach(this);
                return new ValueTask(owner.Drain(new[] { this }));
            }
        }
    }

    private sealed class Invocation
    {
        internal readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record Frame(Invocation Invocation, Frame? Parent);

    private sealed class NullEvents : IGXAmiEvents, IDisposable, IAsyncDisposable
    {
        public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return this;
        }

        public Task PublishAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
