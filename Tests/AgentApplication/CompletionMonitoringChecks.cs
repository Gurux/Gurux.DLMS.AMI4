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

using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;

internal static class CompletionMonitoringChecks
{
    internal static async Task RunAsync()
    {
        await AdmissionUsesSnapshotAsync();
        await BlockedShutdownIsObservableAsync();
    }

    // Catches synchronous scans in CanAcceptTasks and status refresh coupled to a slow HTTP request.
    private static async Task AdmissionUsesSnapshotAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "completion-monitor-" + Guid.NewGuid().ToString("N"));
        string queue = Path.Combine(root, "queue"), saved = Path.Combine(root, "saved");
        Directory.CreateDirectory(queue);
        var report = new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow, Data = "retained" };
        File.WriteAllText(Path.Combine(queue, "result.json"), JsonSerializer.Serialize(new TaskDone { Tasks = [report] }));
        var sending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler(async (_, token) =>
        {
            sending.TrySetResult();
            await release.Task.WaitAsync(token);
            return new(HttpStatusCode.ServiceUnavailable);
        })) { BaseAddress = new("https://completion.test") };
        await using var outbox = new GXTaskCompletionOutbox(http, queue, null, TimeSpan.FromHours(1),
            new() { MinFreeBytes = 0, RequestTimeout = TimeSpan.FromSeconds(30), FlushBudget = TimeSpan.FromSeconds(30) });
        try
        {
            await sending.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Directory.Move(queue, saved);
            File.WriteAllText(queue, "storage unavailable");
            // The snapshot was healthy: querying admission must not touch the now-broken disk path.
            for (int i = 0; i < 100; ++i)
                {
                if (!outbox.CanAcceptTasks) {
                    throw new Exception("Admission inspected storage instead of reading the cached snapshot.");
                }
            }
            await UntilAsync(() => outbox.Status.ClaimsPaused);
            if (release.Task.IsCompleted) {
                throw new Exception("Storage detection waited for HTTP completion.");
            }
            File.Delete(queue);
            Directory.Move(saved, queue);
            await UntilAsync(() => !outbox.Status.ClaimsPaused);
            if (!outbox.CanAcceptTasks || !File.Exists(Path.Combine(queue, "result.json")))
                {
                throw new Exception("Background storage recovery did not resume admission with the result intact.");
            }
            Console.WriteLine("PASS cached admission and independent storage monitoring during blocked HTTP");
        }
        finally
        {
            if (File.Exists(queue)) {
                File.Delete(queue);
            }
            if (Directory.Exists(saved)) {
                Directory.Move(saved, queue);
            }
            release.TrySetResult();
            await outbox.DisposeAsync();
            Directory.Delete(root, true);
        }
    }

    // Catches silent shutdown stalls, premature completion and loss of the original result on disk failure.
    private static async Task BlockedShutdownIsObservableAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "completion-shutdown-" + Guid.NewGuid().ToString("N"));
        var logger = new CaptureLogger();
        long blocked = -1;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == "Gurux.DLMS.AMI.Agent.Completions" && instrument.Name == "agent.completions.shutdown_blocked")
                {
                current.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((_, value, _, _) => Interlocked.Exchange(ref blocked, value));
        listener.Start();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))))
            { BaseAddress = new("https://completion.test") };
        var outbox = new GXTaskCompletionOutbox(http, root, logger, TimeSpan.FromHours(1), new() { MinFreeBytes = 0 });
        var report = new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow, Data = "original result after storage recovery" };
        Task? shutdown = null;
        try
        {
            await RemoveEmptyQueueAsync(root);
            File.WriteAllText(root, "disk failure");
            await outbox.ReportAsync([report]);
            shutdown = outbox.DisposeAsync().AsTask();
            listener.RecordObservableInstruments();
            if (shutdown.IsCompleted || outbox.CanAcceptTasks || outbox.Status.UnpersistedCount != 1)
                {
                throw new Exception("Shutdown did not retain its in-memory result and stop admission.");
            }
            if (!logger.Entries.Any(e => e.Level == LogLevel.Critical && e.Message.Contains(root)))
                {
                throw new Exception("Blocked shutdown did not log a critical storage recovery instruction with its path.");
            }
            if (Interlocked.Read(ref blocked) != 1) {
                throw new Exception("Blocked shutdown is not exposed as a metric.");
            }
            File.Delete(root);
            Directory.CreateDirectory(root);
            await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
            var restored = JsonSerializer.Deserialize<TaskDone>(File.ReadAllText(Directory.GetFiles(root, "*.json").Single()))!.Tasks!.Single();
            if (restored.Id != report.Id || restored.Start != report.Start || restored.Data != report.Data)
                {
                throw new Exception("Storage recovery changed or lost the original completion.");
            }
            if (!logger.Entries.Any(e => e.Level == LogLevel.Information && e.Message.Contains("resumed", StringComparison.OrdinalIgnoreCase)))
                {
                throw new Exception("Shutdown recovery was not logged.");
            }
            Console.WriteLine("PASS observable blocked shutdown and lossless persistence after storage recovery");
        }
        finally
        {
            if (File.Exists(root)) {
                File.Delete(root);
            }
            Directory.CreateDirectory(root);
            await (shutdown ?? outbox.DisposeAsync().AsTask()).WaitAsync(TimeSpan.FromSeconds(5));
            Directory.Delete(root, true);
        }
    }

    private static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        while (!condition()) {
            await Task.Delay(20, timeout.Token);
        }
    }

    private static async Task RemoveEmptyQueueAsync(string directory)
    {
        // The initial retry and periodic monitor can briefly own the write probe on Windows.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            try { Directory.Delete(directory); return; }
            catch (IOException) when (Directory.Exists(directory)) { await Task.Delay(10, timeout.Token); }
        }
    }

    private sealed class CaptureLogger : ILogger
    {
        internal ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Enqueue((logLevel, formatter(state, exception)));
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
