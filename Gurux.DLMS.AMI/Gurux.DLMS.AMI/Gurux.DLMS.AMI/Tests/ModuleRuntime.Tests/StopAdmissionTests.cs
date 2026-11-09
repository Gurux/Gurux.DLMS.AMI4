using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public class StopAdmissionTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public async Task StopDuringScopePreparationRejectsBeforeCallbackButPreservesCallerCancellation(bool withContext, bool cancelCaller)
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var gated = new GatedProvider(services);
        await using var runtime = new GXModuleRuntime(gated, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        await runtime.ActivateAsync(new ProbeModule(), typeof(ProbeModule).Assembly.Location, "1.0.0", null);
        using var caller = new CancellationTokenSource();
        gated.Arm();
        var calls = 0;
        var invocation = Task.Run(() => withContext
            ? runtime.TryInvokeWithContextAsync<IProbe>((_, _) => { ++calls; return Task.CompletedTask; }, caller.Token)
            : runtime.TryInvokeAsync<IProbe>((_, _) => { ++calls; return Task.CompletedTask; }, caller.Token));
        try
        {
            await gated.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(AmiModuleState.Stopping, await runtime.StopAsync("Admission.Probe", TimeSpan.Zero));
            if (cancelCaller)
            {
                caller.Cancel();
            }
        }
        finally
        {
            gated.Release.TrySetResult();
        }
        if (cancelCaller)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => invocation);
        }
        else
        {
            Assert.IsFalse(await invocation);
        }
        Assert.AreEqual(0, calls);
        Assert.AreEqual(AmiModuleState.Stopped, await runtime.StopAsync("Admission.Probe"));
    }

    public interface IProbe { }
    public sealed class Probe : IProbe { }
    private sealed class ProbeModule : GXAmiModuleBase
    {
        public override string Id => "Admission.Probe";
        public override string Name => "Probe";
        public override string Description => "Deterministic admission race fixture.";
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddScoped<IProbe, Probe>();
            builder.Export<IProbe>();
        }
    }
    private sealed class GatedProvider(IServiceProvider inner) : IServiceProvider, IServiceScopeFactory
    {
        private int armed;
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm() => Interlocked.Exchange(ref armed, 1);
        public object? GetService(Type serviceType) => serviceType == typeof(IServiceScopeFactory)
            ? this : inner.GetService(serviceType);
        public IServiceScope CreateScope()
        {
            if (Interlocked.Exchange(ref armed, 0) == 1)
            {
                Entered.TrySetResult();
                Release.Task.GetAwaiter().GetResult();
            }
            return inner.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
