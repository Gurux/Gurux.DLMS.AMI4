using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class InvocationTests
{
    public interface IService { }
    public sealed class Service : IService, IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() { Disposed = true; }
    }
    private sealed class TestModule : GXAmiModuleBase
    {
        public override string Id => "runtime-test";
        public override string Name => "Runtime test";
        public override string Description => "Invocation lifecycle test";
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddScoped<IService, Service>();
            builder.Export<IService>();
        }
    }
    private static GXModuleRuntime Create(ServiceProvider provider) => new(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
    private static Task Activate(GXModuleRuntime runtime) => runtime.ActivateAsync(new TestModule(), typeof(TestModule).Assembly.Location, "1.0.0", null);

    [TestMethod]
    public async Task MissingProviderReturnsFalseWithoutCallingCallback()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        Assert.IsFalse(await runtime.TryInvokeAsync<IService>((_, _) => throw new AssertFailedException()));
        var result = await runtime.TryInvokeAsync<IService, int>((_, _) => Task.FromResult(42));
        Assert.IsFalse(result.Invoked);
    }

    [TestMethod]
    public async Task CallbackUnavailableExceptionIsNeverSuppressed()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        var expected = new AmiModuleUnavailableException("callback failure");
        try
        {
            await runtime.TryInvokeAsync<IService>((_, _) => throw expected);
            Assert.Fail("Expected callback error.");
        }
        catch (AmiModuleUnavailableException error)
        {
            Assert.AreSame(expected, error);
        }
        Assert.IsTrue(runtime.IsAvailable<IService>());
    }

    [TestMethod]
    public async Task SelfStopDoesNotWaitForItsOwnLease()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        await runtime.InvokeAsync<IService>(async (_, _) =>
        {
            var state = await runtime.StopAsync("runtime-test", TimeSpan.FromSeconds(20)).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.AreEqual(AmiModuleState.Stopping, state);
        });
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync("runtime-test"));
    }

    [TestMethod]
    public async Task ShutdownSignalsAreIndependentAndScopesDrain()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        using var request = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AmiModuleInvocationContext? context = null;
        Service? scoped = null;
        var invocation = runtime.InvokeWithContextAsync<IService>(async (service, ctx) =>
        {
            context = ctx;
            scoped = (Service)service;
            entered.SetResult();
            await release.Task;
        }, request.Token);
        await entered.Task;
        Assert.AreEqual(AmiModuleState.Stopping, await runtime.StopAsync("runtime-test", TimeSpan.FromMilliseconds(20)));
        Assert.IsTrue(context!.StoppingToken.IsCancellationRequested);
        Assert.IsTrue(context.ForcedCancellationToken.IsCancellationRequested);
        Assert.IsFalse(context.RequestCancellationToken.IsCancellationRequested);
        Assert.IsFalse(scoped!.Disposed);
        request.Cancel();
        Assert.IsTrue(context.RequestCancellationToken.IsCancellationRequested);
        release.SetResult();
        await invocation;
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync("runtime-test"));
        Assert.IsTrue(scoped.Disposed);
    }

    [TestMethod]
    public async Task DownstreamFailureRetainsProviderAndOriginalException()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        var expected = new InvalidOperationException("downstream failure");
        try
        {
            await runtime.InvokeWithContextAsync<IService>((_, context) => context.RunDownstreamAsync(() => throw expected));
            Assert.Fail("Expected downstream error.");
        }
        catch (InvalidOperationException error)
        {
            Assert.AreSame(expected, error);
        }
        Assert.IsTrue(runtime.IsAvailable<IService>());
    }

    [TestMethod]
    public async Task CallerCancellationDoesNotSignalModuleShutdown()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        using var request = new CancellationTokenSource();
        await runtime.InvokeWithContextAsync<IService>((_, context) =>
        {
            request.Cancel();
            Assert.IsTrue(context.RequestCancellationToken.IsCancellationRequested);
            Assert.IsFalse(context.StoppingToken.IsCancellationRequested);
            Assert.IsFalse(context.ForcedCancellationToken.IsCancellationRequested);
            return Task.CompletedTask;
        }, request.Token);
        Assert.IsTrue(runtime.IsAvailable<IService>());
    }

    [TestMethod]
    public async Task StopRaceEitherRejectsCallbackOrDrainsIt()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        var calls = Enumerable.Range(0, 50).Select(_ => Task.Run(() => runtime.TryInvokeAsync<IService>(async (_, _) =>
        {
            Interlocked.Increment(ref started);
            await release.Task;
        }))).ToArray();
        var stop = runtime.StopAsync("runtime-test", TimeSpan.FromMilliseconds(20));
        release.SetResult();
        var results = await Task.WhenAll(calls);
        await stop;
        Assert.AreEqual(started, results.Count(result => result));
        Assert.IsFalse(await runtime.TryInvokeAsync<IService>((_, _) => throw new AssertFailedException()));
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync("runtime-test"));
    }

    [TestMethod]
    public async Task OptionalResultDistinguishesDefaultValueFromMissingProvider()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        var result = await runtime.TryInvokeAsync<IService, int>((_, _) => Task.FromResult(0));
        Assert.IsTrue(result.Invoked);
        Assert.AreEqual(0, result.Result);
    }

    [TestMethod]
    public async Task StopCancelsLegacyTokenButCancelledWaiterDoesNotAbandonForcedDeadline()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken legacy = default;
        AmiModuleInvocationContext? context = null;
        var contextCall = runtime.InvokeWithContextAsync<IService>(async (_, ctx) =>
        {
            context = ctx;
            await release.Task;
        });
        var legacyCall = runtime.InvokeAsync<IService>(async (_, token) =>
        {
            legacy = token;
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;
        using var waiter = new CancellationTokenSource();
        waiter.Cancel();
        try
        {
            await runtime.StopAsync("runtime-test", TimeSpan.FromMilliseconds(20), waiter.Token);
            Assert.Fail("Expected cancelled wait.");
        }
        catch (OperationCanceledException) { }
        Assert.IsTrue(legacy.IsCancellationRequested);
        var forced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = context!.ForcedCancellationToken.Register(() => forced.TrySetResult());
        await forced.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsFalse(context.RequestCancellationToken.IsCancellationRequested);
        release.SetResult();
        await Task.WhenAll(contextCall, legacyCall);
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync("runtime-test"));
    }

    [TestMethod]
    public async Task ProviderFaultSignalsForcedDeadlineAndDrainsSiblingCall()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Modules:StopTimeoutSeconds"] = "0"
        }).Build();
        await using var runtime = new GXModuleRuntime(provider, configuration, NullLogger<GXModuleRuntime>.Instance);
        await Activate(runtime);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var forced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Service? scoped = null;
        var sibling = runtime.InvokeWithContextAsync<IService>(async (service, context) =>
        {
            scoped = (Service)service;
            using var registration = context.ForcedCancellationToken.Register(() => forced.TrySetResult());
            await Task.WhenAny(forced.Task, release.Task);
        });
        try
        {
            try
            {
                await runtime.InvokeAsync<IService>((_, _) => throw new InvalidOperationException("provider failure"));
                Assert.Fail("Expected provider error.");
            }
            catch (InvalidOperationException) { }
            await forced.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
            await sibling.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsTrue(scoped!.Disposed);
            Assert.IsFalse(runtime.IsAvailable<IService>());
        }
        finally
        {
            release.TrySetResult();
            await sibling;
        }
        Assert.AreEqual(AmiModuleState.Failed, await runtime.StopAsync("runtime-test", TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public async Task CallbackBusinessFailureFaultsProvider()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = Create(provider);
        await Activate(runtime);
        try
        {
            await runtime.TryInvokeAsync<IService>((_, _) => throw new InvalidOperationException("provider failure"));
            Assert.Fail("Expected provider error.");
        }
        catch (InvalidOperationException) { }
        Assert.IsFalse(runtime.IsAvailable<IService>());
        Assert.AreEqual(AmiModuleState.Failed, await runtime.StopAsync("runtime-test"));
    }
}
