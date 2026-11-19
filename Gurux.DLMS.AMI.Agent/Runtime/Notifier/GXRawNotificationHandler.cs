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

using System.Net.Http.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier;

/// <summary>Claims raw connections on notifications, maintains leases and acknowledges awaited parser work.</summary>
internal sealed class GXRawNotificationHandler : IAsyncDisposable
{
    /// <summary>Sends requests to the configured AMI server.</summary>
    private readonly HttpClient _http;
    /// <summary>Publishes in-process signals when raw notification work changes.</summary>
    private readonly GXAgentNotification _notification;
    /// <summary>Identifies this process when acquiring server-side notification leases.</summary>
    private readonly Guid _processor;
    /// <summary>Number of concurrent raw notification consumers.</summary>
    private readonly int _workers;
    /// <summary>Processes a leased raw notification within its cancellation lifetime.</summary>
    private readonly Func<GXRawNotification, CancellationToken, Task> _process;
    /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
    private readonly ILogger _logger;
    /// <summary>Period for which a leased notification remains owned by this processor.</summary>
    private readonly TimeSpan _leaseDuration;
    /// <summary>Callback invoked when a raw notification stream finishes.</summary>
    private readonly Action<GXRawNotification>? _connectionFinished;
    /// <summary>Reports whether the worker is currently allowed to claim notifications.</summary>
    private readonly Func<bool>? _canProcess;
    /// <summary>Wakes processors when notifications become available.</summary>
    private readonly SemaphoreSlim _wake;
    /// <summary>Cancels background processing during shutdown.</summary>
    private readonly CancellationTokenSource _stop = new();
    /// <summary>Subscription forwarding available-work notifications to the processor.</summary>
    private IDisposable? _subscription;
    /// <summary>Background processor tasks owned by the handler.</summary>
    private Task[] _running = [];

    /// <summary>Configures leased raw notification consumers and their bounded processing lifetime.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    /// <param name="notification">Publishes in-process raw notification work signals.</param>
    /// <param name="processor">Unique identity used to own server-side notification leases.</param>
    /// <param name="workers">Maximum number of parallel raw notification consumers.</param>
    /// <param name="process">Processes leased raw data within the supplied cancellation lifetime.</param>
    /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
    /// <param name="leaseDuration">Duration for which a claimed raw connection remains owned by this processor.</param>
    /// <param name="connectionFinished">Callback releasing stream resources after all connection records finish.</param>
    /// <param name="canProcess">Reports whether runtime state currently permits new notification claims.</param>
    internal GXRawNotificationHandler(HttpClient http, GXAgentNotification notification, Guid processor, int workers,
        Func<GXRawNotification, CancellationToken, Task> process, ILogger logger, TimeSpan? leaseDuration = null,
        Action<GXRawNotification>? connectionFinished = null, Func<bool>? canProcess = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workers, 1);

        this._http = http;
        this._notification = notification;
        this._processor = processor;
        this._workers = workers;
        this._process = process;
        this._logger = logger;
        this._leaseDuration = leaseDuration ?? TimeSpan.FromSeconds(60);
        this._connectionFinished = connectionFinished;
        this._canProcess = canProcess;
        _wake = new SemaphoreSlim(0, workers);
    }

    /// <summary>Starts the configured background notification consumers and recovery timer.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Starts the configured background notification consumers and recovery timer.</returns>
    internal Task StartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _subscription = _notification.On("raw-notification.add", Signal);
        _running = [.. Enumerable.Range(0, _workers).Select(_ => RunAsync(_stop.Token))
, RecoverMissedEventsAsync(_stop.Token)];
        Signal(); // Recover work queued before the subscription was established.
        return Task.CompletedTask;
    }

    /// <summary>Also called after reconnect to recover events missed while the connection was down.</summary>
    internal void Signal()
    {
        if (_stop.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _wake.Release();
        }
        catch (SemaphoreFullException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>Periodically wakes notification consumers to recover signals missed during disconnection.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Periodically wakes notification consumers to recover signals missed during disconnection.</returns>
    private async Task RecoverMissedEventsAsync(CancellationToken token)
    {
        try
        {
            // One recovery poll for the entire handler, rather than one per idle worker.
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), token);
                Signal();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    /// <summary>Posts a raw notification workflow request and decodes its typed response.</summary>
    /// <param name="route">Relative server endpoint receiving the workflow request.</param>
    /// <param name="request">Request object sent to the AMI server.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The typed server response decoded from the workflow acknowledgement.</returns>
    /// <typeparam name="T">Decoded server response type.</typeparam>
    private async Task<T> PostAsync<T>(string route, object request, CancellationToken token)
    {
        using var response = await _http.PostAsJsonAsync("/api/RawNotification/" + route, request, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token)
            ?? throw new HttpRequestException("The raw notification server returned an empty response.");
    }

    /// <summary>Claims raw connection leases and processes their ordered chunks until shutdown.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Claims raw connection leases and processes their ordered chunks until shutdown.</returns>
    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _wake.WaitAsync(token);
                while (!token.IsCancellationRequested)
                {
                    while (_canProcess?.Invoke() == false)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                    var reply = await PostAsync<GetNextRawNotificationResponse>("Next", new GetNextRawNotification { ProcessorId = _processor, LeaseDuration = _leaseDuration }, token);
                    var chunks = reply.RawNotifications.ToArray();
                    if (chunks.Length == 0)
                    {
                        break;
                    }

                    Signal();
                    await ProcessConnectionAsync(chunks, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Raw notification worker failed; retrying after a connection error");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    Signal();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
    /// <summary>Processes ordered raw chunks under a renewable connection lease and reports their outcome.</summary>
    /// <param name="chunks">Ordered raw records claimed for a single connection lease.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Processes ordered raw chunks under a renewable connection lease and reports their outcome.</returns>
    private async Task ProcessConnectionAsync(GXRawNotification[] chunks, CancellationToken token)
    {
        var first = chunks.OrderBy(n => n.SequenceNumber).First();
        var leaseToken = first.LeaseToken ?? throw new InvalidOperationException("The server did not return a lease token.");
        using var session = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var renewed = new CancellationTokenSource();
        using var changed = new SemaphoreSlim(0, 1);
        using var updates = _notification.On("raw-notification.add", () =>
        {
            try
            {
                changed.Release();
            }
            catch (SemaphoreFullException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        });
        Exception? leaseError = null;
        var renew = KeepAliveAsync();
        try
        {
            long next = 0;
            bool closed = false;
            while (!closed)
            {
                foreach (var chunk in chunks.OrderBy(n => n.SequenceNumber))
                {
                    if (chunk.SequenceNumber < next)
                    {
                        continue; // Leased returns previously retrieved chunks too.
                    }

                    if (chunk.ConnectionId != first.ConnectionId || chunk.LeaseToken != leaseToken || chunk.SequenceNumber != next)
                    {
                        throw new InvalidDataException("The raw connection contains a gap or mismatched lease.");
                    }
                    // Do not wait for TCP closure: a script may already have a complete message.
                    // Its per-lease load context retains parser state across successive chunks.
                    await _process(chunk, session.Token);
                    ++next;
                    if (chunk.ConnectionClosed)
                    { closed = true; break; }
                }
                if (!closed)
                {
                    await changed.WaitAsync(TimeSpan.FromSeconds(1), session.Token);
                    chunks = [.. (await PostAsync<GetLeasedRawNotificationsResponse>("Leased", new GetLeasedRawNotifications {
                        ProcessorId = _processor, LeaseToken = leaseToken
                    }, session.Token)).RawNotifications];
                }
            }
            renewed.Cancel();
            await renew;
            if (leaseError != null)
            {
                throw new InvalidOperationException("Connection lease renewal failed.", leaseError);
            }

            session.Token.ThrowIfCancellationRequested();
            await PostAsync<RawNotificationDoneResponse>("Done", new RawNotificationDone
            {
                ProcessorId = _processor,
                LeaseToken = leaseToken
            }, session.Token);
        }
        catch (Exception error)
        {
            renewed.Cancel();
            await renew;
            _logger.LogError(error, "Raw notification connection {Connection} failed", first.ConnectionId);
            // Use a separate short timeout so shutdown can release work for another processor.
            using var reportTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                await PostAsync<RawNotificationFailedResponse>("Fail", new RawNotificationFailed { ProcessorId = _processor, LeaseToken = leaseToken, Error = leaseError?.Message ?? error.Message, NextAttempt = error is InvalidDataException || first.Attempts >= 5 ? null : DateTimeOffset.UtcNow.AddSeconds(10) }, reportTimeout.Token);
            }
            catch (Exception reportError)
            {
                _logger.LogWarning(reportError, "Could not release raw notification lease {Lease}; it will expire", leaseToken);
            }
        }
        finally
        {
            session.Cancel();
            renewed.Cancel();
            await renew;
            _connectionFinished?.Invoke(first);
        }

        async Task KeepAliveAsync()
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, renewed.Token);
            try
            {
                while (!linked.IsCancellationRequested)
                {
                    await Task.Delay(_leaseDuration / 3, linked.Token);
                    await PostAsync<RenewRawNotificationLeaseResponse>("RenewLease", new RenewRawNotificationLease { ProcessorId = _processor, LeaseToken = leaseToken, LeaseDuration = _leaseDuration }, linked.Token);
                }
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                leaseError = error;
                session.Cancel();
            }
        }
    }

    /// <summary>Stops accepting work and waits for admitted operations to finish.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Stops accepting work and waits for admitted operations to finish.</returns>
    internal async Task StopAsync(CancellationToken token)
    {
        _subscription?.Dispose();
        _subscription = null;
        _stop.Cancel();
        await Task.WhenAll(_running).WaitAsync(token);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _stop.Dispose();
        _wake.Dispose();
    }
}
