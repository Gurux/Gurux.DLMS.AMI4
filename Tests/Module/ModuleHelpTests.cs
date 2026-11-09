using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public sealed class ModuleHelpTests
{
    [TestMethod]
    public async Task DefaultModuleHasNoHelp()
    {
        IGXAmiModule module = new EmptyModule();
        Assert.IsNull(await module.GetHelpAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task FileHelpReadsContentAndSupportsCancellation()
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "# Ohje\nKäyttöohje tiedostosta.");
            IGXAmiModule module = new FileModule(path);
            AmiModuleHelp? help = await module.GetHelpAsync(CancellationToken.None);
            Assert.IsNotNull(help);
            Assert.AreEqual(AmiModuleHelpKind.Markdown, help.Kind);
            Assert.AreEqual("# Ohje\nKäyttöohje tiedostosta.", help.Value);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
                await module.GetHelpAsync(cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private class EmptyModule : GXAmiModuleBase
    {
        public override string Id => "help-test";
        public override string Name => "Help test";
        public override string Description => "Tests module help.";
    }

    private sealed class FileModule(string path) : EmptyModule
    {
        public override async Task<AmiModuleHelp?> GetHelpAsync(CancellationToken cancellationToken)
        {
            return new AmiModuleHelp(AmiModuleHelpKind.Markdown,
                await File.ReadAllTextAsync(path, cancellationToken));
        }
    }
}
