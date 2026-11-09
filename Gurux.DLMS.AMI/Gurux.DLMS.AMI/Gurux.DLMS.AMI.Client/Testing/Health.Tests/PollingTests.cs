using Gurux.DLMS.AMI.Client.Pages.Module;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Health.Tests;
[TestClass]
public sealed class PollingTests
{
    [TestMethod]
    public async Task CloseCancelsInflightRequestAndDisposeWaitsForCompletion()
    {
        var polling = new ModuleHealthPolling();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        polling.Start(async token =>
        {
            entered.SetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            finally { exited.SetResult(); }
        });
        await entered.Task;
        polling.Stop();
        await polling.DisposeAsync();
        Assert.IsTrue(exited.Task.IsCompletedSuccessfully);
    }
    [TestMethod]
    public async Task ReopeningCancelsPreviousPollingAndDisposedPollingCannotRestart()
    {
        var polling = new ModuleHealthPolling();
        CancellationToken first = default;
        polling.Start(token => { first = token; return Task.CompletedTask; });
        polling.Start(_ => Task.CompletedTask);
        Assert.IsTrue(first.IsCancellationRequested);
        await polling.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => polling.Start(_ => Task.CompletedTask));
    }
}
