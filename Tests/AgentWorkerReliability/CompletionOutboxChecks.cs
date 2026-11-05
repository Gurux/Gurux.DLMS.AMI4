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
using System.Reflection;

internal static class CompletionOutboxChecks
{
    internal static async Task RunAsync()
    {
        var type = typeof(GXAgentWorker).Assembly.GetType("Gurux.DLMS.AMI.Agent.Worker.GXTaskCompletionOutbox")
            ?? throw new Exception("Completion reports have no durable retry outbox.");
        var directory = Path.Combine(Path.GetTempPath(), "gurux-completions-" + Guid.NewGuid().ToString("N"));
        var item = new GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow, Result = null, Data = "executed-once" };
        object Create(HttpClient http) => type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single()
            .Invoke([http, directory, null, TimeSpan.FromMilliseconds(30), null]);
        try
        {
            int attempts = 0;
            using var failingHttp = new HttpClient(new Handler((_, _) =>
            { Interlocked.Increment(ref attempts); throw new HttpRequestException("Lost completion response"); })) { BaseAddress = new Uri("https://ack.test/") };
            var first = Create(failingHttp);
            await (Task)type.GetMethod("ReportAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(first, [new[] { item }])!;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (attempts < 2) {
                await Task.Delay(10, timeout.Token);
            }
            await ((IAsyncDisposable)first).DisposeAsync();
            if (Directory.GetFiles(directory, "*.json").Length != 1) {
                throw new Exception("Failed report was not retained durably.");
            }
            var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var recoveredHttp = new HttpClient(new Handler(async (request, token) =>
            {
                var body = await request.Content!.ReadFromJsonAsync<TaskDone>(token);
                var report = body!.Tasks!.Single();
                if (report.Id != item.Id || report.Start != item.Start || report.Result != null || report.Data != "executed-once")
                    {
                    throw new Exception("Retry changed the original task claim/result.");
                }
                acknowledged.TrySetResult();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new TaskDoneResponse()) };
            })) { BaseAddress = new Uri("https://ack.test/") };
            var second = Create(recoveredHttp);
            await acknowledged.Task.WaitAsync(TimeSpan.FromSeconds(5));
            while (Directory.GetFiles(directory, "*.json").Length != 0) {
                await Task.Delay(10, timeout.Token);
            }
            await ((IAsyncDisposable)second).DisposeAsync();
            Console.WriteLine("PASS completion retry, immutable result payload and recovery after worker restart");
        }
        finally { if (Directory.Exists(directory)) {
            Directory.Delete(directory, true);
        } }
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
