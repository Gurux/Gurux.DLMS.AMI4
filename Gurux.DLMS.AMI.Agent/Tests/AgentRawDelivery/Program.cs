using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Diagnostics.Metrics;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker.Notifier;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.Logging.Abstractions;

var root = Path.Combine(Path.GetTempPath(), "gurux-raw-delivery-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var id = Guid.NewGuid();
    var attempted = new TaskCompletionSource<GXRawNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
    using var offline = new HttpClient(new Handler(async (request, token) =>
    {
        var record = (await request.Content!.ReadFromJsonAsync<AddRawNotification>(token))!.RawNotifications!.Single();
        attempted.TrySetResult(record);
        throw new HttpRequestException("Response lost after accepted upload");
    })) { BaseAddress = new Uri("https://raw.test/") };
    var directory = Path.Combine(root, "recovery");
    var first = Create(offline, id, directory);
    first.Receive("meter:123", [1, 2, 3], false);
    var original = await attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    await first.DisposeAsync();
    if (!Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).Any())
    {
        throw new Exception("Receive returned without durable transport storage.");
    }
    var delivered = new List<GXRawNotification>();
    using var online = new HttpClient(new Handler(async (request, token) =>
    {
        var record = (await request.Content!.ReadFromJsonAsync<AddRawNotification>(token))!.RawNotifications!.Single();
        lock (delivered)
        {
            delivered.Add(record);
        }
        return new HttpResponseMessage(HttpStatusCode.OK);
    })) { BaseAddress = new Uri("https://raw.test/") };
    await using (var second = Create(online, id, directory))
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await second.CompleteAsync(deadline.Token);
    }
    if (delivered.Count != 2 || delivered[0].Id != original.Id || !delivered[0].Value!.SequenceEqual(new byte[] { 1, 2, 3 }) ||
        delivered[1].ConnectionId != original.ConnectionId || delivered[1].SequenceNumber != 1 || !delivered[1].ConnectionClosed)
    {
        throw new Exception("Restart did not preserve identity/content/order and recover the unfinished stream end.");
    }
    Console.WriteLine("PASS durable receive, lost response retry, stable identity, restart and recovered end marker");

    directory = Path.Combine(root, "permanent");
    var received = new List<GXRawNotification>();
    using var reject = new HttpClient(new Handler(async (request, token) =>
    {
        var record = (await request.Content!.ReadFromJsonAsync<AddRawNotification>(token))!.RawNotifications!.Single();
        lock (received)
        {
            received.Add(record);
        }
        return new HttpResponseMessage(record.Value?.FirstOrDefault() == 9 ? HttpStatusCode.BadRequest : HttpStatusCode.OK);
    })) { BaseAddress = new Uri("https://raw.test/") };
    await using (var sender = Create(reject, id, directory))
    {
        sender.Receive("bad", [9], false);
        sender.Receive("bad", [8], false);
        sender.Close("bad");
        sender.Receive("good", [7], true);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await sender.CompleteAsync(deadline.Token);
    }
    if (!received.Any(n => n.Value?.FirstOrDefault() == 7) || received.Any(n => n.Value?.FirstOrDefault() == 8) ||
        !Directory.Exists(Path.Combine(directory, "quarantine")))
    {
        throw new Exception("Permanent rejection blocked unrelated connections or delivered a partial failed stream.");
    }
    Console.WriteLine("PASS permanent rejection quarantines a stream and allows unrelated delivery");

    directory = Path.Combine(root, "corruption");
    var corrupt = Create(offline, id, directory);
    corrupt.Receive("corrupt", [4], true);
    await corrupt.DisposeAsync();
    var chunkFile = Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).Single(p => Path.GetFileName(p) != "state.json");
    File.WriteAllText(chunkFile, "broken json");
    await using (var recovered = Create(online, id, directory))
    {
        await recovered.CompleteAsync(CancellationToken.None);
    }
    if (!Directory.EnumerateFiles(Path.Combine(directory, "quarantine"), "*.json", SearchOption.AllDirectories).Any())
    {
        throw new Exception("Corrupt record was not quarantined.");
    }
    Console.WriteLine("PASS corrupt storage is quarantined without stopping other delivery");

    directory = Path.Combine(root, "throttle");
    var throttled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var unrelated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    int throttleAttempts = 0;
    using var throttling = new HttpClient(new Handler(async (request, token) =>
    {
        var record = (await request.Content!.ReadFromJsonAsync<AddRawNotification>(token))!.RawNotifications!.Single();
        if (record.Value?.FirstOrDefault() == 5)
        {
            Interlocked.Increment(ref throttleAttempts);
            throttled.TrySetResult();
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return response;
        }
        unrelated.TrySetResult();
        return new HttpResponseMessage(HttpStatusCode.OK);
    })) { BaseAddress = new Uri("https://raw.test/") };
    var observed = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();
    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, meterListener) =>
    {
        if (instrument.Meter.Name == "Gurux.DLMS.AMI.Agent.RawDelivery")
        {
            meterListener.EnableMeasurementEvents(instrument);
        }
    };
    listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) => observed[instrument.Name] = measurement);
    listener.Start();
    var budget = new GXRawDeliveryOptions { ShutdownFlushBudget = TimeSpan.FromMilliseconds(80) };
    await using (var sender = Create(throttling, id, directory, budget))
    {
        sender.Receive("throttle", [5], true);
        sender.Receive("unrelated", [6], true);
        await throttled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await unrelated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(300);
        listener.RecordObservableInstruments();
        if (throttleAttempts != 1 || !observed.TryGetValue("agent.raw.pending", out var pending) || pending != 1 ||
            !observed.TryGetValue("agent.raw.retries", out var retryCount) || retryCount < 1)
        {
            throw new Exception("Retry-After or raw queue metrics were not preserved.");
        }
        await sender.CompleteAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }
    if (!Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).Any())
    {
        throw new Exception("Bounded shutdown lost throttled records.");
    }
    Console.WriteLine("PASS Retry-After, unrelated stream fairness, queue metrics and bounded durable shutdown");

    directory = Path.Combine(root, "capacity");
    await using (var sender = Create(offline, id, directory, new GXRawDeliveryOptions { MaxPendingRecords = 1 }))
    {
        sender.Receive("first", [1], false);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocked = Task.Run(() =>
        {
            entered.SetResult();
            try
            {
                sender.Receive("first", [2], false);
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        });
        await entered.Task;
        await Task.Delay(100);
        if (blocked.IsCompleted)
        {
            throw new Exception("Storage limits did not apply backpressure.");
        }
        var closing = Task.Run(() => sender.Close("first"));
        await Task.Delay(100);
        if (closing.IsCompleted)
        {
            throw new Exception("Stream closure overtook an earlier storage-blocked chunk.");
        }
        sender.StopIntake();
        if (!await blocked.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            throw new Exception("Stopped intake acknowledged a record it could not persist.");
        }
        await closing.WaitAsync(TimeSpan.FromSeconds(2));
    }
    Console.WriteLine("PASS storage backpressure is released before transport shutdown");

    directory = Path.Combine(root, "gap");
    var gap = Create(offline, id, directory);
    gap.Receive("gap", [1], false);
    gap.Receive("gap", [2], false);
    gap.Receive("gap", [3], false);
    await gap.DisposeAsync();
    var files = Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
        .Where(p => Path.GetFileName(p) != "state.json").Order(StringComparer.Ordinal).ToArray();
    File.Delete(files[1]);
    int beforeGap = delivered.Count;
    await using (var recovered = Create(online, id, directory))
    {
        await recovered.CompleteAsync(CancellationToken.None);
    }
    if (delivered.Count != beforeGap || !Directory.Exists(Path.Combine(directory, "quarantine")))
    {
        throw new Exception("A gapped stored stream was delivered instead of quarantined.");
    }
    Console.WriteLine("PASS gapped stream storage is quarantined before upload");

    directory = Path.Combine(root, "timeout");
    var healthy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var hanging = new HttpClient(new Handler(async (request, token) =>
    {
        var record = (await request.Content!.ReadFromJsonAsync<AddRawNotification>(token))!.RawNotifications!.Single();
        if (record.Value?.FirstOrDefault() == 10)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }
        healthy.TrySetResult();
        return new HttpResponseMessage(HttpStatusCode.OK);
    })) { BaseAddress = new Uri("https://raw.test/") };
    await using (var sender = Create(hanging, id, directory, new GXRawDeliveryOptions
    {
        RequestTimeout = TimeSpan.FromMilliseconds(80), ShutdownFlushBudget = TimeSpan.FromMilliseconds(80)
    }))
    {
        sender.Receive("hang", [10], true);
        sender.Receive("healthy", [11], true);
        await healthy.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await sender.CompleteAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
    }
    Console.WriteLine("PASS request deadlines retain failed data and release unrelated delivery");
}
finally
{
    Directory.Delete(root, true);
}

static GXRawNotificationSender Create(HttpClient http, Guid agentId, string directory, GXRawDeliveryOptions? options = null)
{
    var constructor = typeof(GXRawNotificationSender).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
        .SingleOrDefault(c => c.GetParameters().Length == 6)
        ?? throw new Exception("Raw sender has no durable storage constructor.");
    return (GXRawNotificationSender)constructor.Invoke([http, agentId, 0, NullLogger.Instance, directory, options]);
}

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
}
