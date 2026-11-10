using Gurux.DLMS.AMI.Update;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class DeploymentEnvironmentTests
{
    [TestMethod]
    public void ContainerTakesPrecedenceOverSystemd()
    {
        foreach (var setting in new[] { "DOTNET_RUNNING_IN_CONTAINER", "DOTNET_RUNNING_IN_CONTAINERS" })
        {
            Assert.AreEqual("Container", GXDeploymentEnvironment.Detect(true,
                name => name == setting ? "true" : name == "INVOCATION_ID" ? "service" : null,
                _ => false, _ => null));
        }
        Assert.AreEqual("Container", GXDeploymentEnvironment.Detect(true, _ => "service",
            path => path == "/.dockerenv", _ => null));
    }

    [TestMethod]
    public void DetectsSystemdInvocationAndServiceCgroup()
    {
        Assert.AreEqual("Systemd", GXDeploymentEnvironment.Detect(true,
            name => name == "INVOCATION_ID" ? "service" : null, _ => false, _ => null));
        Assert.AreEqual("Systemd", GXDeploymentEnvironment.Detect(true, _ => null,
            _ => false, _ => "0::/system.slice/gurux-ami.service"));
    }

    [TestMethod]
    public void DetectsContainersWithoutDotnetEnvironmentVariables()
    {
        foreach (var cgroup in new[] { "0::/docker/test", "0::/kubepods/burstable/test", "0::/system.slice/libpod-test.scope" })
        {
            Assert.AreEqual("Container", GXDeploymentEnvironment.Detect(true, _ => null, _ => false, _ => cgroup));
        }
    }

    [TestMethod]
    public void DoesNotGuessSystemdForInteractiveSessionsOrOtherPlatforms()
    {
        Assert.AreEqual("Linux", GXDeploymentEnvironment.Detect(true, _ => null, _ => false, _ => null));
        Assert.AreEqual("Linux", GXDeploymentEnvironment.Detect(true, _ => null, _ => false,
            _ => "0::/user.slice/user-1000.slice/session-2.scope"));
        Assert.AreEqual("Other", GXDeploymentEnvironment.Detect(false, _ => null, _ => false, _ => null));
    }
}
