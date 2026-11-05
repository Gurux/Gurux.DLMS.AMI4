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
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

internal static class CompletionResilienceChecks
{
    internal static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "completion-resilience-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Guid slow = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                healthy = Guid.Parse("00000000-0000-0000-0000-000000000002"), rejected = Guid.Parse("00000000-0000-0000-0000-000000000003");
            string Record(Guid id) => Path.Combine(root, $"{id:N}-1.json");
            void Save(Guid id) => File.WriteAllText(Record(id), JsonSerializer.Serialize(new TaskDone { Tasks = [new GXTask { Id = id, Start = DateTimeOffset.UtcNow, Data = "original" }] }));
            Save(slow); Save(healthy); Save(rejected);
            File.WriteAllText(Path.Combine(root, "corrupt.json"), "{not json");
            int healthyUploads = 0, rejectedUploads = 0;
            using var http = new HttpClient(new Handler(async (request, token) =>
            {
                var item = (await request.Content!.ReadFromJsonAsync<TaskDone>(token))!.Tasks!.Single();
                if (item.Id == slow) { await Task.Delay(Timeout.Infinite, token); }
                if (item.Id == healthy) {
                    Interlocked.Increment(ref healthyUploads);
                }
                if (item.Id == rejected) { Interlocked.Increment(ref rejectedUploads); return new(HttpStatusCode.BadRequest); }
                return new(HttpStatusCode.OK);
            })) { BaseAddress = new("https://completion.test") };
            var limits = new GXCompletionOutboxOptions { RequestTimeout = TimeSpan.FromSeconds(2), FlushBudget = TimeSpan.FromMilliseconds(50), MinFreeBytes = 0 };
            await using (var outbox = new GXTaskCompletionOutbox(http, root, null, TimeSpan.FromMilliseconds(30), limits))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (healthyUploads == 0 || outbox.Status.QuarantinedCount < 2) {
                    await Task.Delay(20, timeout.Token);
                }
                await Task.Delay(150);
                if (rejectedUploads != 1 || !File.Exists(Record(slow)) || Directory.GetFiles(Path.Combine(root, "quarantine")).Length != 2)
                    {
                    throw new Exception("Fair retry/quarantine lost a report or replayed a permanent rejection.");
                }
                limits.MaxPendingRecords = 1;
                await WaitForAdmissionAsync(outbox, allowed: false);
                if (outbox.CanAcceptTasks) {
                    throw new Exception("Pending completion limit did not pause claims.");
                }
                limits.MaxPendingRecords = 10000;
                await WaitForAdmissionAsync(outbox, allowed: true);
                limits.MaxPendingBytes = 1;
                await WaitForAdmissionAsync(outbox, allowed: false);
                if (outbox.CanAcceptTasks) {
                    throw new Exception("Completion byte limit did not pause claims.");
                }
                limits.MaxPendingBytes = 256L * 1024 * 1024;
                await WaitForAdmissionAsync(outbox, allowed: true);
                limits.MinFreeBytes = long.MaxValue;
                await WaitForAdmissionAsync(outbox, allowed: false);
                if (outbox.CanAcceptTasks) {
                    throw new Exception("Low disk space did not pause claims.");
                }
                var worker = new GXAgentWorker(http);
                var outboxField = typeof(GXAgentWorker).GetField("completionOutbox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                outboxField.SetValue(worker, outbox);
                try
                {
                    var claim = typeof(GXAgentWorker).GetMethod("ClaimPolledTasksAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                    var task = (Task)claim.Invoke(worker, [CancellationToken.None])!;
                    await task;
                    if (task.GetType().GetProperty("Result")!.GetValue(task) != null) {
                        throw new Exception("Paused worker claimed a new task.");
                    }
                }
                finally { outboxField.SetValue(worker, null); await worker.DisposeAsync(); }
                limits.MinFreeBytes = 0;
                await WaitForAdmissionAsync(outbox, allowed: true);
                if (!outbox.CanAcceptTasks) {
                    throw new Exception("Claims did not resume after disk recovery.");
                }
            }
            int authAttempts = 0, throttledAttempts = 0;
            var authTask = new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow };
            var throttledTask = new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow };
            using var transientHttp = new HttpClient(new Handler(async (request, token) =>
            {
                var item = (await request.Content!.ReadFromJsonAsync<TaskDone>(token))!.Tasks!.Single();
                if (item.Id == authTask.Id) { Interlocked.Increment(ref authAttempts); return new(HttpStatusCode.Unauthorized); }
                Interlocked.Increment(ref throttledAttempts);
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(2));
                return response;
            })) { BaseAddress = new("https://completion.test") };
            await using (var outbox = new GXTaskCompletionOutbox(transientHttp, Path.Combine(root, "transient"), null,
                TimeSpan.FromMilliseconds(30), new GXCompletionOutboxOptions { MinFreeBytes = 0 }))
            {
                await outbox.ReportAsync([authTask, throttledTask]);
                await Task.Delay(150);
                if (outbox.Status.QuarantinedCount != 0 || throttledAttempts != 1 || authAttempts < 2)
                    {
                    throw new Exception("Authentication errors must remain retryable and HTTP Retry-After must be respected.");
                }
            }
            string failedRoot = Path.Combine(root, "storage");
            var storageLimits = new GXCompletionOutboxOptions { MinFreeBytes = 0 };
            using var upload = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))) { BaseAddress = new("https://completion.test") };
            await using (var outbox = new GXTaskCompletionOutbox(upload, failedRoot, null, TimeSpan.FromHours(1), storageLimits))
            {
                Directory.Delete(failedRoot);
                File.WriteAllText(failedRoot, "blocked directory");
                await outbox.ReportAsync([new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow, Data = "must survive write failure" }]);
                if (outbox.CanAcceptTasks || outbox.Status.UnpersistedCount != 1) {
                    throw new Exception("Persistence failure did not preserve result and pause claims.");
                }
                var shutdown = outbox.DisposeAsync().AsTask();
                await Task.Delay(80);
                if (shutdown.IsCompleted) {
                    throw new Exception("Shutdown discarded an in-memory result instead of waiting for storage recovery.");
                }
                File.Delete(failedRoot); Directory.CreateDirectory(failedRoot);
                await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
                if (Directory.GetFiles(failedRoot, "*.json").Length != 1) {
                    throw new Exception("Storage recovery lost the original result.");
                }
            }
            Console.WriteLine("PASS completion fairness, quarantine, storage limits and recovery of an unpersisted result.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    private static async Task WaitForAdmissionAsync(GXTaskCompletionOutbox outbox, bool allowed)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (outbox.CanAcceptTasks != allowed) {
            await Task.Delay(20, timeout.Token);
        }
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }
}
