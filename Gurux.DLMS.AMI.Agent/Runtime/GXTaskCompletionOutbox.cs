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
    private readonly HttpClient http;
    private readonly string directory;
    private readonly ILogger? logger;
    private readonly TimeSpan retryDelay;
    private readonly GXCompletionOutboxOptions options;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource stop = new();
    private readonly Task retry;
    private readonly Task monitoring;
    private readonly ConcurrentDictionary<string, string> unpersisted = new();
    private readonly Dictionary<string, (int Attempts, DateTimeOffset Next)> schedule = [];
    private readonly object statusGate = new();
    private readonly object disposalGate = new();
    private Task? disposal;
    private int shutdownRequested;
    private string? cursor;
    private GXCompletionOutboxStatus status = new(0, 0, 0, 0, 0, 0, false);
    private DateTimeOffset lastStatusLog;
    private readonly Meter meter = new("Gurux.DLMS.AMI.Agent.Completions", "1.0");

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
        this.http = http;
        this.directory = directory;
        this.logger = logger;
        this.retryDelay = retryDelay;
        this.options = options ?? new();
        this.options.Validate();
        if (retryDelay <= TimeSpan.Zero)
            {
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        }
        GXAgentSettingsStore.EnsurePrivateDirectory(directory);
        meter.CreateObservableGauge("agent.completions.pending", () => Status.PendingCount);
        meter.CreateObservableGauge("agent.completions.pending_bytes", () => Status.PendingBytes, "By");
        meter.CreateObservableGauge("agent.completions.oldest_seconds", () => Status.OldestSeconds, "s");
        meter.CreateObservableGauge("agent.completions.quarantined", () => Status.QuarantinedCount);
        meter.CreateObservableGauge("agent.completions.free_bytes", () => Status.FreeBytes, "By");
        meter.CreateObservableGauge("agent.completions.unpersisted", () => Status.UnpersistedCount);
        meter.CreateObservableGauge("agent.completions.claims_paused", () => Status.ClaimsPaused ? 1 : 0);
        meter.CreateObservableGauge("agent.completions.shutdown_blocked", () => Status.ShutdownBlocked ? 1 : 0);
        UpdateStatus();
        retry = Task.Run(RetryAsync);
        monitoring = Task.Run(MonitorStorageAsync);
    }

    /// <summary>Gets the latest immutable storage snapshot without performing disk or network I/O.</summary>
    internal GXCompletionOutboxStatus Status => Volatile.Read(ref status);
    /// <summary>Gets whether cached storage status permits task claims and no result awaits persistence or shutdown.</summary>
    /// <remarks>This hot-path check performs no disk I/O. External storage changes are normally detected on the next two-second sample.</remarks>
    internal bool CanAcceptTasks => Volatile.Read(ref shutdownRequested) == 0 && !Status.ClaimsPaused && unpersisted.IsEmpty;

    /// <summary>Retains completion payloads and attempts to acknowledge them within the configured flush budget.</summary>
    /// <param name="tasks">Completed tasks whose identifiers, claim start times, results and data must be preserved.</param>
    /// <returns>A task that completes after local retention and the bounded acknowledgement attempt.</returns>
    /// <remarks>
    /// Existing queued or quarantined records for the same task identifier and start time are not overwritten.
    /// Completion does not guarantee server acknowledgement or disk persistence; inspect <see cref="Status"/> for results retained only in memory.
    /// </remarks>
    internal async Task ReportAsync(IEnumerable<GXTask> tasks)
    {
        var reports = tasks.Select(t => new GXTask { Id = t.Id, Start = t.Start, Result = t.Result, Data = t.Data }).ToArray();
        if (reports.Length == 0)
            {
            return;
        }
        await gate.WaitAsync();
        try
        {
            var paths = new List<string>();
            foreach (var report in reports)
            {
                string path = Path.Combine(directory, $"{report.Id:N}-{report.Start?.UtcTicks ?? 0}.json");
                paths.Add(path);
                if (!File.Exists(path) && !File.Exists(QuarantinePath(path)))
                    {
                    unpersisted.TryAdd(path, JsonSerializer.Serialize(new TaskDone { Tasks = [report] }));
                }
            }
            await PersistAsync();
            using var deadline = new CancellationTokenSource(options.FlushBudget);
            try
            { await SendPendingAsync(deadline.Token, paths); }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
            UpdateStatus();
        }
        finally { gate.Release(); }
    }

    private async Task PersistAsync()
    {
        foreach (var item in unpersisted)
        {
            try
            {
                if (!File.Exists(item.Key))
                    {
                    await GXAgentSettingsStore.SaveAsync(item.Key, item.Value, CancellationToken.None);
                }
                unpersisted.TryRemove(item.Key, out _);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { logger?.LogError(error, "Cannot persist task result {Record}; retaining it in memory and pausing new claims", Path.GetFileName(item.Key)); }
        }
    }

    /// <summary>Retries local persistence and eligible queued acknowledgements without executing meter tasks.</summary>
    /// <param name="token">Cancels waiting for queue access and HTTP/file reads; does not cancel persistence of in-memory results.</param>
    /// <returns>A task that completes when this pass finishes; failed acknowledgements remain queued.</returns>
    /// <exception cref="OperationCanceledException">The caller's cancellation interrupts the pass.</exception>
    internal async Task FlushAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        { await PersistAsync(); await SendPendingAsync(token); }
        finally { UpdateStatus(); gate.Release(); }
    }

    private async Task SendPendingAsync(CancellationToken token, IEnumerable<string>? records = null)
    {
        var files = (records ?? Directory.EnumerateFiles(directory, "*.json")).Distinct().Order(StringComparer.Ordinal).ToArray();
        string? startAfter = cursor;
        var ordered = startAfter == null ? files : files.Where(p => string.CompareOrdinal(p, startAfter) > 0).Concat(files.Where(p => string.CompareOrdinal(p, startAfter) <= 0));
        foreach (string path in ordered)
        {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path) || schedule.TryGetValue(path, out var entry) && entry.Next > DateTimeOffset.UtcNow)
                {
                continue;
            }
            // Advance before awaiting: cancellation must let the next flush begin with another record.
            cursor = path;
            try
            {
                TaskDone report;
                try
                {
                    report = JsonSerializer.Deserialize<TaskDone>(await File.ReadAllTextAsync(path, token))
                        ?? throw new InvalidDataException("Empty completion record.");
                    if (report.Tasks == null || !report.Tasks.Any() || report.Tasks.Any(t => t == null || t.Id == Guid.Empty))
                        {
                        throw new InvalidDataException("Invalid completion record.");
                    }
                }
                catch (Exception error) when (error is JsonException or InvalidDataException)
                { Quarantine(path, "invalid local record", error); continue; }
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(options.RequestTimeout);
                using var response = await http.PostAsJsonAsync("/api/task/Done", report, deadline.Token);
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
                    logger?.LogWarning("Task result {Record} remains pending after HTTP {Status}; next attempt {Next}", Path.GetFileName(path), (int)response.StatusCode, schedule[path].Next);
                    continue;
                }
                File.Delete(path);
                schedule.Remove(path);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { Defer(path); throw; }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or IOException or UnauthorizedAccessException)
            {
                Defer(path);
                logger?.LogWarning(error, "Task result {Record} remains pending; only its acknowledgement will be retried", Path.GetFileName(path));
            }
        }
    }

    private void Defer(string path, DateTimeOffset? retryAt = null)
    {
        int attempt = schedule.TryGetValue(path, out var value) ? Math.Min(value.Attempts + 1, 30) : 1;
        DateTimeOffset next = DateTimeOffset.UtcNow + GXRetryBackoff.GetDelay(attempt, retryDelay, TimeSpan.FromMinutes(5));
        if (retryAt > next)
            {
            next = retryAt.Value;
        }
        schedule[path] = (attempt, next);
    }

    private string QuarantinePath(string path) => Path.Combine(directory, "quarantine", Path.GetFileName(path));
    private void Quarantine(string path, string reason, Exception? error = null)
    {
        GXAgentSettingsStore.EnsurePrivateDirectory(Path.Combine(directory, "quarantine"));
        File.Move(path, QuarantinePath(path), overwrite: false);
        schedule.Remove(path);
        logger?.LogError(error, "Task result {Record} quarantined: {Reason}. Result retained for operator recovery; no automatic replay", Path.GetFileName(path), reason);
    }

    private void UpdateStatus()
    {
        lock (statusGate)
            UpdateStatusCore();
    }

    private void UpdateStatusCore()
    {
        var previous = Status;
        GXCompletionOutboxStatus next;
        try
        {
            var files = new DirectoryInfo(directory).GetFiles("*.json");
            var quarantineDir = new DirectoryInfo(Path.Combine(directory, "quarantine"));
            var quarantine = quarantineDir.Exists ? quarantineDir.GetFiles("*.json") : [];
            long bytes = files.Sum(f => f.Length) + unpersisted.Sum(p => (long)Encoding.UTF8.GetByteCount(p.Value));
            string absolute = Path.GetFullPath(directory);
            var drive = DriveInfo.GetDrives().Where(d => absolute == Path.TrimEndingDirectorySeparator(d.Name) ||
                absolute.StartsWith(Path.EndsInDirectorySeparator(d.Name) ? d.Name : d.Name + Path.DirectorySeparatorChar,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .OrderByDescending(d => d.Name.Length).First();
            long free = drive.AvailableFreeSpace;
            string probe = Path.Combine(directory, ".probe-" + Guid.NewGuid().ToString("N"));
            using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
                file.WriteByte(0);
            bool paused = Volatile.Read(ref shutdownRequested) != 0 || !unpersisted.IsEmpty || files.Length + quarantine.Length >= options.MaxPendingRecords ||
                bytes + quarantine.Sum(f => f.Length) >= options.MaxPendingBytes || free < options.MinFreeBytes;
            double age = files.Length == 0 ? 0 : Math.Max(0, (DateTime.UtcNow - files.Min(f => f.CreationTimeUtc)).TotalSeconds);
            next = new(files.Length + unpersisted.Count, bytes, age, quarantine.Length, free, unpersisted.Count, paused);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            next = previous with { ClaimsPaused = true, UnpersistedCount = unpersisted.Count,
                PendingCount = Math.Max(0, previous.PendingCount - previous.UnpersistedCount) + unpersisted.Count };
            if (!previous.ClaimsPaused)
                {
                logger?.LogError(error, "Completion storage is unavailable; new task claims paused");
            }
        }
        next = next with { ShutdownBlocked = Volatile.Read(ref shutdownRequested) != 0 && !unpersisted.IsEmpty };
        Volatile.Write(ref status, next);
        if (next.ShutdownBlocked && !previous.ShutdownBlocked)
            {
            logger?.LogCritical("Shutdown blocked: {Count} task results remain only in memory at {Directory}. Restore free disk space and directory write access; keep this process running until persistence recovers", next.UnpersistedCount, Path.GetFullPath(directory));
        }
        else if (!next.ShutdownBlocked && previous.ShutdownBlocked)
            {
            logger?.LogInformation("Completion storage recovered at {Directory}; all in-memory results are persisted and shutdown resumed", Path.GetFullPath(directory));
        }
        if (next.ClaimsPaused != previous.ClaimsPaused)
            {
            logger?.LogWarning("Completion storage admission changed: paused={Paused}, pending={Pending}, quarantined={Quarantined}, freeBytes={Free}", next.ClaimsPaused, next.PendingCount, next.QuarantinedCount, next.FreeBytes);
        }
        if (DateTimeOffset.UtcNow - lastStatusLog > TimeSpan.FromSeconds(30))
        {
            lastStatusLog = DateTimeOffset.UtcNow;
            logger?.LogInformation("Completion queue: pending={Pending}, bytes={Bytes}, oldestSeconds={Oldest}, quarantined={Quarantined}, freeBytes={Free}, unpersisted={Unpersisted}", next.PendingCount, next.PendingBytes, next.OldestSeconds, next.QuarantinedCount, next.FreeBytes, next.UnpersistedCount);
        }
    }

    private async Task MonitorStorageAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(stop.Token))
                {
                UpdateStatus();
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }

    private async Task RetryAsync()
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                    deadline.CancelAfter(options.FlushBudget);
                    await FlushAsync(deadline.Token);
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { throw; }
                catch (OperationCanceledException) { }
                catch (Exception error) { logger?.LogError(error, "Retrying retained task results failed"); }
                await Task.Delay(retryDelay, stop.Token);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    }

    /// <summary>Stops admission, persists all in-memory results and stops background operations.</summary>
    /// <returns>The shared disposal operation; repeated calls await the same cleanup.</returns>
    /// <remarks>
    /// Storage failure can keep disposal pending until access is restored. Blocked shutdown is exposed through status, logs and a metric.
    /// Persisted but unacknowledged records remain on disk for the next instance. The caller-owned HTTP client is not disposed.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        lock (disposalGate) return new(disposal ??= DisposeCoreAsync());
    }

    private async Task DisposeCoreAsync()
    {
        Interlocked.Exchange(ref shutdownRequested, 1);
        // Keep recovery alive until all outcomes are on disk. A shutdown caller's deadline
        // may expire, but an update must not terminate the process holding the only copy.
        while (true)
        {
            await gate.WaitAsync();
            try { await PersistAsync(); UpdateStatus(); if (unpersisted.IsEmpty) {
                break;
            } }
            finally { gate.Release(); }
            await Task.Delay(retryDelay < TimeSpan.FromSeconds(2) ? retryDelay : TimeSpan.FromSeconds(2));
        }
        await stop.CancelAsync();
        await Task.WhenAll(retry, monitoring);
        meter.Dispose();
        stop.Dispose();
        gate.Dispose();
    }
}
