using Gurux.DLMS.AMI.Agent;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Release smoke fixture only: exercise the production heartbeat while the process is drained.
if (args is not ["--health-reporter", var healthRoot])
{
    throw new ArgumentException("Usage: --health-reporter <state-directory>");
}
if (!OperatingSystem.IsLinux())
{
    throw new PlatformNotSupportedException("The container health smoke reporter runs in Linux containers.");
}
Directory.CreateDirectory(Path.Combine(healthRoot, "queue"));
using var cancellation = new CancellationTokenSource();
using var signal = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
{
    context.Cancel = true;
    cancellation.Cancel();
});
var version = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null,
    FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion) ?? "";
GXAgentContainerHealth.MarkDrained();
try
{
    await GXAgentContainerHealth.RunAsync(healthRoot, version, () => false, cancellation.Token);
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
}
