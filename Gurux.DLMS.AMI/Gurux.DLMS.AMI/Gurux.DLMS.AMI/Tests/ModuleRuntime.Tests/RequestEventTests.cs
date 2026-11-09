using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public class RequestEventTests
{
    [TestMethod]
    public async Task HostPublishesMetadataEvenWhenPerformanceCollectionIsDisabled()
    {
        await using var events = new AmiEventHub();
        AmiRequestCompleted? received = null;
        using var subscription = events.Subscribe<AmiRequestCompleted>((message, token) =>
        {
            received = message;
            return Task.CompletedTask;
        });
        using var source = new GXDiagnosticsService(null!, DispatchProxy.Create<IConfigurationRepository, ConfigurationProxy>(), NullLogger<GXDiagnosticsService>.Instance, events);
        Assert.AreEqual(0, source.PerformanceSnapshotIntervalSeconds);
        await source.RecordAsync("user-id", "device", CrudAction.Read, 201, TimeSpan.FromMilliseconds(7), default);
        Assert.IsNotNull(received);
        Assert.AreEqual("user-id", received.UserId);
        Assert.AreEqual("device", received.Target);
        Assert.AreEqual((byte)CrudAction.Read, received.Action);
        Assert.AreEqual(201, received.StatusCode);
        Assert.AreEqual(TimeSpan.FromMilliseconds(7), received.Elapsed);
    }
    public class ConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "remove_Updated")
            {
                return null;
            }
            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
