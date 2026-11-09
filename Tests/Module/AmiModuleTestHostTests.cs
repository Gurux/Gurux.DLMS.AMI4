using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Testing.Tests;

[TestClass]
public sealed class AmiModuleTestHostTests
{
    [TestMethod]
    public async Task RealRuntimeOwnsActivationReloadAndDisposalWithoutHttpOrDatabase()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["test"] = "configured" }).Build();
        var marker = new Marker();
        var module = new ProbeModule();
        await using (var host = new AmiModuleTestHost(services => services.AddSingleton(marker), configuration))
        {
            Assert.AreSame(configuration, host.Configuration);
            Assert.AreSame(marker, host.HostServices.GetRequiredService<Marker>());
            Assert.AreSame(host.Runtime, host.Dispatcher);
            var manifest = await host.ActivateAsync(module);
            Assert.AreEqual(module.Id, manifest.Id);
            Assert.IsNotNull(host.GetManifest(module.Id));
            Assert.AreEqual(1, module.Starts);
            await host.ReloadAsync(module);
            Assert.AreEqual(2, module.Starts);
            Assert.AreEqual(1, module.Stops);
            Assert.AreEqual(AmiModuleState.Stopped, await host.StopAsync(module.Id));
            Assert.AreEqual(2, module.Stops);
            await host.ActivateAsync(module);
        }
        Assert.AreEqual(3, module.Starts);
        Assert.AreEqual(3, module.Stops);
    }

    [TestMethod]
    public async Task HostEventsUseConfiguredSharedHub()
    {
        await using var host = new AmiModuleTestHost();
        var received = 0;
        using var subscription = host.Events.Subscribe<int>((value, _) =>
        {
            received += value;
            return Task.CompletedTask;
        });
        await host.HostServices.GetRequiredService<IGXAmiEvents>().PublishAsync(7);
        Assert.AreEqual(7, received);
    }

    [TestMethod]
    public async Task TestHostValidatesRequiredProvidersAndProtectsActiveDependents()
    {
        await using var host = new AmiModuleTestHost();
        var provider = new DependencyModule("Testing.Provider");
        var consumer = new DependencyModule("Testing.Consumer", provider.Id);
        await Assert.ThrowsAsync<AmiModuleDependencyException>(() => host.ActivateAsync(consumer));
        await host.ActivateAsync(provider);
        await host.ActivateAsync(consumer);
        await Assert.ThrowsAsync<AmiModuleDependencyException>(() => host.StopAsync(provider.Id));
        Assert.AreEqual(AmiModuleState.Stopped, await host.StopAsync(consumer.Id));
        Assert.AreEqual(AmiModuleState.Stopped, await host.StopAsync(provider.Id));
    }

    private sealed class DependencyModule(string id, string? required = null) : GXAmiModuleBase
    {
        public override string Id => id;
        public override string Name => id;
        public override string Description => "Tests required module providers.";
        public override IReadOnlyList<AmiModuleDependency> Dependencies => required == null
            ? [] : [new(AmiModuleDependencyKind.Module, required)];
    }

    private sealed class Marker;
    private sealed class ProbeModule : GXAmiModuleBase
    {
        public override string Id => "Testing.Probe";
        public override string Name => "Probe";
        public override string Description => "Test lifecycle";
        public int Starts;
        public int Stops;
        public override Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            ++Starts;
            return Task.CompletedTask;
        }
        public override Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            ++Stops;
            return Task.CompletedTask;
        }
    }
}
