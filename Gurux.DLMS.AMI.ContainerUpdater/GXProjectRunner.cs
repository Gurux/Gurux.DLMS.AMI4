using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.ContainerUpdater;

internal sealed class GXProjectRunner(GXDeploymentOptions options) : IGXComposeRunner
{
    private readonly GXComposeProcessRunner runner = new("/project", options.Compose!.CommandTimeoutSeconds);
    public async Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken token = default)
    {
        var values = arguments.ToList();
        if (values[0] == "compose" && File.Exists("/project/.env"))
        {
            values.InsertRange(1, new[] { "--env-file", "/project/.env" });
        }
        if (values[0] == "compose" && values.Contains("up", StringComparer.Ordinal))
        {
            // Publish the selected image before Docker activation, including rollback and recovery.
            await Bootstrap.SyncOverrideAsync(options.Root, "/project");
        }
        return await runner.RunAsync(values, token);
    }
}
