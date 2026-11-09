using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public class AmiEventHubTests
{
    [TestMethod]
    public async Task DeliveryUsesExactTypeAndIsolatesFailures()
    {
        int delivered = 0;
        int errors = 0;
        await using var hub = new AmiEventHub(_ => { ++errors; throw new Exception(); });
        using var other = hub.Subscribe<object>((_, _) => throw new AssertFailedException());
        using var failing = hub.Subscribe<string>((_, _) => throw new InvalidOperationException());
        using var working = hub.Subscribe<string>((value, _) => { delivered += value.Length; return Task.CompletedTask; });
        await hub.PublishAsync("abc", CancellationToken.None);
        Assert.AreEqual(3, delivered);
        Assert.AreEqual(1, errors);
    }

    [TestMethod]
    public async Task CancellationPropagatesAndPreventsLaterDelivery()
    {
        await using var hub = new AmiEventHub();
        using var cancellation = new CancellationTokenSource();
        using var first = hub.Subscribe<int>((_, token) => { Assert.AreEqual(cancellation.Token, token); cancellation.Cancel(); return Task.CompletedTask; });
        using var second = hub.Subscribe<int>((_, _) => throw new AssertFailedException());
        await Assert.ThrowsAsync<OperationCanceledException>(() => hub.PublishAsync(1, cancellation.Token));
    }

    [TestMethod]
    public async Task DetachmentPreventsSnapshotCallbackAndAsyncDisposalDrains()
    {
        await using var hub = new AmiEventHub();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = hub.Subscribe<int>(async (_, _) => { entered.SetResult(); await release.Task; });
        int later = 0;
        using var second = hub.Subscribe<int>((_, _) => { ++later; return Task.CompletedTask; });
        var publication = hub.PublishAsync(1, CancellationToken.None);
        await entered.Task;
        second.Dispose();
        var draining = ((IAsyncDisposable)first).DisposeAsync().AsTask();
        Assert.IsFalse(draining.IsCompleted);
        release.SetResult();
        await Task.WhenAll(publication, draining);
        await hub.PublishAsync(2, CancellationToken.None);
        Assert.AreEqual(0, later);
    }

    [TestMethod]
    public async Task ReentrantPublishAndSelfDisposalDoNotDeadlock()
    {
        await using var hub = new AmiEventHub();
        IDisposable? registration = null;
        int calls = 0;
        registration = hub.Subscribe<int>(async (value, token) =>
        {
            ++calls;
            if (value == 1)
            {
                await hub.PublishAsync(2, token);
                await ((IAsyncDisposable)registration!).DisposeAsync();
            }
        });
        await hub.PublishAsync(1, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        await hub.PublishAsync(3, CancellationToken.None);
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public async Task HubDisposalDrainsAndRejectsNewSubscriptions()
    {
        var hub = new AmiEventHub();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.Subscribe<int>(async (_, _) => { entered.SetResult(); await release.Task; });
        var publication = hub.PublishAsync(1, CancellationToken.None);
        await entered.Task;
        var disposal = hub.DisposeAsync().AsTask();
        Assert.IsFalse(disposal.IsCompleted);
        release.SetResult();
        await Task.WhenAll(disposal, publication);
        await hub.PublishAsync(2, CancellationToken.None);
        Assert.Throws<ObjectDisposedException>(() => hub.Subscribe<int>((_, _) => Task.CompletedTask));
    }
    [TestMethod]
    public async Task SelfHubDisposalDoesNotDeadlock()
    {
        var hub = new AmiEventHub();
        int later = 0;
        hub.Subscribe<int>(async (_, _) => await hub.DisposeAsync());
        hub.Subscribe<int>((_, _) => { ++later; return Task.CompletedTask; });
        await hub.PublishAsync(1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, later);
        await hub.DisposeAsync();
    }

    [TestMethod]
    public async Task ConcurrentPublicationsCanDrainAfterDetachment()
    {
        await using var hub = new AmiEventHub();
        int calls = 0;
        var registration = hub.Subscribe<int>(async (_, _) => { Interlocked.Increment(ref calls); await Task.Yield(); });
        var publications = Enumerable.Range(0, 100).Select(value => Task.Run(() => hub.PublishAsync(value))).ToArray();
        await ((IAsyncDisposable)registration).DisposeAsync();
        int drainedCalls = Volatile.Read(ref calls);
        await Task.WhenAll(publications);
        await hub.PublishAsync(200);
        Assert.AreEqual(drainedCalls, calls);
    }

    [TestMethod]
    public async Task DetachedRegistrationAndHubDoNotRetainHandlerOrPayload()
    {
        await using var hub = new AmiEventHub();
        var (registration, target, payload) = PublishAndDetach(hub);
        for (int count = 0; count != 3; ++count)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Assert.IsFalse(target.IsAlive, "Detached handler target must be collectible even while its registration remains alive.");
        Assert.IsFalse(payload.IsAlive, "The hub must not retain published values.");
        GC.KeepAlive(registration);
        GC.KeepAlive(hub);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (IDisposable, WeakReference, WeakReference) PublishAndDetach(AmiEventHub hub)
    {
        var target = new HandlerTarget();
        var payload = new object();
        var registration = hub.Subscribe<object>(target.Handle);
        hub.PublishAsync(payload).GetAwaiter().GetResult();
        registration.Dispose();
        return (registration, new WeakReference(target), new WeakReference(payload));
    }

    private sealed class HandlerTarget
    {
        public Task Handle(object value, CancellationToken token) => Task.CompletedTask;
    }

    [TestMethod]
    public async Task DefaultEventsAreSafeAndHonorCancellation()
    {
        using var registration = AmiEventHub.None.Subscribe<int>((_, _) => throw new AssertFailedException());
        await AmiEventHub.None.PublishAsync(1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => AmiEventHub.None.PublishAsync(1, cancellation.Token));
    }
}
