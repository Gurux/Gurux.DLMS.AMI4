using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public sealed class InterfaceNamingTests
{
    [TestMethod]
    public void PublicModuleInterfacesUseIGXAmiPrefix()
    {
        var interfaces = typeof(GXAmiModuleBase).Assembly.GetExportedTypes().Where(type => type.IsInterface).ToArray();
        Assert.IsFalse(interfaces.Any(type => type.Name.StartsWith("IAmi", StringComparison.Ordinal)),
            "All public IAmi interfaces must use the IGXAmi prefix.");
        Assert.IsTrue(interfaces.Any(type => type.Name == "IGXAmiModule"));
        Assert.IsTrue(interfaces.Any(type => type.Name == "IGXAmiModuleServices"));
        Assert.IsTrue(interfaces.Any(type => type.Name == "IGXAmiEvents"));
    }
}
