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
    /// <summary>Persists raw connection identity, ordering and terminal delivery state.</summary>
    private sealed class StreamState
    {
        /// <summary>Persists raw connection identity, ordering and terminal delivery state.</summary>
        public StreamState() { }
        /// <summary>Gets or sets the identity associated with this registered work or stream.</summary>
        public Guid Id { get; set; }
        /// <summary>Gets or sets the transport identity associated with the raw connection.</summary>
        public string Sender { get; set; } = "";
        /// <summary>Gets or sets the sequence assigned to the next raw record.</summary>
        public long NextSequence { get; set; }
        /// <summary>Gets or sets whether the raw stream has reached its terminal record.</summary>
        public bool Closed { get; set; }
        /// <summary>Gets or sets whether the raw stream was rejected and cannot accept more records.</summary>
        public bool Rejected { get; set; }
        /// <summary>Gets or sets the timestamp of the most recently accepted raw data.</summary>
        public DateTimeOffset Received { get; set; }
    }

    /// <summary>Owns connection cancellation, cleanup and completion tracking.</summary>
    /// <param name="state">Persisted identity and sequencing state shared by a raw connection.</param>
    /// <param name="path">File or directory on which the operation acts.</param>
    private sealed class Connection(StreamState state, string path)
    {
        /// <summary>Stream state shared by the connection and its persisted records.</summary>
        internal readonly StreamState State = state;
        /// <summary>Persistent queue file containing this connection's delivery record.</summary>
        internal string Path = path;
        /// <summary>Earliest time at which this delivery record may be retried.</summary>
        internal DateTimeOffset RetryAt;
        /// <summary>Number of failed delivery attempts for this record.</summary>
        internal int Attempts;
    }

    /// <summary>Sends requests to the configured AMI server.</summary>
    private readonly HttpClient _http;
    /// <summary>Agent identifier included in raw notification delivery records.</summary>
    private readonly Guid _agentId;
    /// <summary>Protocol interface identifier included in delivery records.</summary>
    private readonly int _interfaceType;
    /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
    private readonly ILogger _logger;
    /// <summary>Validated limits and settings used by this component.</summary>
    private readonly GXRawDeliveryOptions _options;
    /// <summary>Persistent storage directory for queued delivery records.</summary>
    private readonly string _directory;
    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly object _gate = new();
    // Storage waits release gate; intake serialization prevents Close/Receive callbacks from overtaking them.
    /// <summary>Serializes acceptance of new raw notification bytes.</summary>
    private readonly object _intakeGate = new();
    /// <summary>Active raw streams indexed by transport connection identity.</summary>
    private readonly Dictionary<string, Connection> _active = new(StringComparer.Ordinal);
    /// <summary>Tracks active connections owned by this component.</summary>
    private readonly List<Connection> _connections = [];
    /// <summary>Cancels background processing during shutdown.</summary>
    private readonly CancellationTokenSource _stop = new();
    /// <summary>Wakes delivery processing when persisted queue contents change.</summary>
    private readonly SemaphoreSlim _changed = new(0, 1);
    /// <summary>Background task that delivers persisted raw notification records.</summary>
    private readonly Task _sending;
    /// <summary>Publishes instruments describing this component's runtime activity.</summary>
    private readonly Meter _meter = new("Gurux.DLMS.AMI.Agent.RawDelivery", "1.0");
    /// <summary>Counts failed deliveries scheduled for retry.</summary>
    private readonly Counter<long> _retries;
    /// <summary>Number of records waiting for server acknowledgement.</summary>
    private long _pendingCount;
    /// <summary>Total size of records waiting for server acknowledgement.</summary>
    private long _pendingBytes;
    /// <summary>Number of invalid queue records moved aside for inspection.</summary>
    private long _quarantined;
    /// <summary>Age in seconds of the oldest pending delivery record.</summary>
    private double _oldestSeconds;
    /// <summary>Tracks completion of work or input for this component.</summary>
    private bool _completed;
    /// <summary>Indicates that the component no longer accepts incoming data.</summary>
    private bool _intakeStopped;

    /// <summary>Gets whether raw input has stopped accepting new delivery records.</summary>
    internal bool IsIntakeStopped => Volatile.Read(ref _intakeStopped);

    /// <summary>Releases storage-blocked receivers before their transport waits for callbacks to finish.</summary>
    internal void StopIntake()
    {
        lock (_gate)
        {
            Volatile.Write(ref _intakeStopped, true);
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Restores private raw delivery storage and starts its background sender.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    /// <param name="agentId">Registered agent identity attached to delivered raw records.</param>
    /// <param name="interfaceType">DLMS framing interface used by the notification transport.</param>
    /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
    /// <param name="directory">Expected parent directory used to constrain resolved paths.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    internal GXRawNotificationSender(HttpClient http, Guid agentId, int interfaceType, ILogger logger,
        string? directory = null, GXRawDeliveryOptions? options = null)
    {
        this._http = http;
        this._agentId = agentId;
        this._interfaceType = interfaceType;
        this._logger = logger;
        this._options = options ?? new();
        this._options.Validate();
        this._directory = directory ?? DefaultDirectory(http.BaseAddress, agentId, interfaceType);
        GXAgentSettingsStore.EnsurePrivateDirectory(this._directory);
        Recover();
        _meter.CreateObservableGauge("agent.raw.pending", () => Interlocked.Read(ref _pendingCount));
        _meter.CreateObservableGauge("agent.raw.pending_bytes", () => Interlocked.Read(ref _pendingBytes), "By");
        _meter.CreateObservableGauge("agent.raw.quarantined", () => Interlocked.Read(ref _quarantined));
        _meter.CreateObservableGauge("agent.raw.oldest_seconds", () => Volatile.Read(ref _oldestSeconds), "s");
        _retries = _meter.CreateCounter<long>("agent.raw.retries");
        UpdateStatus();
        _sending = Task.Run(() => SendAsync(_stop.Token));
    }

    /// <summary>Derives a private raw delivery directory from server, agent and transport identity.</summary>
    /// <param name="server">AMI server address used to isolate queue storage.</param>
    /// <param name="agent">Server agent registration used to update local configuration.</param>
    /// <param name="transport">HTTP transport used by worker requests.</param>
    /// <returns>A private queue path isolated by server, agent and protocol transport identity.</returns>
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
    /// <param name="sender">Transport identity or event source that produced the notification.</param>
    /// <param name="data">Serialized task value decoded using the protocol attribute type.</param>
    /// <param name="datagram">Whether the accepted bytes form one complete datagram stream.</param>
    internal void Receive(string sender, byte[] data, bool datagram)
    {
        lock (_intakeGate)
        {
            lock (_gate)
            {
                if (_completed || _intakeStopped || _stop.IsCancellationRequested)
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
                if (datagram || !_active.TryGetValue(sender, out connection!))
                {
                    var state = new StreamState { Id = Guid.NewGuid(), Sender = sender, Received = DateTimeOffset.UtcNow };
                    connection = new Connection(state, Path.Combine(_directory, state.Id.ToString("N")));
                    GXAgentSettingsStore.EnsurePrivateDirectory(connection.Path);
                    SaveState(connection);
                    _connections.Add(connection);
                    if (!datagram)
                    {
                        _active.Add(sender, connection);
                    }
                }
                Append(connection, data, datagram);
                Pulse();
            }
        }
    }

    /// <summary>Closes notification input and wakes consumers so their waits can finish.</summary>
    /// <param name="sender">Transport identity or event source that produced the notification.</param>
    internal void Close(string sender)
    {
        lock (_intakeGate)
        {
            lock (_gate)
            {
                if (!_completed && _active.TryGetValue(sender, out var connection))
                {
                    Append(connection, [], true);
                    _active.Remove(sender);
                    Pulse();
                }
            }
        }
    }

    /// <summary>Pauses input until durable queue limits permit the additional raw bytes.</summary>
    /// <param name="additionalBytes">Size of the raw data awaiting admission to durable storage.</param>
    private void WaitForCapacity(long additionalBytes)
    {
        if (additionalBytes > _options.MaxPendingBytes)
        {
            throw new IOException("A raw notification exceeds the configured durable queue byte limit.");
        }
        while (true)
        {
            _stop.Token.ThrowIfCancellationRequested();
            if (_completed || _intakeStopped)
            {
                throw new OperationCanceledException("Raw delivery is stopping.");
            }
            UpdateStatus();
            string absolute = Path.GetFullPath(_directory);
            var drive = DriveInfo.GetDrives().Where(d => absolute == Path.TrimEndingDirectorySeparator(d.Name) ||
                absolute.StartsWith(Path.EndsInDirectorySeparator(d.Name) ? d.Name : d.Name + Path.DirectorySeparatorChar,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .OrderByDescending(d => d.Name.Length).First();
            if (_pendingCount + _quarantined < _options.MaxPendingRecords && _pendingBytes + additionalBytes < _options.MaxPendingBytes &&
                drive.AvailableFreeSpace >= _options.MinFreeBytes + additionalBytes)
            {
                return;
            }
            Monitor.Wait(_gate, TimeSpan.FromMilliseconds(200));
        }
    }

    /// <summary>Persists the next ordered raw stream record before advancing connection state.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    /// <param name="data">Serialized task value decoded using the protocol attribute type.</param>
    /// <param name="closed">Whether this is the terminal raw record for the stream.</param>
    private void Append(Connection connection, byte[] data, bool closed)
    {
        var record = new GXRawNotification
        {
            Id = Guid.NewGuid(), Agent = new GXAgent { Id = _agentId },
            Sender = Encoding.UTF8.GetBytes(connection.State.Sender), Received = DateTimeOffset.UtcNow,
            Interface = _interfaceType, ConnectionId = connection.State.Id,
            SequenceNumber = connection.State.NextSequence, ConnectionClosed = closed, Value = [.. data]
        };
        Save(Path.Combine(connection.Path, $"{record.SequenceNumber:D20}.json"), JsonSerializer.Serialize(record));
        // The chunk is authoritative if a crash interrupts the following state replacement.
        ++connection.State.NextSequence;
        connection.State.Closed = closed;
        SaveState(connection);
        UpdateStatus();
    }

    /// <summary>Atomically persists raw queue JSON and flushes it before publication.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <param name="json">Serialized release manifest containing the signed package identity.</param>
    private static void Save(string path, string json)
    {
        GXAgentSettingsStore.SaveAsync(path, json, CancellationToken.None).GetAwaiter().GetResult();
        // Ensure the completed write has reached the OS's durable storage boundary before returning to the receiver.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>Persists the current raw stream identity and sequence state.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    private static void SaveState(Connection connection) => Save(Path.Combine(connection.Path, "state.json"), JsonSerializer.Serialize(connection.State));

    /// <summary>Restores persisted raw stream state and quarantines unreadable records.</summary>
    private void Recover()
    {
        foreach (var path in Directory.EnumerateDirectories(_directory).Where(p => Path.GetFileName(p) != "quarantine"))
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
                if (records.Any(r => r.ConnectionId != state.Id || r.Agent?.Id != _agentId || r.Interface != _interfaceType || r.SequenceNumber < 0 ||
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
                _connections.Add(connection);
            }
            catch (Exception error) when (error is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            {
                QuarantineDirectory(path, "Invalid local stream: " + error.Message);
            }
        }
    }

    /// <summary>Enumerates ordered persisted records belonging to the raw connection.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    /// <returns>Persisted raw record paths ordered by stream sequence.</returns>
    private static string[] RecordFiles(Connection connection) => [.. Directory.GetFiles(connection.Path, "*.json")
        .Where(p => Path.GetFileName(p) != "state.json").Order(StringComparer.Ordinal)];

    /// <summary>Delivers persisted raw records in connection order while retrying transient failures.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Delivers persisted raw records in connection order while retrying transient failures.</returns>
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
                    lock (_gate)
                    {
                        streams = [.. _connections.Where(c => !c.State.Rejected)];
                        if (_completed && streams.Length == 0)
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
                        lock (_gate)
                        {
                            path = RecordFiles(connection).FirstOrDefault();
                            if (path == null)
                            {
                                if (connection.State.Closed)
                                {
                                    Directory.Delete(connection.Path, true);
                                    _connections.Remove(connection);
                                    Monitor.PulseAll(_gate);
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
                            deadline.CancelAfter(_options.RequestTimeout);
                            using var response = await _http.PostAsJsonAsync("/api/RawNotification/Add", new AddRawNotification { RawNotifications = [record] }, deadline.Token);
                            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Gone or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnprocessableEntity)
                            {
                                lock (_gate)
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
                                _logger.LogWarning("Raw stream {Connection} upload returned HTTP {Status}; retained for retry", connection.State.Id, (int)response.StatusCode);
                                continue;
                            }
                            else
                            {
                                lock (_gate)
                                {
                                    File.Delete(path);
                                    connection.Attempts = 0;
                                    Monitor.PulseAll(_gate);
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
                            _logger.LogWarning(error, "Raw stream {Connection} upload failed; original chunk retained durably", connection.State.Id);
                        }
                    }
                    lock (_gate)
                    {
                        UpdateStatus();
                    }
                    if (!progressed)
                    {
                        await _changed.WaitAsync(TimeSpan.FromMilliseconds(200), token);
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    _logger.LogError(error, "Raw delivery storage is unavailable; retained records will be retried after storage recovery");
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    /// <summary>Schedules a delivery retry using bounded backoff and an optional server retry time.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    /// <param name="retryAt">Optional server-requested earliest retry time.</param>
    private void Defer(Connection connection, DateTimeOffset? retryAt = null)
    {
        connection.Attempts = Math.Min(connection.Attempts + 1, 30);
        connection.RetryAt = DateTimeOffset.UtcNow + GXRetryBackoff.GetDelay(connection.Attempts, _options.RetryDelay, TimeSpan.FromSeconds(30));
        if (retryAt > connection.RetryAt)
        {
            connection.RetryAt = retryAt.Value;
        }
        _retries.Add(1);
    }

    /// <summary>Moves permanently invalid delivery data aside and records its failure reason.</summary>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    /// <param name="reason">Diagnostic reason for rejecting or quarantining the data.</param>
    private void Quarantine(Connection connection, string reason)
    {
        // Mark before moving: a failed move must never make this stream eligible for later chunks.
        connection.State.Rejected = true;
        SaveState(connection);
        connection.Path = QuarantineDirectory(connection.Path, reason);
        UpdateStatus();
        Monitor.PulseAll(_gate);
    }

    /// <summary>Moves an unreadable raw stream directory aside for diagnostic inspection.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <param name="reason">Diagnostic reason for rejecting or quarantining the data.</param>
    /// <returns>The destination path containing the quarantined raw stream directory.</returns>
    private string QuarantineDirectory(string path, string reason)
    {
        var quarantine = Path.Combine(_directory, "quarantine");
        GXAgentSettingsStore.EnsurePrivateDirectory(quarantine);
        var destination = Path.Combine(quarantine, Path.GetFileName(path));
        Directory.Move(path, destination);
        try
        {
            Save(Path.Combine(destination, "reason.txt"), reason);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(error, "Quarantined stream reason could not be persisted; stream remains retained at {Directory}", destination);
        }
        _logger.LogError("Raw stream {Stream} quarantined: {Reason}; no subsequent chunks will be uploaded", Path.GetFileName(path), reason);
        return destination;
    }

    /// <summary>Refreshes the immutable storage health measurements from current queue state.</summary>
    private void UpdateStatus()
    {
        var files = new DirectoryInfo(_directory).GetFiles("*", SearchOption.AllDirectories);
        var quarantine = Path.Combine(_directory, "quarantine") + Path.DirectorySeparatorChar;
        var records = files.Where(f => f.Extension == ".json" && f.Name != "state.json").ToArray();
        Interlocked.Exchange(ref _pendingCount, records.LongCount(f => !f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)));
        Interlocked.Exchange(ref _quarantined, records.LongCount(f => f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)));
        Interlocked.Exchange(ref _pendingBytes, files.Sum(f => f.Length));
        var pending = records.Where(f => !f.FullName.StartsWith(quarantine, StringComparison.OrdinalIgnoreCase)).ToArray();
        Volatile.Write(ref _oldestSeconds, pending.Length == 0 ? 0 : Math.Max(0, (DateTime.UtcNow - pending.Min(f => f.CreationTimeUtc)).TotalSeconds));
    }

    /// <summary>Wakes the background sender after durable queue contents change.</summary>
    private void Pulse()
    {
        if (_changed.CurrentCount == 0)
        {
            _changed.Release();
        }
    }

    /// <summary>Persists stream ends and gives delivery a bounded flush window; unacknowledged files survive shutdown.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task completing after pending raw delivery is flushed within its shutdown budget.</returns>
    internal async Task CompleteAsync(CancellationToken token)
    {
        StopIntake();
        lock (_gate)
        {
            if (!_completed)
            {
                foreach (var connection in _active.Values)
                {
                    Append(connection, [], true);
                }
                _active.Clear();
                _completed = true;
                Pulse();
            }
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(_options.ShutdownFlushBudget);
        try
        {
            await _sending.WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            _stop.Cancel();
            await _sending;
            _logger.LogWarning("Raw delivery flush budget elapsed; pending streams retained at {Directory}", _directory);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        StopIntake();
        _stop.Cancel();
        lock (_gate)
        {
            _completed = true;
            Monitor.PulseAll(_gate);
        }
        await _sending;
        _meter.Dispose();
        // Receivers are detached before disposal by the owning service.
        _changed.Dispose();
        _stop.Dispose();
    }
}
