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
    private readonly HttpClient http;
    private readonly GXAgentNotification notification;
    private readonly Guid processor;
    private readonly int workers;
    private readonly Func<GXRawNotification, CancellationToken, Task> process;
    private readonly ILogger logger;
    private readonly TimeSpan leaseDuration;
    private readonly Action<GXRawNotification>? connectionFinished;
    private readonly Func<bool>? canProcess;
    private readonly SemaphoreSlim wake;
    private readonly CancellationTokenSource stop = new();
    private IDisposable? subscription;
    private Task[] running = [];

    internal GXRawNotificationHandler(HttpClient http, GXAgentNotification notification, Guid processor, int workers,
        Func<GXRawNotification, CancellationToken, Task> process, ILogger logger, TimeSpan? leaseDuration = null,
        Action<GXRawNotification>? connectionFinished = null, Func<bool>? canProcess = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workers, 1);

        this.http = http;
        this.notification = notification;
        this.processor = processor;
        this.workers = workers;
        this.process = process;
        this.logger = logger;
        this.leaseDuration = leaseDuration ?? TimeSpan.FromSeconds(60);
        this.connectionFinished = connectionFinished;
        this.canProcess = canProcess;
        wake = new SemaphoreSlim(0, workers);
    }

    internal Task StartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        subscription = notification.On("raw-notification.add", Signal);
        running = [.. Enumerable.Range(0, workers).Select(_ => RunAsync(stop.Token))
, RecoverMissedEventsAsync(stop.Token)];
        Signal(); // Recover work queued before the subscription was established.
        return Task.CompletedTask;
    }

    /// <summary>Also called after reconnect to recover events missed while the connection was down.</summary>
    internal void Signal()
    {
        if (stop.IsCancellationRequested)
        {
            return;
        }

        try
        {
            wake.Release();
        }
        catch (SemaphoreFullException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

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

    private async Task<T> PostAsync<T>(string route, object request, CancellationToken token)
    {
        using var response = await http.PostAsJsonAsync("/api/RawNotification/" + route, request, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token)
            ?? throw new HttpRequestException("The raw notification server returned an empty response.");
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await wake.WaitAsync(token);
                while (!token.IsCancellationRequested)
                {
                    while (canProcess?.Invoke() == false)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                    var reply = await PostAsync<GetNextRawNotificationResponse>("Next", new GetNextRawNotification { ProcessorId = processor, LeaseDuration = leaseDuration }, token);
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
                logger.LogError(error, "Raw notification worker failed; retrying after a connection error");
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
    private async Task ProcessConnectionAsync(GXRawNotification[] chunks, CancellationToken token)
    {
        var first = chunks.OrderBy(n => n.SequenceNumber).First();
        var leaseToken = first.LeaseToken ?? throw new InvalidOperationException("The server did not return a lease token.");
        using var session = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var renewed = new CancellationTokenSource();
        using var changed = new SemaphoreSlim(0, 1);
        using var updates = notification.On("raw-notification.add", () =>
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
                    await process(chunk, session.Token);
                    ++next;
                    if (chunk.ConnectionClosed)
                    { closed = true; break; }
                }
                if (!closed)
                {
                    await changed.WaitAsync(TimeSpan.FromSeconds(1), session.Token);
                    chunks = [.. (await PostAsync<GetLeasedRawNotificationsResponse>("Leased", new GetLeasedRawNotifications {
                        ProcessorId = processor, LeaseToken = leaseToken
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
                ProcessorId = processor,
                LeaseToken = leaseToken
            }, session.Token);
        }
        catch (Exception error)
        {
            renewed.Cancel();
            await renew;
            logger.LogError(error, "Raw notification connection {Connection} failed", first.ConnectionId);
            // Use a separate short timeout so shutdown can release work for another processor.
            using var reportTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                await PostAsync<RawNotificationFailedResponse>("Fail", new RawNotificationFailed { ProcessorId = processor, LeaseToken = leaseToken, Error = leaseError?.Message ?? error.Message, NextAttempt = error is InvalidDataException || first.Attempts >= 5 ? null : DateTimeOffset.UtcNow.AddSeconds(10) }, reportTimeout.Token);
            }
            catch (Exception reportError)
            {
                logger.LogWarning(reportError, "Could not release raw notification lease {Lease}; it will expire", leaseToken);
            }
        }
        finally
        {
            session.Cancel();
            renewed.Cancel();
            await renew;
            connectionFinished?.Invoke(first);
        }

        async Task KeepAliveAsync()
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, renewed.Token);
            try
            {
                while (!linked.IsCancellationRequested)
                {
                    await Task.Delay(leaseDuration / 3, linked.Token);
                    await PostAsync<RenewRawNotificationLeaseResponse>("RenewLease", new RenewRawNotificationLease { ProcessorId = processor, LeaseToken = leaseToken, LeaseDuration = leaseDuration }, linked.Token);
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

    internal async Task StopAsync(CancellationToken token)
    {
        subscription?.Dispose();
        subscription = null;
        stop.Cancel();
        await Task.WhenAll(running).WaitAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        stop.Dispose();
        wake.Dispose();
    }
}
