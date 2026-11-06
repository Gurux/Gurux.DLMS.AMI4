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

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier;

/// <summary>Persists transport chunks before returning to the receiver, then uploads in stream order.</summary>
internal sealed class GXRawNotificationSender : IAsyncDisposable
{
    private sealed class StreamState
    {
        public StreamState() { }
        public Guid Id { get; set; }
        public string Sender { get; set; } = "";
        public long NextSequence { get; set; }
        public bool Closed { get; set; }
        public bool Rejected { get; set; }
        public DateTimeOffset Received { get; set; }
    }

    private sealed class Connection(StreamState state, string path)
    {
        internal readonly StreamState State = state;
        internal string Path = path;
        internal DateTimeOffset RetryAt;
        internal int Attempts;
    }

    private readonly HttpClient http;
    private readonly Guid agentId;
    private readonly int interfaceType;
    private readonly ILogger logger;
    private readonly GXRawDeliveryOptions options;
    private readonly string directory;
    private readonly object gate = new();
    // Storage waits release gate; intake serialization prevents Close/Receive callbacks from overtaking them.
    private readonly object intakeGate = new();
    private readonly Dictionary<string, Connection> active = new(StringComparer.Ordinal);
    private readonly List<Connection> connections = [];
    private readonly CancellationTokenSource stop = new();
    private readonly SemaphoreSlim changed = new(0, 1);
    private readonly Task sending;
    private readonly Meter meter = new("Gurux.DLMS.AMI.Agent.RawDelivery", "1.0");
    private readonly Counter<long> retries;
    private long pendingCount;
    private long pendingBytes;
    private long quarantined;
    private double oldestSeconds;
    private bool completed;
    private bool intakeStopped;

    internal bool IsIntakeStopped => Volatile.Read(ref intakeStopped);

    /// <summary>Releases storage-blocked receivers before their transport waits for callbacks to finish.</summary>
    internal void StopIntake()
    {
        lock (gate)
        {
            Volatile.Write(ref intakeStopped, true);
            Monitor.PulseAll(gate);
        }
    }

    internal GXRawNotificationSender(HttpClient http, Guid agentId, int interfaceType, ILogger logger,
        string? directory = null, GXRawDeliveryOptions? options = null)
    {
        this.http = http;
        this.agentId = agentId;
        this.interfaceType = interfaceType;
        this.logger = logger;
        this.options = options ?? new();
        this.options.Validate();
        this.directory = directory ?? DefaultDirectory(http.BaseAddress, agentId, interfaceType);
        GXAgentSettingsStore.EnsurePrivateDirectory(this.directory);
        Recover();
        meter.CreateObservableGauge("agent.raw.pending", () => Interlocked.Read(ref pendingCount));
        meter.CreateObservableGauge("agent.raw.pending_bytes", () => Interlocked.Read(ref pendingBytes), "By");
        meter.CreateObservableGauge("agent.raw.quarantined", () => Interlocked.Read(ref quarantined));
        meter.CreateObservableGauge("agent.raw.oldest_seconds", () => Volatile.Read(ref oldestSeconds), "s");
        retries = meter.CreateCounter<long>("agent.raw.retries");
        UpdateStatus();
        sending = Task.Run(() => SendAsync(stop.Token));
    }

    internal static string DefaultDirectory(Uri? server, Guid agent, int transport)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(home))
        {
            home = AppContext.BaseDirectory;
        }
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server?.AbsoluteUri ?? "")))[..16].ToLowerInvariant();
        return Path.Combine(home, "Gurux", "DLMS.AMI", "raw-notifications", hash, agent.ToString("N"), transport.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Returns only after private local storage retains the received bytes; storage pressure blocks intake.</summary>
    internal void Receive(string sender, byte[] data, bool datagram)
    {
        lock (intakeGate)
        {
            lock (gate)
            {
                if (completed || intakeStopped || stop.IsCancellationRequested)
                {
                    return;
                }
                if (!datagram && data.Length == 0)
                {
                    return;
                }
                // Reserve capacity before creating a stream, so a blocked intake cannot leave empty stream directories.
                WaitForCapacity(data.Length * 2L + Encoding.UTF8.GetByteCount(sender) * 2L + 2048);
                Connection connection;
                if (datagram || !active.TryGetValue(sender, out connection!))
                {
                    var state = new StreamState { Id = Guid.NewGuid(), Sender = sender, Received = DateTimeOffset.UtcNow };
                    connection = new Connection(state, Path.Combine(directory, state.Id.ToString("N")));
                    GXAgentSettingsStore.EnsurePrivateDirectory(connection.Path);
                    SaveState(connection);
                    connections.Add(connection);
                    if (!datagram)
                    {
                        active.Add(sender, connection);
                    }
                }
                Append(connection, data, datagram);
                Pulse();
            }
        }
    }

    internal void Close(string sender)
    {
        lock (intakeGate)
        {
            lock (gate)
            {
                if (!completed && active.TryGetValue(sender, out var connection))
                {
                    Append(connection, [], true);
                    active.Remove(sender);
                    Pulse();
                }
            }
        }
    }

    private void WaitForCapacity(long additionalBytes)
    {
        if (additionalBytes > options.MaxPendingBytes)
        {
            throw new IOException("A raw notification exceeds the configured durable queue byte limit.");
        }
        while (true)
        {
            stop.Token.ThrowIfCancellationRequested();
            if (completed || intakeStopped)
            {
                throw new OperationCanceledException("Raw delivery is stopping.");
            }
            UpdateStatus();
            string absolute = Path.GetFullPath(directory);
            var drive = DriveInfo.GetDrives().Where(d => absolute == Path.TrimEndingDirectorySeparator(d.Name) ||
                absolute.StartsWith(Path.EndsInDirectorySeparator(d.Name) ? d.Name : d.Name + Path.DirectorySeparatorChar,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .OrderByDescending(d => d.Name.Length).First();
            if (pendingCount + quarantined < options.MaxPendingRecords && pendingBytes + additionalBytes < options.MaxPendingBytes &&
                drive.AvailableFreeSpace >= options.MinFreeBytes + additionalBytes)
            {
                return;
            }
            Monitor.Wait(gate, TimeSpan.FromMilliseconds(200));
        }
    }

    private void Append(Connection connection, byte[] data, bool closed)
    {
        var record = new GXRawNotification
        {
            Id = Guid.NewGuid(), Agent = new GXAgent { Id = agentId },
            Sender = Encoding.UTF8.GetBytes(connection.State.Sender), Received = DateTimeOffset.UtcNow,
            Interface = interfaceType, ConnectionId = connection.State.Id,
            SequenceNumber = connection.State.NextSequence, ConnectionClosed = closed, Value = [.. data]
        };
        Save(Path.Combine(connection.Path, $"{record.SequenceNumber:D20}.json"), JsonSerializer.Serialize(record));
        // The chunk is authoritative if a crash interrupts the following state replacement.
        ++connection.State.NextSequence;
        connection.State.Closed = closed;
        SaveState(connection);
        UpdateStatus();
    }

    private static void Save(string path, string json)
    {
        GXAgentSettingsStore.SaveAsync(path, json, CancellationToken.None).GetAwaiter().GetResult();
        // Ensure the completed write has reached the OS's durable storage boundary before returning to the receiver.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        stream.Flush(flushToDisk: true);
    }

    private static void SaveState(Connection connection) => Save(Path.Combine(connection.Path, "state.json"), JsonSerializer.Serialize(connection.State));

    private void Recover()
    {
        foreach (var path in Directory.EnumerateDirectories(directory).Where(p => Path.GetFileName(p) != "quarantine"))
        {
            try
            {
                var state = JsonSerializer.Deserialize<StreamState>(File.ReadAllText(Path.Combine(path, "state.json")))
                    ?? throw new InvalidDataException("Empty stream state.");
                if (state.Id == Guid.Empty || Path.GetFileName(path) != state.Id.ToString("N") || state.NextSequence < 0)
                {
                    throw new InvalidDataException("Invalid raw stream state.");
                }
                var connection = new Connection(state, path);
                var paths = RecordFiles(connection);
                var records = paths.Select(p => JsonSerializer.Deserialize<GXRawNotification>(File.ReadAllText(p))
                    ?? throw new InvalidDataException("Empty raw record.")).ToArray();
                if (records.Any(r => r.ConnectionId != state.Id || r.Agent?.Id != agentId || r.Interface != interfaceType || r.SequenceNumber < 0 ||
                    r.Id == Guid.Empty || r.Received == null || r.Sender == null || !r.Sender.SequenceEqual(Encoding.UTF8.GetBytes(state.Sender))))
                {
                    throw new InvalidDataException("Mismatched raw stream record.");
                }
                for (int index = 0; index < records.Length; ++index)
                {
                    var record = records[index];
                    if (Path.GetFileName(paths[index]) != $"{record.SequenceNumber:D20}.json" ||
                        record.SequenceNumber == long.MaxValue ||
                        index > 0 && record.SequenceNumber != records[index - 1].SequenceNumber + 1 ||
                        index < records.Length - 1 && record.ConnectionClosed ||
                        !record.ConnectionClosed && record.Value?.Length is not > 0)
                    {
                        throw new InvalidDataException("Gapped, out-of-order or malformed raw stream.");
                    }
                }
                if (records.Length > 0)
                {
                    if (state.NextSequence > records[^1].SequenceNumber + 1)
                    {
                        throw new InvalidDataException("The tail of the pending stream is missing.");
                    }
                    state.NextSequence = Math.Max(state.NextSequence, records.Max(r => r.SequenceNumber) + 1);
                    state.Closed |= records.Any(r => r.ConnectionClosed);
                }
                if (state.Rejected)
                {
                    QuarantineDirectory(path, "Recovering an interrupted quarantine move.");
                    continue;
                }
                if (!state.Closed && !state.Rejected && state.NextSequence > 0)
                {
                    Append(connection, [], true);
                }
                else if (state.NextSequence == 0)
                {
                    Directory.Delete(path, true);
                    continue;
                }
                connections.Add(connection);
            }
            catch (Exception error) when (error is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            {
                QuarantineDirectory(path, "Invalid local stream: " + error.Message);
            }
        }
    }

    private static string[] RecordFiles(Connection connection) => Directory.GetFiles(connection.Path, "*.json")
        .Where(p => Path.GetFileName(p) != "state.json").Order(StringComparer.Ordinal).ToArray();

    private async Task SendAsync(CancellationToken token)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    Connection[] streams;
                    lock (gate)
                    {
                        streams = connections.Where(c => !c.State.Rejected).ToArray();
                        if (completed && streams.Length == 0)
                        {
                            return;
                        }
                    }
                    bool progressed = false;
                    foreach (var connection in streams)
                    {
                        if (connection.RetryAt > DateTimeOffset.UtcNow)
                        {
                            continue;
                        }
                        string? path;
                        GXRawNotification? record;
                        lock (gate)
                        {
                            path = RecordFiles(connection).FirstOrDefault();
                            if (path == null)
                            {
                                if (connection.State.Closed)
                                {
                                    Directory.Delete(connection.Path, true);
                                    connections.Remove(connection);
                                    Monitor.PulseAll(gate);
                                    progressed = true;
                                }
                                continue;
                            }
                            try
                            {
                                record = JsonSerializer.Deserialize<GXRawNotification>(File.ReadAllText(path))
                                    ?? throw new InvalidDataException("Empty raw notification.");
                            }
                            catch (Exception error) when (error is JsonException or InvalidDataException)
                            {
                                Quarantine(connection, "Invalid local record: " + error.Message);
                                progressed = true;
                                continue;
                            }
                        }
                        try
                        {
                            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                            deadline.CancelAfter(options.RequestTimeout);
                            using var response = await http.PostAsJsonAsync("/api/RawNotification/Add", new AddRawNotification { RawNotifications = [record] }, deadline.Token);
                            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Gone or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnprocessableEntity)
                            {
                                lock (gate)
                                {
                                    Quarantine(connection, $"HTTP {(int)response.StatusCode}");
                                }
                            }
                            else if (!response.IsSuccessStatusCode)
                            {
                                var retryAt = response.Headers.RetryAfter?.Date;
                                if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
                                {
                                    retryAt = DateTimeOffset.UtcNow + delta;
                                }
                                Defer(connection, retryAt);
                                logger.LogWarning("Raw stream {Connection} upload returned HTTP {Status}; retained for retry", connection.State.Id, (int)response.StatusCode);
                                continue;
                            }
                            else
                            {
                                lock (gate)
                                {
                                    File.Delete(path);
                                    connection.Attempts = 0;
                                    Monitor.PulseAll(gate);
                                }
                            }
                            progressed = true;
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or IOException or UnauthorizedAccessException)
                        {
                            Defer(connection);
                            logger.LogWarning(error, "Raw stream {Connection} upload failed; original chunk retained durably", connection.State.Id);
                        }
                    }
                    lock (gate)
                    {
                        UpdateStatus();
                    }
                    if (!progressed)
                    {
                        await changed.WaitAsync(TimeSpan.FromMilliseconds(200), token);
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(error, "Raw delivery storage is unavailable; retained records will be retried after storage recovery");
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private void Defer(Connection connection, DateTimeOffset? retryAt = null)
    {
        connection.Attempts = Math.Min(connection.Attempts + 1, 30);
        connection.RetryAt = DateTimeOffset.UtcNow + GXRetryBackoff.GetDelay(connection.Attempts, options.RetryDelay, TimeSpan.FromSeconds(30));
        if (retryAt > connection.RetryAt)
        {
            connection.RetryAt = retryAt.Value;
        }
        retries.Add(1);
    }

    private void Quarantine(Connection connection, string reason)
    {
        // Mark before moving: a failed move must never make this stream eligible for later chunks.
        connection.State.Rejected = true;
        SaveState(connection);
        connection.Path = QuarantineDirectory(connection.Path, reason);
        UpdateStatus();
        Monitor.PulseAll(gate);
    }

    private string QuarantineDirectory(string path, string reason)
    {
        var quarantine = Path.Combine(directory, "quarantine");
        GXAgentSettingsStore.EnsurePrivateDirectory(quarantine);
        var destination = Path.Combine(quarantine, Path.GetFileName(path));
        Directory.Move(path, destination);
        try
        {
            Save(Path.Combine(destination, "reason.txt"), reason);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Quarantined stream reason could not be persisted; stream remains retained at {Directory}", destination);
        }
        logger.LogError("Raw stream {Stream} quarantined: {Reason}; no subsequent chunks will be uploaded", Path.GetFileName(path), reason);
        return destination;
    }

    private void UpdateStatus()
    {
        var files = new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories);
        var quarantine = Path.Combine(directory, "quarantine") + Path.DirectorySeparatorChar;
        var records = files.Where(f => f.Extension == ".json" && f.Name != "state.json").ToArray();
        Interlocked.Exchange(ref pendingCount, records.LongCount(f => !f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)));
        Interlocked.Exchange(ref quarantined, records.LongCount(f => f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)));
        Interlocked.Exchange(ref pendingBytes, files.Sum(f => f.Length));
        var pending = records.Where(f => !f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)).ToArray();
        Volatile.Write(ref oldestSeconds, pending.Length == 0 ? 0 : Math.Max(0, (DateTime.UtcNow - pending.Min(f => f.CreationTimeUtc)).TotalSeconds));
    }

    private void Pulse()
    {
        if (changed.CurrentCount == 0)
        {
            changed.Release();
        }
    }

    /// <summary>Persists stream ends and gives delivery a bounded flush window; unacknowledged files survive shutdown.</summary>
    internal async Task CompleteAsync(CancellationToken token)
    {
        StopIntake();
        lock (gate)
        {
            if (!completed)
            {
                foreach (var connection in active.Values)
                {
                    Append(connection, [], true);
                }
                active.Clear();
                completed = true;
                Pulse();
            }
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(options.ShutdownFlushBudget);
        try
        {
            await sending.WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            stop.Cancel();
            await sending;
            logger.LogWarning("Raw delivery flush budget elapsed; pending streams retained at {Directory}", directory);
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopIntake();
        stop.Cancel();
        lock (gate)
        {
            completed = true;
            Monitor.PulseAll(gate);
        }
        await sending;
        meter.Dispose();
        // Receivers are detached before disposal by the owning service.
        changed.Dispose();
        stop.Dispose();
    }
}
