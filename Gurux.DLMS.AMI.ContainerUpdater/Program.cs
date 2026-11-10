using System.Runtime.InteropServices;
using Gurux.DLMS.AMI.ContainerUpdater;

if (args is ["--help"])
{
    Console.WriteLine("Gurux AMI Compose updater: starts with the supplied AMI/agent Compose installation.");
    return 0;
}
var root = Environment.GetEnvironmentVariable("GURUX_UPDATER_ROOT") ?? "/state";
if (args is ["--health-check"])
{
    var path = Path.Combine(root, "agent-heartbeat");
    return File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromSeconds(30) ? 0 : 1;
}
using var lifetime = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; lifetime.Cancel(); };
using var term = OperatingSystem.IsLinux() ? PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; lifetime.Cancel(); }) : null;
try
{
    var configuration = await Bootstrap.ConfigureAsync(root, lifetime.Token);
    await GXDeploymentCommand.RunAsync(new[] { "--config", configuration }, lifetime.Token);
    return 0;
}
catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
{
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine("Container updater failed: " + error.Message);
    return 1;
}
