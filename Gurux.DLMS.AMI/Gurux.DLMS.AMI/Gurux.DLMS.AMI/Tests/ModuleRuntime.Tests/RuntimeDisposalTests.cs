using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class RuntimeDisposalTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ContainerDisposalDoesNotResolveServicesFromDisposedProvider(bool sharedEvents)
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        if (sharedEvents)
        {
            registrations.AddSingleton<AmiEventHub>();
        }
        registrations.AddSingleton<GXModuleRuntime>();
        var provider = registrations.BuildServiceProvider();
        var runtime = provider.GetRequiredService<GXModuleRuntime>();

        await provider.DisposeAsync();
        await runtime.DisposeAsync();
    }

    [TestMethod]
    public async Task RuntimeDisposalDoesNotDisposeHostOwnedEvents()
    {
        await using var provider = new ServiceCollection().AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<AmiEventHub>().AddSingleton<GXModuleRuntime>().BuildServiceProvider();
        var runtime = provider.GetRequiredService<GXModuleRuntime>();
        var events = provider.GetRequiredService<AmiEventHub>();
        Assert.AreSame(events, runtime.Events);
        await runtime.DisposeAsync();

        int received = 0;
        using var subscription = events.Subscribe<int>((value, _) =>
        {
            received = value;
            return Task.CompletedTask;
        });
        await events.PublishAsync(7);
        Assert.AreEqual(7, received);
    }
}
