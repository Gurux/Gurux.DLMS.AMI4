using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Loader;

const string source = """
using System;
using System.Threading;
using System.Threading.Tasks;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
namespace Gurux.DLMS.AMI.GeneratedScript
{
    public class GXAmiScript
    {
        private readonly IGXAmi ami;
        public GXAmiScript(IGXAmi ami) { this.ami = ami; }
        public ValueTask<int> Read(TaskCompletionSource<int> result) { return new ValueTask<int>(result.Task); }
        public int Blocking(ManualResetEventSlim entered, ManualResetEventSlim release)
        {
            entered.Set();
            release.Wait();
            return 7;
        }
        public async Task<int> Callback(ManualResetEventSlim entered, ManualResetEventSlim callbackEntered, ManualResetEventSlim callbackRelease)
        {
            ami.CancellationToken.Register(() =>
            {
                callbackEntered.Set();
                callbackRelease.Wait();
            });
            entered.Set();
            await Task.Delay(Timeout.Infinite, ami.CancellationToken);
            return 1;
        }
        public async Task<int> Cancel(ManualResetEventSlim entered, ManualResetEventSlim stopped, ManualResetEventSlim completionRelease)
        {
            entered.Set();
            try
            {
                await Task.Delay(Timeout.Infinite, ami.CancellationToken);
            }
            finally
            {
                stopped.Set();
                completionRelease.Wait();
            }
            return 1;
        }
    }
}
""";
var errors = new List<GXScriptException>();
var bytes = GXAmiScript.Generate(ScriptLanguage.CSharp, "ExecutionChecks" + Guid.NewGuid().ToString("N"), source, errors, null)
    ?? throw new Exception(string.Join("; ", errors.Select(e => e.Message)));
using var services = new ServiceCollection().BuildServiceProvider();
using var http = new HttpClient();
var scripts = new GXAgentScripts(http, new GXScriptExecutionOptions { ExecutionTimeout = TimeSpan.FromMilliseconds(500) });
GXScriptRunArgs Args(string name, params object?[] parameters)
{
    var context = new AssemblyLoadContext("execution-check", true);
    using var stream = new MemoryStream(bytes, false);
    context.LoadFromStream(stream);
    return new GXScriptRunArgs
    {
        ByteAssembly = bytes,
        MethodName = name,
        Parameters = parameters,
        AssemblyLoadContext = context
    };
}
var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
var valueArgs = Args("Read", completion);
var value = scripts.RunPreparedAsync(new GXAmiScript(services), valueArgs);
if (value.IsCompleted)
{
    throw new Exception("ValueTask<T> completed before its asynchronous value.");
}
completion.SetResult(42);
if (!Equals(await value, 42))
{
    throw new Exception("ValueTask<T> result was not unwrapped.");
}
scripts.ReleaseRunArgs(valueArgs);
Console.WriteLine("PASS asynchronous ValueTask<T> result is awaited and unwrapped");

using var entered = new ManualResetEventSlim();
using var release = new ManualResetEventSlim();
var blockingArgs = Args("Blocking", entered, release);
var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
blockingArgs.AssemblyLoadContext!.Unloading += _ => unloaded.TrySetResult();
var blocking = scripts.RunPreparedAsync(new GXAmiScript(services), blockingArgs);
if (!await WaitSignalAsync(entered))
{
    throw new Exception("Blocking script did not start.");
}
try
{
    await blocking;
    throw new Exception("Synchronous script did not time out.");
}
catch (TimeoutException)
{
}
scripts.ReleaseRunArgs(blockingArgs);
if (unloaded.Task.IsCompleted)
{
    throw new Exception("Still-running script context was unloaded.");
}
var blockedArgs = Args("Read", new TaskCompletionSource<int>());
try
{
    await scripts.RunPreparedAsync(new GXAmiScript(services), blockedArgs);
    throw new Exception("New script admitted while timed-out code remained active.");
}
catch (InvalidOperationException)
{
}
scripts.ReleaseRunArgs(blockedArgs);
release.Set();
await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
await WaitUntilAsync(() => scripts.CanExecute, "Script cleanup did not release execution ownership.");
Console.WriteLine("PASS synchronous entry deadline, fail-closed admission and deferred unload");

using var cancel = new CancellationTokenSource();
using var cancelEntered = new ManualResetEventSlim();
using var cancelStopped = new ManualResetEventSlim();
using var cancelCompletionRelease = new ManualResetEventSlim();
var cancelArgs = Args("Cancel", cancelEntered, cancelStopped, cancelCompletionRelease);
var cancelled = scripts.RunPreparedAsync(new GXAmiScript(services), cancelArgs, cancel.Token);
if (!await WaitSignalAsync(cancelEntered))
{
    throw new Exception("Cooperative script did not start.");
}
cancel.Cancel();
try
{
    await cancelled;
    throw new Exception("Script cancellation was not propagated.");
}
catch (OperationCanceledException)
{
}
scripts.ReleaseRunArgs(cancelArgs);
try
{
    await WaitUntilAsync(() => cancelStopped.IsSet, "Script did not receive cooperative cancellation.");
    if (scripts.CanExecute)
    {
        throw new Exception("Cancellation released admission before the script finally block finished.");
    }
}
finally
{
    cancelCompletionRelease.Set();
}
await WaitUntilAsync(() => scripts.CanExecute, "Cancelled script did not release execution ownership.");
Console.WriteLine("PASS script cancellation reaches cooperative runner");

try
{
    new GXScriptExecutionOptions { ExecutionTimeout = TimeSpan.Zero }.Validate();
    throw new Exception("Zero execution timeout was accepted.");
}
catch (ArgumentException)
{
}
Console.WriteLine("PASS invalid execution timeout rejected");


// Cancellation callbacks can outlive the script body and retain ownership as well.
using var callbackStarted = new ManualResetEventSlim();
using var callbackEntered = new ManualResetEventSlim();
using var callbackRelease = new ManualResetEventSlim();
using var callbackCancellation = new CancellationTokenSource();
var callbackArgs = Args("Callback", callbackStarted, callbackEntered, callbackRelease);
var callbackWork = scripts.RunPreparedAsync(new GXAmiScript(services), callbackArgs, callbackCancellation.Token);
if (!await WaitSignalAsync(callbackStarted))
{
    throw new Exception("Callback script did not start.");
}
callbackCancellation.Cancel();
try
{
    await callbackWork;
    throw new Exception("Callback script cancellation was not bounded.");
}
catch (OperationCanceledException)
{
}
if (!await WaitSignalAsync(callbackEntered) || scripts.CanExecute)
{
    callbackRelease.Set();
    throw new Exception("Blocked cancellation callback did not retain execution ownership.");
}
scripts.ReleaseRunArgs(callbackArgs);
callbackRelease.Set();
using (var callbackDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    while (!scripts.CanExecute)
    {
        await Task.Delay(10, callbackDeadline.Token);
    }
}
Console.WriteLine("PASS cancellation callbacks retain ownership until actual completion");
// Unloading callbacks are user code too and must not hold the execution gate or caller.
using var unloadEntered = new ManualResetEventSlim();
using var unloadRelease = new ManualResetEventSlim();
var cleanupArgs = Args("Read", new TaskCompletionSource<int>());
cleanupArgs.AssemblyLoadContext!.Unloading += _ =>
{
    unloadEntered.Set();
    unloadRelease.Wait();
};
var releaseCall = Task.Run(() => scripts.ReleaseRunArgs(cleanupArgs));
if (!await WaitSignalAsync(unloadEntered))
{
    throw new Exception("Cleanup callback did not start.");
}
if (await Task.WhenAny(releaseCall, Task.Delay(150)) != releaseCall)
{
    unloadRelease.Set();
    await releaseCall;
    throw new Exception("Blocking unloading callback blocked ReleaseRunArgs.");
}
using (var cleanupDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    while (scripts.CanExecute)
    {
        await Task.Delay(10, cleanupDeadline.Token);
    }
}
unloadRelease.Set();
using (var cleanupDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    while (!scripts.CanExecute)
    {
        await Task.Delay(10, cleanupDeadline.Token);
    }
}
Console.WriteLine("PASS cleanup callback deadline and execution gate remain bounded");
// Paused scripts must not consume raw notification lease attempts.
var admissionChecked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
bool admitted = false;
using var transport = new ProbeHandler();
using var rawHttp = new HttpClient(transport) { BaseAddress = new Uri("https://ami.test") };
await using (var raw = new Gurux.DLMS.AMI.Agent.Worker.Notifier.GXRawNotificationHandler(rawHttp,
    new GXAgentNotification(), Guid.NewGuid(), 1, (_, _) => Task.CompletedTask,
    Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, canProcess: () =>
    {
        admissionChecked.TrySetResult();
        return Volatile.Read(ref admitted);
    }))
{
    await raw.StartAsync(CancellationToken.None);
    await admissionChecked.Task.WaitAsync(TimeSpan.FromSeconds(5));
    if (transport.Calls != 0)
    {
        throw new Exception("Raw notification lease claimed while scripts were paused.");
    }
    Volatile.Write(ref admitted, true);
    await transport.Called.Task.WaitAsync(TimeSpan.FromSeconds(5));
}
Console.WriteLine("PASS raw claims pause and resume with script admission");

static async Task<bool> WaitSignalAsync(ManualResetEventSlim signal)
{
    try
    {
        await WaitUntilAsync(() => signal.IsSet, "Script test signal was not set.");
        return true;
    }
    catch (TimeoutException)
    {
        return false;
    }
}

static async Task WaitUntilAsync(Func<bool> ready, string message)
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
        while (!ready())
        {
            await Task.Delay(10, deadline.Token);
        }
    }
    catch (OperationCanceledException) when (deadline.IsCancellationRequested)
    {
        throw new TimeoutException(message);
    }
}
sealed class ProbeHandler : HttpMessageHandler
{
    internal int Calls;
    internal readonly TaskCompletionSource Called = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Interlocked.Increment(ref Calls);
        Called.TrySetResult();
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { RawNotifications = Array.Empty<object>() })
        });
    }
}
