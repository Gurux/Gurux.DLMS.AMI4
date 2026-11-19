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

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Retains original completion results; only acknowledgements are retried.</summary>
/// <remarks>
/// Failed persistence retains serialized results in memory and pauses admission. HTTP failures retain persisted records for retry;
/// malformed records and permanent rejections move to quarantine. Storage monitoring runs independently of HTTP retries.
/// The caller owns the HTTP client and must drain result producers before disposing this queue.
/// </remarks>
internal sealed class GXTaskCompletionOutbox : IAsyncDisposable
{
    /// <summary>Versions completion semantics while preserving older queued payloads.</summary>
    private sealed class CompletionReport : TaskDone
    {
        /// <summary>Gets or sets the format version; zero means Result contains legacy errors.</summary>
        public int OutcomeVersion { get; set; }
    }

    /// <summary>Sends requests to the configured AMI server.</summary>
    private readonly HttpClient _http;
    /// <summary>Persistent storage directory for queued delivery records.</summary>
    private readonly string _directory;
    /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
    private readonly ILogger? _logger;
    /// <summary>Delay before retrying a failed server delivery.</summary>
    private readonly TimeSpan _retryDelay;
    /// <summary>Validated limits and settings used by this component.</summary>
    private readonly GXCompletionOutboxOptions _options;
    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Cancels background processing during shutdown.</summary>
    private readonly CancellationTokenSource _stop = new();
    /// <summary>Background task that retries pending completion reports.</summary>
    private readonly Task _retry;
    /// <summary>Background task that updates queue health measurements.</summary>
    private readonly Task _monitoring;
    /// <summary>Serialized completion reports retained in memory after a storage failure.</summary>
    private readonly ConcurrentDictionary<string, string> _unpersisted = new();
    /// <summary>Per-record delivery attempt counts and next retry times.</summary>
    private readonly Dictionary<string, (int Attempts, DateTimeOffset Next)> _schedule = [];
    /// <summary>Protects publication of current status and measurements.</summary>
    private readonly object _statusGate = new();
    /// <summary>Serializes disposal and access to the shared disposal task.</summary>
    private readonly object _disposalGate = new();
    /// <summary>Shared asynchronous disposal operation.</summary>
    private Task? _disposal;
    /// <summary>Atomically records that queue shutdown has been requested.</summary>
    private int _shutdownRequested;
    /// <summary>Last queue entry inspected to keep retry scans fair.</summary>
    private string? _cursor;
    /// <summary>Latest immutable completion queue health snapshot.</summary>
    private GXCompletionOutboxStatus _status = new(0, 0, 0, 0, 0, 0, false);
    /// <summary>Timestamp of the most recent queue health diagnostic.</summary>
    private DateTimeOffset _lastStatusLog;
    /// <summary>Publishes instruments describing this component's runtime activity.</summary>
    private readonly Meter _meter = new("Gurux.DLMS.AMI.Agent.Completions", "1.0");

    /// <summary>Opens private completion storage and starts acknowledgement retry and storage monitoring.</summary>
    /// <param name="http">Caller-owned AMI client with its base address and credentials configured before sending reports.</param>
    /// <param name="directory">Persistent directory for queued JSON records and the quarantine subdirectory.</param>
    /// <param name="logger">Diagnostic logger, or <see langword="null"/> to omit logging.</param>
    /// <param name="retryDelay">Positive interval between retry passes and initial delay for per-record backoff.</param>
    /// <param name="options">Storage and timeout limits, or <see langword="null"/> to use defaults.</param>
    /// <exception cref="ArgumentException">The supplied limits are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryDelay"/> is not positive.</exception>
    internal GXTaskCompletionOutbox(HttpClient http, string directory, ILogger? logger, TimeSpan retryDelay,
        GXCompletionOutboxOptions? options = null)
    {
        this._http = http;
        this._directory = directory;
        this._logger = logger;
        this._retryDelay = retryDelay;
        this._options = options ?? new();
        this._options.Validate();
        if (retryDelay <= TimeSpan.Zero)
            {
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        }
        GXAgentSettingsStore.EnsurePrivateDirectory(directory);
        _meter.CreateObservableGauge("agent.completions.pending", () => Status.PendingCount);
        _meter.CreateObservableGauge("agent.completions.pending_bytes", () => Status.PendingBytes, "By");
        _meter.CreateObservableGauge("agent.completions.oldest_seconds", () => Status.OldestSeconds, "s");
        _meter.CreateObservableGauge("agent.completions.quarantined", () => Status.QuarantinedCount);
        _meter.CreateObservableGauge("agent.completions.free_bytes", () => Status.FreeBytes, "By");
        _meter.CreateObservableGauge("agent.completions.unpersisted", () => Status.UnpersistedCount);
        _meter.CreateObservableGauge("agent.completions.claims_paused", () => Status.ClaimsPaused ? 1 : 0);
        _meter.CreateObservableGauge("agent.completions.shutdown_blocked", () => Status.ShutdownBlocked ? 1 : 0);
        UpdateStatus();
        _retry = Task.Run(RetryAsync);
        _monitoring = Task.Run(MonitorStorageAsync);
    }

    /// <summary>Gets the latest immutable storage snapshot without performing disk or network I/O.</summary>
    internal GXCompletionOutboxStatus Status => Volatile.Read(ref _status);
    /// <summary>Gets whether cached storage status permits task claims and no result awaits persistence or shutdown.</summary>
    /// <remarks>This hot-path check performs no disk I/O. External storage changes are normally detected on the next two-second sample.</remarks>
    internal bool CanAcceptTasks => Volatile.Read(ref _shutdownRequested) == 0 && !Status.ClaimsPaused && _unpersisted.IsEmpty;

    /// <summary>Retains completion payloads and attempts to acknowledge them within the configured flush budget.</summary>
    /// <param name="tasks">Completed tasks whose identifiers, claim start times, results and data must be preserved.</param>
    /// <returns>A task that completes after local retention and the bounded acknowledgement attempt.</returns>
    /// <remarks>
    /// Existing queued or quarantined records for the same task identifier and start time are not overwritten.
    /// Completion does not guarantee server acknowledgement or disk persistence; inspect <see cref="Status"/> for results retained only in memory.
    /// </remarks>
    internal async Task ReportAsync(IEnumerable<GXTask> tasks)
    {
        var reports = tasks.Select(t => new GXTask { Id = t.Id, Start = t.Start, Result = t.Result, Error = t.Error, Data = t.Data }).ToArray();
        if (reports.Length == 0)
            {
            return;
        }
        await _gate.WaitAsync();
        try
        {
            var paths = new List<string>();
            foreach (var report in reports)
            {
                string path = Path.Combine(_directory, $"{report.Id:N}-{report.Start?.UtcTicks ?? 0}.json");
                paths.Add(path);
                if (!File.Exists(path) && !File.Exists(QuarantinePath(path)))
                    {
                    _unpersisted.TryAdd(path, JsonSerializer.Serialize(new CompletionReport { OutcomeVersion = 1, Tasks = [report] }));
                }
            }
            await PersistAsync();
            using var deadline = new CancellationTokenSource(_options.FlushBudget);
            try
            { await SendPendingAsync(deadline.Token, paths); }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
            UpdateStatus();
        }
        finally { _gate.Release(); }
    }

    /// <summary>Retries writing completion reports retained in memory after earlier storage failures.</summary>
    /// <returns>A task representing this operation: Retries writing completion reports retained in memory after earlier storage failures.</returns>
    private async Task PersistAsync()
    {
        foreach (var item in _unpersisted)
        {
            try
            {
                if (!File.Exists(item.Key))
                    {
                    await GXAgentSettingsStore.SaveAsync(item.Key, item.Value, CancellationToken.None);
                }
                _unpersisted.TryRemove(item.Key, out _);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { _logger?.LogError(error, "Cannot persist task result {Record}; retaining it in memory and pausing new claims", Path.GetFileName(item.Key)); }
        }
    }

    /// <summary>Retries local persistence and eligible queued acknowledgements without executing meter tasks.</summary>
    /// <param name="token">Cancels waiting for queue access and HTTP/file reads; does not cancel persistence of in-memory results.</param>
    /// <returns>A task that completes when this pass finishes; failed acknowledgements remain queued.</returns>
    /// <exception cref="OperationCanceledException">The caller's cancellation interrupts the pass.</exception>
    internal async Task FlushAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        { await PersistAsync(); await SendPendingAsync(token); }
        finally { UpdateStatus(); _gate.Release(); }
    }

    /// <summary>Attempts delivery of durable completion reports and retains transient failures for retry.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <param name="records">Optional subset of persisted completion records to deliver.</param>
    /// <returns>A task representing this operation: Attempts delivery of durable completion reports and retains transient failures for retry.</returns>
    private async Task SendPendingAsync(CancellationToken token, IEnumerable<string>? records = null)
    {
        var files = (records ?? Directory.EnumerateFiles(_directory, "*.json")).Distinct().Order(StringComparer.Ordinal).ToArray();
        string? startAfter = _cursor;
        var ordered = startAfter == null ? files : files.Where(p => string.CompareOrdinal(p, startAfter) > 0).Concat(files.Where(p => string.CompareOrdinal(p, startAfter) <= 0));
        foreach (string path in ordered)
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path) || _schedule.TryGetValue(path, out var entry) && entry.Next > DateTimeOffset.UtcNow)
                {
                continue;
            }
            // Advance before awaiting: cancellation must let the next flush begin with another record.
            _cursor = path;
            try
            {
                CompletionReport report;
                try
                {
                    report = JsonSerializer.Deserialize<CompletionReport>(await File.ReadAllTextAsync(path, token))
                        ?? throw new InvalidDataException("Empty completion record.");
                    if (report.Tasks == null || !report.Tasks.Any() || report.Tasks.Any(t => t == null || t.Id == Guid.Empty))
                        {
                        throw new InvalidDataException("Invalid completion record.");
                    }
                }
                catch (Exception error) when (error is JsonException or InvalidDataException)
                { Quarantine(path, "invalid local record", error); continue; }
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(_options.RequestTimeout);
                if (report.OutcomeVersion == 1)
                {
                    using var capability = await _http.GetAsync("/api/task/CompletionFormat", deadline.Token);
                    if (!capability.IsSuccessStatusCode ||
                        (await capability.Content.ReadAsStringAsync(deadline.Token)).Trim() != "1")
                    {
                        Defer(path);
                        _logger?.LogWarning("Task result {Record} remains pending until the server supports separate task errors", Path.GetFileName(path));
                        continue;
                    }
                }
                using var response = await _http.PostAsJsonAsync("/api/task/Done", report, deadline.Token);
                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Gone or HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.UnprocessableEntity)
                { Quarantine(path, $"HTTP {(int)response.StatusCode}"); continue; }
                if (!response.IsSuccessStatusCode)
                {
                    DateTimeOffset? retryAt = response.Headers.RetryAfter?.Date;
                    if (response.Headers.RetryAfter?.Delta is TimeSpan delay)
                        {
                        retryAt = DateTimeOffset.UtcNow + delay;
                    }
                    Defer(path, retryAt);
                    _logger?.LogWarning("Task result {Record} remains pending after HTTP {Status}; next attempt {Next}", Path.GetFileName(path), (int)response.StatusCode, _schedule[path].Next);
                    continue;
                }
                File.Delete(path);
                _schedule.Remove(path);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { Defer(path); throw; }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or IOException or UnauthorizedAccessException)
            {
                Defer(path);
                _logger?.LogWarning(error, "Task result {Record} remains pending; only its acknowledgement will be retried", Path.GetFileName(path));
            }
        }
    }

    /// <summary>Schedules a delivery retry using bounded backoff and an optional server retry time.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <param name="retryAt">Optional server-requested earliest retry time.</param>
    private void Defer(string path, DateTimeOffset? retryAt = null)
    {
        int attempt = _schedule.TryGetValue(path, out var value) ? Math.Min(value.Attempts + 1, 30) : 1;
        DateTimeOffset next = DateTimeOffset.UtcNow + GXRetryBackoff.GetDelay(attempt, _retryDelay, TimeSpan.FromMinutes(5));
        if (retryAt > next)
            {
            next = retryAt.Value;
        }
        _schedule[path] = (attempt, next);
    }

    /// <summary>Resolves the quarantine destination for an invalid completion record.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <returns>The path reserved for the invalid completion record in private quarantine storage.</returns>
    private string QuarantinePath(string path) => Path.Combine(_directory, "quarantine", Path.GetFileName(path));
    /// <summary>Moves permanently invalid delivery data aside and records its failure reason.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <param name="reason">Diagnostic reason for rejecting or quarantining the data.</param>
    /// <param name="error">Failure associated with the current operation, if available.</param>
    private void Quarantine(string path, string reason, Exception? error = null)
    {
        GXAgentSettingsStore.EnsurePrivateDirectory(Path.Combine(_directory, "quarantine"));
        File.Move(path, QuarantinePath(path), overwrite: false);
        _schedule.Remove(path);
        _logger?.LogError(error, "Task result {Record} quarantined: {Reason}. Result retained for operator recovery; no automatic replay", Path.GetFileName(path), reason);
    }

    /// <summary>Refreshes the immutable storage health measurements from current queue state.</summary>
    private void UpdateStatus()
    {
        lock (_statusGate)
            UpdateStatusCore();
    }

    /// <summary>Scans persisted completion records and publishes queue capacity and health measurements.</summary>
    private void UpdateStatusCore()
    {
        var previous = Status;
        GXCompletionOutboxStatus next;
        try
        {
            var files = new DirectoryInfo(_directory).GetFiles("*.json");
            var quarantineDir = new DirectoryInfo(Path.Combine(_directory, "quarantine"));
            var quarantine = quarantineDir.Exists ? quarantineDir.GetFiles("*.json") : [];
            long bytes = files.Sum(f => f.Length) + _unpersisted.Sum(p => (long)Encoding.UTF8.GetByteCount(p.Value));
            string absolute = Path.GetFullPath(_directory);
            var drive = DriveInfo.GetDrives().Where(d => absolute == Path.TrimEndingDirectorySeparator(d.Name) ||
                absolute.StartsWith(Path.EndsInDirectorySeparator(d.Name) ? d.Name : d.Name + Path.DirectorySeparatorChar,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .OrderByDescending(d => d.Name.Length).First();
            long free = drive.AvailableFreeSpace;
            string probe = Path.Combine(_directory, ".probe-" + Guid.NewGuid().ToString("N"));
            using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
                file.WriteByte(0);
            bool paused = Volatile.Read(ref _shutdownRequested) != 0 || !_unpersisted.IsEmpty || files.Length + quarantine.Length >= _options.MaxPendingRecords ||
                bytes + quarantine.Sum(f => f.Length) >= _options.MaxPendingBytes || free < _options.MinFreeBytes;
            double age = files.Length == 0 ? 0 : Math.Max(0, (DateTime.UtcNow - files.Min(f => f.CreationTimeUtc)).TotalSeconds);
            next = new(files.Length + _unpersisted.Count, bytes, age, quarantine.Length, free, _unpersisted.Count, paused);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            next = previous with { ClaimsPaused = true, UnpersistedCount = _unpersisted.Count,
                PendingCount = Math.Max(0, previous.PendingCount - previous.UnpersistedCount) + _unpersisted.Count };
            if (!previous.ClaimsPaused)
                {
                _logger?.LogError(error, "Completion storage is unavailable; new task claims paused");
            }
        }
        next = next with { ShutdownBlocked = Volatile.Read(ref _shutdownRequested) != 0 && !_unpersisted.IsEmpty };
        Volatile.Write(ref _status, next);
        if (next.ShutdownBlocked && !previous.ShutdownBlocked)
            {
            _logger?.LogCritical("Shutdown blocked: {Count} task results remain only in memory at {Directory}. Restore free disk space and directory write access; keep this process running until persistence recovers", next.UnpersistedCount, Path.GetFullPath(_directory));
        }
        else if (!next.ShutdownBlocked && previous.ShutdownBlocked)
            {
            _logger?.LogInformation("Completion storage recovered at {Directory}; all in-memory results are persisted and shutdown resumed", Path.GetFullPath(_directory));
        }
        if (next.ClaimsPaused != previous.ClaimsPaused)
            {
            _logger?.LogWarning("Completion storage admission changed: paused={Paused}, pending={Pending}, quarantined={Quarantined}, freeBytes={Free}", next.ClaimsPaused, next.PendingCount, next.QuarantinedCount, next.FreeBytes);
        }
        if (DateTimeOffset.UtcNow - _lastStatusLog > TimeSpan.FromSeconds(30))
        {
            _lastStatusLog = DateTimeOffset.UtcNow;
            _logger?.LogInformation("Completion queue: pending={Pending}, bytes={Bytes}, oldestSeconds={Oldest}, quarantined={Quarantined}, freeBytes={Free}, unpersisted={Unpersisted}", next.PendingCount, next.PendingBytes, next.OldestSeconds, next.QuarantinedCount, next.FreeBytes, next.UnpersistedCount);
        }
    }

    /// <summary>Refreshes storage capacity independently of HTTP delivery retries.</summary>
    /// <returns>A task representing this operation: Refreshes storage capacity independently of HTTP delivery retries.</returns>
    private async Task MonitorStorageAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token))
                {
                UpdateStatus();
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    /// <summary>Retries durable completion deliveries until queue shutdown is requested.</summary>
    /// <returns>A task representing this operation: Retries durable completion deliveries until queue shutdown is requested.</returns>
    private async Task RetryAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    deadline.CancelAfter(_options.FlushBudget);
                    await FlushAsync(deadline.Token);
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { throw; }
                catch (OperationCanceledException) { }
                catch (Exception error) { _logger?.LogError(error, "Retrying retained task results failed"); }
                await Task.Delay(_retryDelay, _stop.Token);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    /// <summary>Stops admission, persists all in-memory results and stops background operations.</summary>
    /// <returns>The shared disposal operation; repeated calls await the same cleanup.</returns>
    /// <remarks>
    /// Storage failure can keep disposal pending until access is restored. Blocked shutdown is exposed through status, logs and a metric.
    /// Persisted but unacknowledged records remain on disk for the next instance. The caller-owned HTTP client is not disposed.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        lock (_disposalGate) return new(_disposal ??= DisposeCoreAsync());
    }

    /// <summary>Stops queue workers and flushes retained completion reports before releasing resources.</summary>
    /// <returns>A task representing this operation: Stops queue workers and flushes retained completion reports before releasing resources.</returns>
    private async Task DisposeCoreAsync()
    {
        Interlocked.Exchange(ref _shutdownRequested, 1);
        // Keep recovery alive until all outcomes are on disk. A shutdown caller's deadline
        // may expire, but an update must not terminate the process holding the only copy.
        while (true)
        {
            await _gate.WaitAsync();
            try { await PersistAsync(); UpdateStatus(); if (_unpersisted.IsEmpty) {
                break;
            } }
            finally { _gate.Release(); }
            await Task.Delay(_retryDelay < TimeSpan.FromSeconds(2) ? _retryDelay : TimeSpan.FromSeconds(2));
        }
        await _stop.CancelAsync();
        await Task.WhenAll(_retry, _monitoring);
        _meter.Dispose();
        _stop.Dispose();
        _gate.Dispose();
    }
}
