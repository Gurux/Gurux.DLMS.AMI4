using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Testing.Tests;

[TestClass]
public sealed class LegacyBinaryCompatibilityTests
{
    [TestMethod]
    public void PublishedFourXContractModuleIsRejectedBeforeLoadingRemovedInterfaceNames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Compatibility", "AMI.Compatibility.LegacyModule.dll");
        Assert.IsTrue(File.Exists(path), "Build must supply the unchanged published4.x fixture.");
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var reference = reader.AssemblyReferences.Select(reader.GetAssemblyReference)
            .Single(value => reader.GetString(value.Name) == "Gurux.DLMS.AMI.Module");
        Assert.AreEqual(new Version(4, 1, 2611, 502), reference.Version);
        Assert.AreNotEqual(reference.Version.Major, typeof(GXAmiModuleBase).Assembly.GetName().Version!.Major,
            "The incompatible fixture must target a different contract major than the host, including local builds.");
        var error = Assert.ThrowsExactly<AmiModuleDependencyException>(() => GXSharedAssemblyCompatibility.ValidateReferences(path));
        StringAssert.Contains(error.Message, "matching major");
        StringAssert.Contains(error.Message, "4.1.2611.502");
        var loader = new PluginLoadContext(path);
        try
        {
            Assert.ThrowsExactly<AmiModuleDependencyException>(() => loader.LoadMainAssembly());
        }
        finally
        {
            loader.Unload();
        }
    }
}
