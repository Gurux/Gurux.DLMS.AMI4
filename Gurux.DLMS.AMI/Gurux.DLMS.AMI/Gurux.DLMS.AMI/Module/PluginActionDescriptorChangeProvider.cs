using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Gurux.DLMS.AMI.Module;

public sealed class PluginActionDescriptorChangeProvider : IActionDescriptorChangeProvider
{
    public static PluginActionDescriptorChangeProvider Instance { get; } = new();

    private CancellationTokenSource _cts = new();

    public IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

    public void NotifyChanges()
    {
        var prev = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        prev.Cancel();
    }
}
