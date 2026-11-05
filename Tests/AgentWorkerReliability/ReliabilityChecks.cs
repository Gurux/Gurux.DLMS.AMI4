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

using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Agent.Worker.AutoConnect;
using Gurux.DLMS.AMI.Agent.Worker.Gateway;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

internal static class ReliabilityChecks
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);
    private static void Assert(bool condition, string message) { if (!condition) {
        throw new Exception(message);
    } }
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Canceled(Task task)
    {
        try { await task.WaitAsync(Deadline); throw new Exception("Expected cancellation."); }
        catch (OperationCanceledException) { }
    }
    internal static async Task RunAsync()
    {
        await Registrations();
        await SessionShutdown();
        await IndependentWorkers();
        await QueueReconfiguration();
        await PendingReadCancellation();
        await GatewayFailures();
        await IncomingShutdown(false);
        await IncomingShutdown(true);
        await BlockingTransportAbort();
        await GatewayReplacement();
        await IdentificationCallbackShutdown();
    }

    private static async Task Registrations()
    {
        var notifications = new GXTaskNotification();
        var id = Guid.NewGuid();
        using var old = notifications.Register(id);
        var oldWait = old.WaitAsync(CancellationToken.None);
        using var current = notifications.Register(id);
        Assert(!await oldWait.WaitAsync(Deadline), "Replacing a connection must terminate the old wait.");
        old.Dispose();
        var currentWait = current.WaitAsync(CancellationToken.None);
        notifications.Set(id);
        Assert(await currentWait.WaitAsync(Deadline), "Old cleanup removed the new connection.");
        using var canceled = new CancellationTokenSource();
        var pending = current.WaitAsync(canceled.Token);
        canceled.Cancel();
        await Canceled(pending);
        notifications.Set(id);
        Assert(await current.WaitAsync(CancellationToken.None).WaitAsync(Deadline), "Cancellation consumed a future signal.");
        notifications.Close();
        Assert(!await current.WaitAsync(CancellationToken.None), "Closing must terminate connections, not just wake them.");
        using var restarted = notifications.Register(id);
        notifications.NotifyAll();
        Assert(await restarted.WaitAsync(CancellationToken.None).WaitAsync(Deadline), "Registry cannot be reused after service restart.");
        Console.WriteLine("PASS replacement ownership, notification cancellation and restart");
    }

    private static async Task SessionShutdown()
    {
        var started = Signal(); var aborted = Signal(); var cleaning = Signal(); var releaseCleanup = Signal();
        var errors = new List<Exception>();
        var sessions = new GXConnectionTasks(1, CancellationToken.None, errors.Add);
        Assert(sessions.TryStart("meter", async token =>
        {
            started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { cleaning.TrySetResult(); await releaseCleanup.Task; }
        }, () => aborted.TrySetResult()), "Connection was not admitted.");
        await started.Task.WaitAsync(Deadline);
        Assert(!sessions.TryStart("extra", _ => Task.CompletedTask, () => { }), "Capacity was exceeded.");
        var stopped = sessions.StopAsync();
        await aborted.Task.WaitAsync(Deadline);
        await cleaning.Task.WaitAsync(Deadline);
        Assert(!stopped.IsCompleted, "Stop returned before asynchronous cleanup finished.");
        releaseCleanup.TrySetResult();
        await stopped.WaitAsync(Deadline);
        Assert(sessions.Count == 0 && errors.Count == 0, "Session cleanup failed.");
        Assert(!sessions.TryStart("late", _ => Task.CompletedTask, () => { }), "Stopped service admitted new work.");
        using var lifetime = new CancellationTokenSource();
        var canceledSession = new GXConnectionTasks(1, lifetime.Token, errors.Add);
        var closed = Signal();
        canceledSession.TryStart("active", token => Task.Delay(Timeout.InfiniteTimeSpan, token), () => closed.TrySetResult());
        lifetime.Cancel();
        await closed.Task.WaitAsync(Deadline);
        await canceledSession.StopAsync().WaitAsync(Deadline);
        Console.WriteLine("PASS bounded sessions, host cancellation and awaited asynchronous cleanup");
    }

    private static async Task IndependentWorkers()
    {
        var defaults = new GXAgentOptions { Id = Guid.NewGuid(), Token = "test" };
        Assert(defaults.MaxIncomingConnections == 256, "Connection limit default changed.");
        defaults.MaxIncomingConnections = 0;
        Assert(defaults.GetValidationErrors().Any(e => e.PropertyPath == nameof(GXAgentOptions.MaxIncomingConnections)), "Nonpositive connection limits must be rejected.");
        var firstRequests = new List<string>(); var secondRequests = new List<string>();
        await using var first = new Host(new Handler((request, _) =>
        {
            firstRequests.Add(request.RequestUri!.Host + ":" + request.Headers.Authorization!.Parameter);
            return Task.FromResult(Json(new { AgentIds = new[] { Guid.NewGuid() } }));
        }), "https://first.test/", "first-token");
        await first.Worker.AddAgentAsync("first");
        await using var second = new Host(new Handler((request, _) =>
        {
            secondRequests.Add(request.RequestUri!.Host + ":" + request.Headers.Authorization!.Parameter);
            return Task.FromResult(Json(new { AgentIds = new[] { Guid.NewGuid() } }));
        }), "https://second.test/", "second-token");
        Assert(first.Worker.Options.Address == "https://first.test/", "Second initialization overwrote first configuration.");
        Assert(!ReferenceEquals(first.Worker.Scripts, second.Worker.Scripts), "Script catalogs are shared.");
        await Task.WhenAll(first.Worker.AddAgentAsync("first-again"), second.Worker.AddAgentAsync("second"));
        Assert(firstRequests.SequenceEqual(new[] { "first.test:first-token", "first.test:first-token" }), "First worker used another worker's HTTP configuration.");
        Assert(secondRequests.SequenceEqual(new[] { "second.test:second-token" }), "Second worker used the wrong HTTP configuration.");
        await first.Worker.StopAsync();
        await second.Worker.AddAgentAsync("second-after-first-stop");
        Assert(second.Worker.State == GXAgentWorkerState.Initialized, "Stopping the first worker stopped the second.");
        Console.WriteLine("PASS independent configurations, HTTP clients, script catalogs and lifetimes");
    }

    private static async Task QueueReconfiguration()
    {
        var done = Signal();
        await using var host = new Host(new Handler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.Equals("/api/task/Done", StringComparison.OrdinalIgnoreCase)) {
                done.TrySetResult();
            }
            return Task.FromResult(Json(new { }));
        }));
        var firstStarted = Signal(); var secondStarted = Signal(); var releaseFirst = Signal(); var releaseSecond = Signal();
        int processed = 0;
        var old = new ActionBlock<GXAgentWorker.GXActionBlock>(async _ =>
        {
            if (Interlocked.Increment(ref processed) == 1) { firstStarted.TrySetResult(); await releaseFirst.Task; }
            else { secondStarted.TrySetResult(); await releaseSecond.Task; }
        }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1, MaxDegreeOfParallelism = 1 });
        var field = typeof(GXAgentWorker).GetField("_meterReads", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((ActionBlock<GXAgentWorker.GXActionBlock>)field.GetValue(host.Worker)!).Complete();
        field.SetValue(host.Worker, old);
        GXAgentWorker.GXActionBlock Batch() => new() { Tasks = [new GXTask { Id = Guid.NewGuid(), TargetDevice = Guid.NewGuid() }] };
        await host.Worker.QueueMeterReadAsync(Batch(), CancellationToken.None);
        await firstStarted.Task.WaitAsync(Deadline);
        var blockedSend = host.Worker.QueueMeterReadAsync(Batch(), CancellationToken.None);
        var configure = (Task)typeof(GXAgentWorker).GetMethod("ConfigureMeterReadsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(host.Worker, [2, null])!;
        Assert(!blockedSend.IsCompleted && !configure.IsCompleted, "Queue did not enforce backpressure.");
        releaseFirst.TrySetResult();
        await secondStarted.Task.WaitAsync(Deadline);
        await blockedSend.WaitAsync(Deadline);
        Assert(!configure.IsCompleted, "Reconfiguration overlapped old and new readers.");
        var third = host.Worker.QueueMeterReadAsync(Batch(), CancellationToken.None);
        releaseSecond.TrySetResult();
        await configure.WaitAsync(Deadline);
        await third.WaitAsync(Deadline);
        await done.Task.WaitAsync(Deadline);
        Assert(processed == 2 && old.Completion.IsCompletedSuccessfully, "An accepted old batch was lost.");
        var current = (ActionBlock<GXAgentWorker.GXActionBlock>)field.GetValue(host.Worker)!;
        current.Complete(); await current.Completion.WaitAsync(Deadline);
        try { await host.Worker.QueueMeterReadAsync(Batch(), CancellationToken.None); throw new Exception("Closed queue silently accepted a batch."); }
        catch (InvalidOperationException) { }
        Console.WriteLine("PASS full-queue reconfiguration, draining, delivery and explicit rejection");
    }

    private static async Task PendingReadCancellation()
    {
        var entered = Signal(); var release = Signal(); var canceled = Signal();
        await using var host = new Host(new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath.Equals("/api/Device/", StringComparison.OrdinalIgnoreCase))
            {
                entered.TrySetResult();
                try { await release.Task.WaitAsync(token); }
                catch (OperationCanceledException) { canceled.TrySetResult(); throw; }
            }
            return Json(new { });
        }));
        using var cancellation = new CancellationTokenSource();
        var reading = host.Worker.ReadMeter(new GXAgentWorker.GXActionBlock
        { Tasks = [new GXTask { Id = Guid.NewGuid() }], CancellationToken = cancellation.Token });
        await entered.Task.WaitAsync(Deadline);
        cancellation.Cancel();
        try
        {
            await canceled.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await Canceled(reading);
        }
        finally
        {
            release.TrySetResult();
            try { await reading.WaitAsync(Deadline); } catch (OperationCanceledException) { }
        }
        Console.WriteLine("PASS connection cancellation interrupts a pending device HTTP request");
    }
    private static async Task GatewayFailures()
    {
        int requests = 0;
        await using var host = new Host(new Handler((_, _) =>
        {
            Interlocked.Increment(ref requests);
            throw new HttpRequestException("Server unavailable");
        }), configure: options =>
        {
            options.TraceLevel = TraceLevel.Verbose;
            options.GatewaySettings = new() { Enabled = false, Id = Guid.NewGuid() };
        });
        var service = host.Provider.GetRequiredService<GXGatewayService>();
        await service.StartAsync(CancellationToken.None);
        await service.ReportGatewayEventAsync(GatewayStatus.Connected, "test", CancellationToken.None);
        Assert(requests == 2, "Status failure prevented independent event logging.");
        await service.StopAsync(CancellationToken.None);
        Console.WriteLine("PASS gateway status and logging failures are contained");
    }

    private static async Task IncomingShutdown(bool gateway)
    {
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start();
        int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        using var media = new Gurux.Net.GXNet(Gurux.Net.NetworkType.Tcp, port);
        await using var host = new Host(configure: options =>
        {
            if (gateway) {
                options.GatewaySettings = new() { Enabled = true, MediaSettings = media.Settings, IdentifyWaitTime = 3600 };
            }
            else {
                options.ListenerSettings = new() { Enabled = true, MediaSettings = media.Settings, IdentifyWaitTime = 3600, Threads = 1 };
            }
        });
        var service = gateway ? (Microsoft.Extensions.Hosting.IHostedService)host.Provider.GetRequiredService<GXGatewayService>() : host.Provider.GetRequiredService<GXAutoConnectService>();
        await service.StartAsync(CancellationToken.None);
        var originalSessions = service.GetType().GetField("sessions", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service);
        await service.StartAsync(CancellationToken.None);
        Assert(ReferenceEquals(originalSessions, service.GetType().GetField("sessions", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service)), "Repeated start abandoned active sessions.");
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var field = service.GetType().GetField("sessions", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var sessions = (GXConnectionTasks)field.GetValue(service)!;
        using var timeout = new CancellationTokenSource(Deadline);
        while (sessions.Count == 0) {
            await Task.Delay(10, timeout.Token);
        }
        await service.StopAsync(CancellationToken.None).WaitAsync(Deadline);
        Assert(sessions.Count == 0, "Incoming reader survived service stop.");
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None).WaitAsync(Deadline);
        Console.WriteLine($"PASS real {(gateway ? "gateway" : "auto-connect")} identification cancellation, stop and restart");
    }

    private static async Task BlockingTransportAbort()
    {
        using var lifetime = new CancellationTokenSource();
        using var abortStarted = new ManualResetEventSlim();
        using var releaseAbort = new ManualResetEventSlim();
        var entered = Signal();
        var sessions = new GXConnectionTasks(1, lifetime.Token, _ => { });
        sessions.TryStart("blocked-close", async token =>
        { entered.TrySetResult(); await Task.Delay(Timeout.Infinite, token); }, () => { abortStarted.Set(); releaseAbort.Wait(); });
        await entered.Task.WaitAsync(Deadline);
        try
        {
            await Task.Run(lifetime.Cancel).WaitAsync(TimeSpan.FromSeconds(1));
            Assert(abortStarted.Wait(Deadline), "Transport abort was never requested.");
            var stop = sessions.StopAsync();
            Assert(!stop.IsCompleted, "Stop ignored unfinished transport cleanup.");
            releaseAbort.Set();
            await stop.WaitAsync(Deadline);
        }
        finally { releaseAbort.Set(); }
        Console.WriteLine("PASS blocking transport close cannot block lifetime cancellation");
    }

    private static async Task GatewayReplacement()
    {
        var statuses = System.Threading.Channels.Channel.CreateUnbounded<GatewayStatus>();
        var requested = System.Threading.Channels.Channel.CreateUnbounded<bool>();
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start();
        int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        using var media = new Gurux.Net.GXNet(Gurux.Net.NetworkType.Tcp, port);
        await using var host = new Host(new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/Gateway/UpdateStatus")
            {
                var status = await request.Content!.ReadFromJsonAsync<Gurux.DLMS.AMI.Shared.Rest.UpdateGatewayStatus>(token);
                statuses.Writer.TryWrite(status!.Status);
            }
            if (request.RequestUri.AbsolutePath == "/api/Task/Next")
            {
                requested.Writer.TryWrite(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            return Json(new { Tasks = Array.Empty<GXTask>() });
        }), configure: options =>
        {
            options.MaxIncomingConnections = 1;
            options.GatewaySettings = new() { Enabled = true, MediaSettings = media.Settings, Id = Guid.NewGuid() };
        });
        var service = host.Provider.GetRequiredService<GXGatewayService>();
        await service.StartAsync(CancellationToken.None);
        using var first = new TcpClient(); await first.ConnectAsync(IPAddress.Loopback, port);
        Assert(await statuses.Reader.ReadAsync().AsTask().WaitAsync(Deadline) == GatewayStatus.Connected, "First connection did not report Connected.");
        await requested.Reader.ReadAsync().AsTask().WaitAsync(Deadline);
        using var second = new TcpClient(); await second.ConnectAsync(IPAddress.Loopback, port);
        Assert(await statuses.Reader.ReadAsync().AsTask().WaitAsync(Deadline) == GatewayStatus.Offline, "Old connection did not report Offline.");
        Assert(await statuses.Reader.ReadAsync().AsTask().WaitAsync(Deadline) == GatewayStatus.Connected, "Replacement was rejected at capacity or stale Offline overwrote Connected.");
        await requested.Reader.ReadAsync().AsTask().WaitAsync(Deadline);
        first.Close();

        var sessionOwner = (GXConnectionTasks)typeof(GXGatewayService).GetField("sessions", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service)!;
        var active = (System.Collections.IDictionary)typeof(GXConnectionTasks).GetField("connections", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(sessionOwner)!;
        var secondInfo = active.Keys.Cast<string>().Single();
        second.Close();
        // Attached GXNet clients in the current library do not emit an event for a clean TCP FIN.
        // Exercise the disconnect-event contract explicitly as well as the physical socket close.
        service.OnClientDisconnected(media, new Gurux.Common.ConnectionEventArgs(secondInfo));
        Assert(await statuses.Reader.ReadAsync().AsTask().WaitAsync(Deadline) == GatewayStatus.Offline, "Ordinary disconnect did not report Offline.");
        await service.StopAsync(CancellationToken.None).WaitAsync(Deadline);
        Console.WriteLine("PASS real gateway replacement at capacity, old disconnect and status ordering");
    }

    private static async Task IdentificationCallbackShutdown()
    {
        var entered = Signal(); var release = Signal(); var receiverReady = Signal();
        using var logs = new CallbackLogger(id =>
        {
            if (id.Name == "GatewayReceiverReady")
            {
                receiverReady.TrySetResult();
            }
        });
        var methodId = Guid.NewGuid(); var scriptId = Guid.NewGuid();
        var code = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Gurux.Net;
            using Gurux.DLMS.AMI.Script;
            using Gurux.DLMS.AMI.Shared.DTOs;
            using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
            namespace Gurux.DLMS.AMI.GeneratedScript {
                public class GXAmiScript {
                    private readonly IGXAmi ami;
                    public GXAmiScript(IGXAmi value) { ami = value; }
                    public async Task<GXGateway> Identify(string info, GXNet media, byte[] data) {
                        await ami.AddAsync(new GXValue { Attribute = new GXAttribute { Id = Guid.NewGuid() }, Value = "blocking" }, CancellationToken.None);
                        await Task.Yield();
                        if (media.IsOpen) media.Send(new byte[] { 0x42 }, null);
                        return new GXGateway { Id = Guid.NewGuid() };
                    }
                }
            }
            """;
        var errors = new List<Gurux.DLMS.AMI.Script.GXScriptException>();
        var assembly = Gurux.DLMS.AMI.Script.GXAmiScript.Generate(ScriptLanguage.CSharp, Guid.NewGuid().ToString(), code, errors, null)
            ?? throw new Exception("Identification test compile failed: " + string.Join("; ", errors));
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start();
        int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        using var media = new Gurux.Net.GXNet(Gurux.Net.NetworkType.Tcp, port);
        await using var host = new Host(new Handler(async (request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/Script/List") {
                return Json(new
            {
                Scripts = new[] { new Gurux.DLMS.AMI.Shared.DTOs.Script.GXScript { Id = scriptId, ByteAssembly = assembly,
                    Methods = [new() { Id = methodId, Name = "Identify", IsAsynchronous = true }] } }
            });
            }
            if (request.RequestUri.AbsolutePath == "/api/Value/Add") { entered.TrySetResult(); await release.Task; return Json(new { Ids = new[] { Guid.NewGuid() } }); }
            return Json(new { });
        }), configure: options => options.GatewaySettings = new()
        { Enabled = true, MediaSettings = media.Settings, IdentifyWaitTime = 3600, ScriptMethod = methodId, TraceLevel = TraceLevel.Verbose }, logs: logs);
        var service = host.Provider.GetRequiredService<GXGatewayService>();
        await service.StartAsync(CancellationToken.None);
        using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
        await receiverReady.Task.WaitAsync(Deadline);
        await client.GetStream().WriteAsync(new byte[] { 1 });
        await entered.Task.WaitAsync(Deadline);
        release.TrySetResult();
        var reply = new byte[1];
        Assert(await client.GetStream().ReadAsync(reply).AsTask().WaitAsync(Deadline) == 1 && reply[0] == 0x42, "Async identification Send deadlocked verbose tracing.");
        await service.StopAsync(CancellationToken.None).WaitAsync(Deadline);
        entered = Signal(); release = Signal(); receiverReady = Signal();
        await service.StartAsync(CancellationToken.None);
        using var blockedClient = new TcpClient(); await blockedClient.ConnectAsync(IPAddress.Loopback, port);
        await receiverReady.Task.WaitAsync(Deadline);
        await blockedClient.GetStream().WriteAsync(new byte[] { 1 });
        await entered.Task.WaitAsync(Deadline);
        using var caller = new CancellationTokenSource();
        var stop = service.StopAsync(caller.Token);
        caller.Cancel();
        try
        {
            await Canceled(stop);
            var cleanup = service.StopAsync(CancellationToken.None);
            Assert(!cleanup.IsCompleted, "Stop released host resources while an identification callback was active.");
            release.TrySetResult();
            await cleanup.WaitAsync(Deadline);
        }
        finally { release.TrySetResult(); }
        var registry = (GXTaskNotification)host.Provider.GetRequiredService<IGXTaskNotification>();
        var entries = (System.Collections.IDictionary)typeof(GXTaskNotification).GetField("registrations", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(registry)!;
        Assert(entries.Count == 0, "Callback registered a connection after shutdown cleanup.");
        Console.WriteLine("PASS async identification Send with verbose trace and awaited callback cleanup after canceled shutdown wait");
    }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private sealed class CallbackLogger(Action<EventId> callback) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Logger(callback);
        public void Dispose() { }
        private sealed class Logger(Action<EventId> callback) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
                => callback(id);
        }
    }
    private sealed class Host : IAsyncDisposable
    {
        internal readonly HttpClient Http;
        internal readonly GXAgentWorker Worker;
        internal readonly ServiceProvider Provider;
        private readonly GXAgentHostNotifications notifications = new();
        internal Host(HttpMessageHandler? handler = null, string address = "https://agent.test/", string token = "test-token", Action<GXAgentOptions>? configure = null, ILoggerProvider? logs = null)
        {
            Http = new HttpClient(handler ?? new Handler((_, _) => Task.FromResult(Json(new { }))));
            Worker = new GXAgentWorker(Http);
            var services = new ServiceCollection().AddLogging(builder =>
            {
                if (logs != null)
                {
                    builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs);
                }
            });
            Worker.ConfigureServices(services);
            Provider = services.BuildServiceProvider();
            var options = new GXAgentOptions { Id = Guid.NewGuid(), Address = address, Token = token };
            configure?.Invoke(options); Worker.Init(Provider, options, notifications);
        }
        public async ValueTask DisposeAsync()
        {
            await Worker.DisposeAsync(); await Provider.DisposeAsync(); Http.Dispose(); notifications.Dispose();
        }
    }
}
