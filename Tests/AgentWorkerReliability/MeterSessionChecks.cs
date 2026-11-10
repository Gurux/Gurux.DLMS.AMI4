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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Secure;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

internal static class MeterSessionChecks
{
    private static void Check(bool ok, string message) { if (!ok) {
        throw new Exception(message);
    } }
    internal static async System.Threading.Tasks.Task RunAsync()
    {
        var failures = new List<string>();
        foreach (var (name, test) in new (string, Func<System.Threading.Tasks.Task>)[]
        {
            ("failed password change stops ordinary work", PasswordFailureStopsSession), ("IEC timeout units", TimeoutUnits), ("receive cancellation", ReceiveCancellation), ("receive total timeout and retry cancellation", WaitBudget),
            ("owned media cleanup", FailedSetupCleanup), ("same-session follow-up tasks", FollowUpTasks), ("poller handoff to connected meter", PollerHandoff), ("incoming sessions close when empty", IncomingEmpty), ("tasks handed off during failed setup", HandoffDuringFailedSetup), ("infinite receive cancellation", InfiniteReceiveCancellation), ("IP-identified gateway drains tasks", IpIdentifiedGateway), ("completion retry never repeats meter write", WriteCompletionRetry)
        })
        {
            try { await test(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures.Add(name + ": " + error.Message); Console.WriteLine("FAIL " + failures[^1]); }
        }
        if (failures.Count != 0) {
            throw new Exception(string.Join(Environment.NewLine, failures));
        }
    }
    private static System.Threading.Tasks.Task TimeoutUnits()
    {
        using var media = new ProbeMedia(); using var http = new HttpClient();
        var reader = new GXDLMSReader(new GXDLMSSecureClient(true, 16, 1, Authentication.None, null, InterfaceType.WRAPPER),
            media, null, TraceLevel.Off, TraceLevel.Off, 5, 1, null, http);
        typeof(GXDLMSReader).GetMethod("DiscIEC", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(reader, null);
        Check(media.LastWaitTime > 0 && media.LastWaitTime <= 5000, $"Five-second receive used {media.LastWaitTime} ms.");
        return System.Threading.Tasks.Task.CompletedTask;
    }
    private static async System.Threading.Tasks.Task ReceiveCancellation()
    {
        using var media = new ProbeMedia { BlockReceive = true }; using var http = new HttpClient();
        using var cancellation = new CancellationTokenSource();
        var reader = new GXDLMSReader(new GXDLMSSecureClient(true, 16, 1, Authentication.None, null, InterfaceType.WRAPPER),
            media, null, TraceLevel.Off, TraceLevel.Off, 5, 1, null, http) { CancellationToken = cancellation.Token };
        var reading = System.Threading.Tasks.Task.Run(() => reader.ReadDLMSPacket(new byte[] { 1 }, new Gurux.DLMS.GXReplyData()));
        while (media.Receives == 0) {
            await System.Threading.Tasks.Task.Delay(5);
        }
        cancellation.Cancel();
        try
        {
            try { await reading.WaitAsync(TimeSpan.FromSeconds(1)); throw new Exception("Receive ignored cancellation."); }
            catch (OperationCanceledException) { }
        }
        finally { media.Close(); try { await reading; } catch { } }
    }
    private static async System.Threading.Tasks.Task WaitBudget()
    {
        using var media = new ProbeMedia { BlockReceive = true }; using var http = new HttpClient();
        using var cancellation = new CancellationTokenSource();
        var reader = new GXDLMSReader(new GXDLMSSecureClient(true, 16, 1, Authentication.None, null, InterfaceType.WRAPPER),
            media, null, TraceLevel.Off, TraceLevel.Off, 5, 1, null, http) { CancellationToken = cancellation.Token };
        var parameters = new Gurux.Common.ReceiveParameters<byte[]> { Count = 1, WaitTime = 500 };
        var elapsed = Stopwatch.StartNew();
        Check(!reader.Receive(parameters), "A timed-out receive unexpectedly succeeded.");
        Check(elapsed.ElapsedMilliseconds >= 450 && elapsed.ElapsedMilliseconds < 1500 && parameters.WaitTime == 500,
            "Receive slicing changed the configured total timeout.");
        var waiting = System.Threading.Tasks.Task.Run(() => reader.Wait(10000));
        cancellation.CancelAfter(30);
        try { await waiting.WaitAsync(TimeSpan.FromSeconds(1)); throw new Exception("Long protocol retry wait ignored cancellation."); }
        catch (OperationCanceledException) { }
    }
    private static GXDevice Device(Guid id, bool invalid = false)
    {
        var settings = JsonSerializer.Serialize(new GXDLMSSettings { InterfaceType = (int)InterfaceType.WRAPPER,
            PreEstablished = true, IgnoreSNRMWithPreEstablished = true, UseLogicalNameReferencing = true, ClientAddress = 16 });
        return new GXDevice { Id = id, MediaType = typeof(ProbeMedia).AssemblyQualifiedName, MediaSettings = "", WaitTime = 1, ResendCount = 1,
            Settings = invalid ? "{" : settings, Template = new GXDeviceTemplate { Settings = settings } };
    }
    private static GXTask Item(Guid device) => new() { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow,
        TargetDevice = device, Device = new GXDevice { Id = device, Objects = [] }, Data = "retained-result" };
    private static async System.Threading.Tasks.Task FailedSetupCleanup()
    {
        var id = Guid.NewGuid();
        await using var host = new Host((request, _) => System.Threading.Tasks.Task.FromResult(Json(
            request.Method == HttpMethod.Get ? new GetDeviceResponse { Item = Device(id, true) } : new object())));
        await host.Worker.ReadMeter(new() { Tasks = [Item(id)] });
        Check(ProbeMedia.Last!.Closes > 0 && ProbeMedia.Last.Disposals == 1, "Opened transport was not closed/disposed after invalid settings.");
    }
    private static async System.Threading.Tasks.Task PasswordFailureStopsSession()
    {
        var id = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var password = Item(id);
        password.TaskType = Gurux.DLMS.AMI.Shared.Enums.TaskType.Write;
        password.Data = JsonSerializer.Serialize(new { PasswordChangeId = operation });
        var ordinary = Item(id);
        ordinary.TaskType = Gurux.DLMS.AMI.Shared.Enums.TaskType.Write;
        ordinary.Object = new GXObject { Template = new GXObjectTemplate { ObjectType = 1, LogicalName = "0.0.96.1.0.255" } };
        ordinary.Index = 2;
        ordinary.Data = Gurux.DLMS.GXDLMSTranslator.ValueToXml((byte)42);
        var device = Device(id);
        var settings = JsonSerializer.Deserialize<GXDLMSSettings>(device.Settings!)!;
        settings.Authentication = 1;
        device.Settings = JsonSerializer.Serialize(settings);
        await using var host = new Host((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/Work"))
            {
                return System.Threading.Tasks.Task.FromResult(Json(new
                {
                    Id = operation, State = "Queued", Device = device, AssociationLogicalName = "0.0.40.0.0.255",
                    CurrentPassword = new byte[] { 1 }, NewPassword = new byte[] { 2 }
                }));
            }
            if (path.EndsWith("/Writing"))
            {
                throw new IOException("sensitive-response-placeholder");
            }
            return System.Threading.Tasks.Task.FromResult(Json(new { }));
        });
        await host.Worker.ReadMeter(new() { Tasks = [password, ordinary] });
        Check(ProbeMedia.Last!.Writes == 0, "An ordinary task ran after a failed password operation.");
        Check(ordinary.Ready != null && !string.IsNullOrEmpty(ordinary.Result), "The blocked ordinary task was abandoned.");
        Check(!password.Result!.Contains("sensitive-response-placeholder"), "A password operation copied an arbitrary response into its task report.");
    }

    private static async System.Threading.Tasks.Task FollowUpTasks()
    {
        var id = Guid.NewGuid(); var first = Item(id); var second = Item(id); int polls = 0, deviceFetches = 0;
        first.TaskType = second.TaskType = Gurux.DLMS.AMI.Shared.Enums.TaskType.Write;
        first.Object = second.Object = new GXObject { Template = new GXObjectTemplate { ObjectType = 1, LogicalName = "0.0.96.1.0.255" } };
        first.Index = second.Index = 2;
        first.Data = second.Data = Gurux.DLMS.GXDLMSTranslator.ValueToXml((byte)42);
        var completed = new List<Guid>(); var results = new List<string?>();
        await using var host = new Host(async (request, token) =>
        {
            var path = request.RequestUri!.AbsolutePath.ToLowerInvariant();
            if (request.Method == HttpMethod.Get) { deviceFetches++; return Json(new GetDeviceResponse { Item = Device(id) }); }
            if (path == "/api/task/next")
            {
                var body = await request.Content!.ReadFromJsonAsync<GetNextTask>(token);
                Check(body!.DeviceId == id && body.WaitTime == 0, "Follow-up poll was not scoped to the connected meter.");
                return Json(new GetNextTaskResponse { Tasks = ++polls == 1 ? [second] : [] });
            }
            if (path == "/api/task/done")
            {
                var body = await request.Content!.ReadFromJsonAsync<TaskDone>(token);
                results.AddRange(body!.Tasks!.Select(t => t.Result));
                completed.AddRange(body.Tasks!.Select(t => t.Id));
            }
            return Json(new { });
        });
        await host.Worker.ReadMeter(new() { Tasks = [first] });
        Check(results.All(t => t == null), "Meter operations failed: " + string.Join("; ", results));
        Check(completed.SequenceEqual(new[] { first.Id, second.Id }), "New tasks were not executed before disconnect.");
        Check(polls == 2 && deviceFetches == 1, "Reader was reinitialized or queue was not drained to empty.");
        Check(ProbeMedia.Last!.Writes == 2, "Each meter write must execute exactly once.");
        Check(ProbeMedia.Last!.Opens == 1 && ProbeMedia.Last.Disposals == 1, "Meter transport was not reused and disposed once.");
    }
    private static async System.Threading.Tasks.Task PollerHandoff()
    {
        var claim = typeof(GXAgentWorker).GetMethod("ClaimPolledTasksAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception("Poller cannot deliver already-claimed tasks to an active meter session.");
        var id = Guid.NewGuid(); var first = Item(id); var second = Item(id);
        var firstDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new List<Guid>();
        await using var host = new Host(async (request, token) =>
        {
            if (request.Method == HttpMethod.Get) {
                return Json(new GetDeviceResponse { Item = Device(id) });
            }
            if (request.RequestUri!.AbsolutePath.Equals("/api/task/next", StringComparison.OrdinalIgnoreCase))
            {
                var body = await request.Content!.ReadFromJsonAsync<GetNextTask>(token);
                return Json(new GetNextTaskResponse { Tasks = body!.DeviceId == null ? [second] : [] });
            }
            if (request.RequestUri.AbsolutePath.Equals("/api/task/done", StringComparison.OrdinalIgnoreCase))
            {
                var body = await request.Content!.ReadFromJsonAsync<TaskDone>(token);
                completed.AddRange(body!.Tasks!.Select(t => t.Id));
                if (body.Tasks!.Single().Id == first.Id) { firstDone.TrySetResult(); await release.Task; }
            }
            return Json(new { });
        });
        var reading = host.Worker.ReadMeter(new() { Tasks = [first] });
        await firstDone.Task.WaitAsync(TimeSpan.FromSeconds(3));
        try
        {
            var claimed = await (System.Threading.Tasks.Task<GXAgentWorker.GXActionBlock?>)claim.Invoke(host.Worker, [CancellationToken.None])!;
            Check(claimed == null, "Already connected meter was scheduled for a second connection.");
        }
        finally { release.TrySetResult(); }
        await reading.WaitAsync(TimeSpan.FromSeconds(3));
        Check(completed.SequenceEqual(new[] { first.Id, second.Id }) && ProbeMedia.Last!.Opens == 1,
            "An initial poller claim was lost instead of being executed on the connected meter.");
    }
    private static async System.Threading.Tasks.Task IncomingEmpty()
    {
        foreach (bool gateway in new[] { false, true })
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            using var settingsMedia = new Gurux.Net.GXNet(Gurux.Net.NetworkType.Tcp, port);
            int polls = 0;
            await using var host = new Host((request, _) =>
            {
                if (request.RequestUri!.AbsolutePath.Equals("/api/task/next", StringComparison.OrdinalIgnoreCase)) {
                    Interlocked.Increment(ref polls);
                }
                return System.Threading.Tasks.Task.FromResult(Json(new GetNextTaskResponse { Tasks = [] }));
            }, options =>
            {
                if (gateway) {
                    options.GatewaySettings = new() { Enabled = true, MediaSettings = settingsMedia.Settings, Id = Guid.NewGuid() };
                }
                else {
                    options.ListenerSettings = new() { Enabled = true, MediaSettings = settingsMedia.Settings, PreEstablished = true, IdentifyWaitTime = 0, Threads = 1 };
                }
            });
            var service = gateway ? (Microsoft.Extensions.Hosting.IHostedService)host.Provider.GetRequiredService<Gurux.DLMS.AMI.Agent.Worker.AutoConnect.GXGatewayService>()
                : host.Provider.GetRequiredService<Gurux.DLMS.AMI.Agent.Worker.AutoConnect.GXAutoConnectService>();
            await service.StartAsync(CancellationToken.None);
            using var client = new System.Net.Sockets.TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
            int bytes = await client.GetStream().ReadAsync(new byte[1]).AsTask().WaitAsync(TimeSpan.FromSeconds(3));
            Check(bytes == 0 && polls == 1, "An empty incoming task queue was left waiting for notifications.");
            await service.StopAsync(CancellationToken.None);
        }
    }
    private static async System.Threading.Tasks.Task InfiniteReceiveCancellation()
    {
        using var media = new ProbeMedia { BlockReceive = true }; using var http = new HttpClient();
        using var cancellation = new CancellationTokenSource();
        var reader = new GXDLMSReader(new GXDLMSSecureClient(true, 16, 1, Authentication.None, null, InterfaceType.WRAPPER),
            media, null, TraceLevel.Off, TraceLevel.Off, 0, 1, null, http) { CancellationToken = cancellation.Token };
        var receive = System.Threading.Tasks.Task.Run(() => reader.Receive(new Gurux.Common.ReceiveParameters<byte[]> { Count = 1, WaitTime = 0 }));
        while (media.Receives == 0) {
            await System.Threading.Tasks.Task.Delay(5);
        }
        cancellation.Cancel();
        try
        {
            try { await receive.WaitAsync(TimeSpan.FromSeconds(1)); throw new Exception("Infinite receive ignored cancellation."); }
            catch (OperationCanceledException) { }
        }
        finally { media.Close(); try { await receive; } catch { } }
    }

    private static async System.Threading.Tasks.Task HandoffDuringFailedSetup()
    {
        var id = Guid.NewGuid(); var first = Item(id); var second = Item(id);
        var opening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new List<Guid>();
        ProbeMedia.Opening = _ => { opening.TrySetResult(); release.Task.GetAwaiter().GetResult(); throw new IOException("Cannot connect to meter"); };
        try
        {
            await using var host = new Host(async (request, token) =>
            {
                if (request.Method == HttpMethod.Get) {
                    return Json(new GetDeviceResponse { Item = Device(id) });
                }
                if (request.RequestUri!.AbsolutePath.Equals("/api/task/next", StringComparison.OrdinalIgnoreCase)) {
                    return Json(new GetNextTaskResponse { Tasks = [second] });
                }
                if (request.RequestUri.AbsolutePath.Equals("/api/task/done", StringComparison.OrdinalIgnoreCase))
                {
                    var body = await request.Content!.ReadFromJsonAsync<TaskDone>(token);
                    completed.AddRange(body!.Tasks!.Select(t => t.Id));
                }
                return Json(new { });
            });
            var reading = System.Threading.Tasks.Task.Run(() => host.Worker.ReadMeter(new() { Tasks = [first] }));
            await opening.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var claim = typeof(GXAgentWorker).GetMethod("ClaimPolledTasksAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            try { await (System.Threading.Tasks.Task<GXAgentWorker.GXActionBlock?>)claim.Invoke(host.Worker, [CancellationToken.None])!; }
            finally { release.TrySetResult(); }
            await reading.WaitAsync(TimeSpan.FromSeconds(3));
            Check(completed.Contains(first.Id) && completed.Contains(second.Id), "A claimed task appended during failed setup was abandoned.");
        }
        finally { ProbeMedia.Opening = null; release.TrySetResult(); }
    }
    private static async System.Threading.Tasks.Task IpIdentifiedGateway()
    {
        var code = """
            using Gurux.Net;
            using Gurux.DLMS.AMI.Script;
            using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
            namespace Gurux.DLMS.AMI.GeneratedScript {
                public class GXAmiScript {
                    public GXAmiScript(IGXAmi ami) { }
                    public GXGateway Identify(string info, GXNet media, byte[] data) => new GXGateway { Id = System.Guid.NewGuid() };
                }
            }
            """;
        var errors = new List<Gurux.DLMS.AMI.Script.GXScriptException>();
        var assembly = Gurux.DLMS.AMI.Script.GXAmiScript.Generate(Gurux.DLMS.AMI.Shared.DTOs.Enums.ScriptLanguage.CSharp, Guid.NewGuid().ToString(), code, errors, null)
            ?? throw new Exception("Cannot compile gateway identification test: " + string.Join("; ", errors));
        var method = Guid.NewGuid(); int polls = 0;
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        using var settings = new Gurux.Net.GXNet(Gurux.Net.NetworkType.Tcp, port);
        await using var host = new Host((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/Script/List") {
                return System.Threading.Tasks.Task.FromResult(Json(new ListScriptsResponse
            {
                Scripts = [new() { Id = Guid.NewGuid(), ByteAssembly = assembly, CompilationDependencies = Gurux.DLMS.AMI.Script.GXScriptCompiler.ReadDependencies(assembly), Methods = [new() { Id = method, Name = "Identify" }] }]
            }));
            }
            if (request.RequestUri.AbsolutePath == "/api/Task/Next") {
                Interlocked.Increment(ref polls);
            }
            return System.Threading.Tasks.Task.FromResult(Json(new GetNextTaskResponse { Tasks = [] }));
        }, options => options.GatewaySettings = new() { Enabled = true, MediaSettings = settings.Settings, IdentifyWaitTime = 0, ScriptMethod = method });
        var service = host.Provider.GetRequiredService<Gurux.DLMS.AMI.Agent.Worker.AutoConnect.GXGatewayService>();
        await service.StartAsync(CancellationToken.None);
        using var client = new System.Net.Sockets.TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
        await client.GetStream().ReadAsync(new byte[1]).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Check(polls == 1, "IP identification disconnected the gateway before checking its queued tasks.");
        await service.StopAsync(CancellationToken.None);
    }
    private static async System.Threading.Tasks.Task WriteCompletionRetry()
    {
        var id = Guid.NewGuid(); var task = Item(id); int attempts = 0;
        task.TaskType = Gurux.DLMS.AMI.Shared.Enums.TaskType.Write;
        task.Object = new GXObject { Template = new GXObjectTemplate { ObjectType = 1, LogicalName = "0.0.96.1.0.255" } };
        task.Index = 2; task.Data = Gurux.DLMS.GXDLMSTranslator.ValueToXml((byte)42);
        var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = new Host(async (request, token) =>
        {
            if (request.Method == HttpMethod.Get) {
                return Json(new GetDeviceResponse { Item = Device(id) });
            }
            if (request.RequestUri!.AbsolutePath.Equals("/api/task/done", StringComparison.OrdinalIgnoreCase))
            {
                var body = await request.Content!.ReadFromJsonAsync<TaskDone>(token);
                var completion = body!.Tasks!.Single();
                Check(completion.Id == task.Id && completion.Start == task.Start && completion.Data == task.Data && completion.Result == null,
                    "Completion retry lost the original write claim or successful result.");
                if (Interlocked.Increment(ref attempts) == 1) {
                    throw new HttpRequestException("Response lost after committing the completion.");
                }
                acknowledged.TrySetResult();
            }
            return Json(new GetNextTaskResponse { Tasks = [] });
        });
        await host.Worker.ReadMeter(new() { Tasks = [task] });
        var media = ProbeMedia.Last!;
        await acknowledged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Check(attempts >= 2 && media.Writes == 1 && media.Opens == 1 && media.Disposals == 1,
            "A lost completion response caused the meter write to execute again.");
    }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value, value.GetType()) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, System.Threading.Tasks.Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private sealed class Host : IAsyncDisposable
    {
        internal readonly GXAgentWorker Worker;
        private readonly HttpClient http;
        internal readonly ServiceProvider Provider;
        private readonly GXAgentHostNotifications notifications = new();
        internal Host(Func<HttpRequestMessage, CancellationToken, System.Threading.Tasks.Task<HttpResponseMessage>> send, Action<GXAgentOptions>? configure = null)
        {
            http = new HttpClient(new Handler(send)); Worker = new GXAgentWorker(http);
            var services = new ServiceCollection().AddLogging(); Worker.ConfigureServices(services); Provider = services.BuildServiceProvider();
            var options = new GXAgentOptions { Id = Guid.NewGuid(), Address = "https://session.test/", Token = "test" };
            configure?.Invoke(options); Worker.Init(Provider, options, notifications);
        }
        public async ValueTask DisposeAsync() { await Worker.DisposeAsync(); await Provider.DisposeAsync(); http.Dispose(); notifications.Dispose(); }
    }
}
