using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class EventRuntimeTests
{
    private sealed class EventLifecycleProbe : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class EventLifecycleModule(string id, Func<AmiModuleContext, int, CancellationToken, Task> handler) : GXAmiModuleBase
    {
        public override string Id => id;
        public override string Name => id;
        public override string Description => "Event lifecycle integration test";
        public EventLifecycleProbe? Probe { get; private set; }
        public bool StopCalled { get; private set; }

        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddScoped<EventLifecycleProbe>();
        }

        public override Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            Probe = context.Services.GetRequiredService<EventLifecycleProbe>();
            context.Events.Subscribe<int>((value, token) => handler(context, value, token));
            return Task.CompletedTask;
        }

        public override Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            StopCalled = true;
            Assert.IsFalse(Probe!.Disposed);
            return Task.CompletedTask;
        }
    }

    private static GXModuleRuntime CreateEventRuntime(ServiceProvider provider) =>
        new(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);

    private static Task ActivateEventModule(GXModuleRuntime runtime, EventLifecycleModule module) =>
        runtime.ActivateAsync(module, typeof(EventLifecycleModule).Assembly.Location, "1.0.0", null);

    [TestMethod]
    public async Task StopAndReloadDetachThePreviousGeneration()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = CreateEventRuntime(provider);
        int oldCalls = 0;
        int newCalls = 0;
        var first = new EventLifecycleModule("event-reload", (_, _, _) => { ++oldCalls; return Task.CompletedTask; });
        await ActivateEventModule(runtime, first);
        await runtime.Events.PublishAsync(1);
        Assert.AreEqual(1, oldCalls);
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync(first.Id));
        await runtime.Events.PublishAsync(2);
        var second = new EventLifecycleModule(first.Id, (_, _, _) => { ++newCalls; return Task.CompletedTask; });
        await ActivateEventModule(runtime, second);
        await runtime.Events.PublishAsync(3);
        Assert.AreEqual(1, oldCalls);
        Assert.AreEqual(1, newCalls);
        Assert.IsTrue(first.Probe!.Disposed);
    }

    [TestMethod]
    public async Task ActiveEventLeaseDelaysStopAndLifecycleScopeDisposal()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = CreateEventRuntime(provider);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var module = new EventLifecycleModule("event-drain", async (context, _, _) =>
        {
            entered.SetResult();
            await release.Task;
            Assert.IsFalse(context.Services.GetRequiredService<EventLifecycleProbe>().Disposed);
        });
        await ActivateEventModule(runtime, module);
        var publication = runtime.Events.PublishAsync(1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.AreEqual(AmiModuleState.Stopping, await runtime.StopAsync(module.Id, TimeSpan.FromMilliseconds(25)));
            Assert.IsFalse(module.StopCalled);
            Assert.IsFalse(module.Probe!.Disposed);
        }
        finally
        {
            release.TrySetResult();
        }
        await publication.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync(module.Id));
        Assert.IsTrue(module.StopCalled);
        Assert.IsTrue(module.Probe!.Disposed);
    }

    [TestMethod]
    public async Task QueuedSnapshotCannotEnterStoppedOrReplacementGeneration()
    {
        await using var hub = new AmiEventHub();
        await using var provider = new ServiceCollection().AddSingleton(hub).BuildServiceProvider();
        await using var runtime = CreateEventRuntime(provider);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var blocker = hub.Subscribe<int>(async (value, _) =>
        {
            if (value == 1)
            {
                entered.SetResult();
                await release.Task;
            }
        });
        int oldCalls = 0;
        int newCalls = 0;
        var first = new EventLifecycleModule("event-queued", (_, _, _) => { ++oldCalls; return Task.CompletedTask; });
        await ActivateEventModule(runtime, first);
        var publication = hub.PublishAsync(1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync(first.Id));
            await ActivateEventModule(runtime, new EventLifecycleModule(first.Id, (_, _, _) => { ++newCalls; return Task.CompletedTask; }));
        }
        finally
        {
            release.TrySetResult();
        }
        await publication.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, oldCalls);
        Assert.AreEqual(0, newCalls);
        await hub.PublishAsync(2);
        Assert.AreEqual(1, newCalls);
    }

    [TestMethod]
    public async Task HandlerFailureFaultsOnlyItsModuleAndPreservesOtherSubscribers()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = CreateEventRuntime(provider);
        var failing = new EventLifecycleModule("event-failing", (_, _, _) => throw new InvalidOperationException("event handler failed"));
        int moduleCalls = 0;
        int hostCalls = 0;
        var healthy = new EventLifecycleModule("event-healthy", (_, _, _) => { ++moduleCalls; return Task.CompletedTask; });
        await ActivateEventModule(runtime, failing);
        await ActivateEventModule(runtime, healthy);
        using var hostSubscription = runtime.Events.Subscribe<int>((_, _) => { ++hostCalls; return Task.CompletedTask; });
        await runtime.Events.PublishAsync(1);
        Assert.AreEqual(AmiModuleState.Failed, runtime.GetState(failing.Id));
        Assert.AreEqual(AmiModuleState.Active, runtime.GetState(healthy.Id));
        Assert.AreEqual(1, moduleCalls);
        Assert.AreEqual(1, hostCalls);
        Assert.AreEqual(AmiModuleState.Failed, await runtime.StopAsync(failing.Id));
        await runtime.Events.PublishAsync(2);
        Assert.AreEqual(2, moduleCalls);
        Assert.AreEqual(2, hostCalls);
    }

    [TestMethod]
    public async Task HandlerCanStopItsOwnModuleWithoutWaitingForItsLease()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = CreateEventRuntime(provider);
        AmiModuleState? selfStopResult = null;
        var module = new EventLifecycleModule("event-self-stop", async (_, _, _) =>
        {
            selfStopResult = await runtime.StopAsync("event-self-stop", TimeSpan.FromSeconds(20)).WaitAsync(TimeSpan.FromSeconds(2));
        });
        await ActivateEventModule(runtime, module);
        await runtime.Events.PublishAsync(1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(AmiModuleState.Stopping, selfStopResult);
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync(module.Id));
        Assert.IsTrue(module.Probe!.Disposed);
    }
}
