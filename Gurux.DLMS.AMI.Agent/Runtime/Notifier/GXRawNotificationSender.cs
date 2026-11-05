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
using System.Text;
using System.Threading.Channels;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier;

/// <summary>Uploads unparsed transport chunks in order, retrying each with a stable identifier.</summary>
internal sealed class GXRawNotificationSender : IAsyncDisposable
{
    private sealed class Connection
    {
        internal readonly Guid Id = Guid.NewGuid();
        internal long Sequence;
    }
    private readonly HttpClient http;
    private readonly Guid agentId;
    private readonly int interfaceType;
    private readonly ILogger logger;
    private readonly Lock gate = new();
    private readonly Dictionary<string, Connection> connections = new(StringComparer.Ordinal);
    private readonly Channel<GXRawNotification> queue = Channel.CreateBounded<GXRawNotification>(new BoundedChannelOptions(256)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    });
    private readonly CancellationTokenSource stop = new();
    private readonly Task sending;
    private bool completed;

    internal GXRawNotificationSender(HttpClient http, Guid agentId, int interfaceType, ILogger logger)
    {
        this.http = http;
        this.agentId = agentId;
        this.interfaceType = interfaceType;
        this.logger = logger;
        sending = SendAsync(stop.Token);
    }

    /// <summary>Copies bytes without parsing; bounded queue pressure blocks receipt instead of dropping chunks.</summary>
    internal void Receive(string sender, byte[] data, bool datagram)
    {
        lock (gate)
        {
            if (completed)
            {
                return;
            }

            if (datagram)
            {
                Enqueue(sender, new Connection(), data, true);
            }
            else
            {
                if (!connections.TryGetValue(sender, out var connection))
                {
                    connections[sender] = connection = new Connection();
                }

                Enqueue(sender, connection, data, false);
            }
        }
    }

    /// <summary>Marks the end of a stream, including streams with no final data chunk.</summary>
    internal void Close(string sender)
    {
        lock (gate)
        {
            if (!completed && connections.Remove(sender, out var connection))
            {
                Enqueue(sender, connection, [], true);
            }
        }
    }

    private void Enqueue(string sender, Connection connection, byte[] data, bool closed)
    {
        var record = new GXRawNotification
        {
            Id = Guid.NewGuid(),
            Agent = new GXAgent { Id = agentId },
            Sender = Encoding.UTF8.GetBytes(sender),
            Received = DateTimeOffset.UtcNow,
            Interface = interfaceType,
            ConnectionId = connection.Id,
            SequenceNumber = connection.Sequence++,
            ConnectionClosed = closed,
            Value = [.. data]
        };
        queue.Writer.WriteAsync(record, stop.Token).AsTask().GetAwaiter().GetResult();
    }

    private async Task SendAsync(CancellationToken token)
    {
        await foreach (var record in queue.Reader.ReadAllAsync(token))
        {
            int attempts = 0;
            while (true)
            {
                try
                {
                    using var response = await http.PostAsJsonAsync("/api/RawNotification/Add", new AddRawNotification { RawNotifications = [record] }, token);
                    response.EnsureSuccessStatusCode();
                    break;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error) when (error is HttpRequestException or OperationCanceledException)
                {
                    ++attempts;
                    logger.LogWarning(error, "Raw notification {Id} upload failed; retrying without changing its identifier", record.Id);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempts, 30)), token);
                }
            }
        }
    }

    /// <summary>Flushes connection end markers and waits for acknowledged uploads.</summary>
    internal async Task CompleteAsync(CancellationToken token)
    {
        // Cancellation must also release a producer blocked by a full queue during shutdown.
        using var registration = token.Register(stop.Cancel);
        lock (gate)
        {
            if (!completed)
            {
                foreach (var pair in connections)
                {
                    Enqueue(pair.Key, pair.Value, [], true);
                }

                connections.Clear();
                completed = true;
                queue.Writer.TryComplete();
            }
        }
        try
        {
            await sending.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            stop.Cancel();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        stop.Cancel();
        queue.Writer.TryComplete();
        try
        {
            await sending;
        }
        catch (OperationCanceledException)
        {
        }
        stop.Dispose();
    }
}
