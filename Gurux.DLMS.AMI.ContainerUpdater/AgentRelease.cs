using System.Text.Json;
using System.Text.Json.Nodes;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.ContainerUpdater;

internal static class AgentRelease
{
    internal static async Task WaitForHealthyContainerAsync(IGXComposeRunner runner, GXDeploymentOptions options, CancellationToken token)
    {
        if (options.ProductId != "Gurux.DLMS.AMI.Agent")
        {
            return;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.CheckTimeoutSeconds));
        var settings = options.Compose!;
        var arguments = new List<string> { "compose", "--project-name", settings.ProjectName, "--project-directory", settings.ProjectDirectory };
        foreach (var file in settings.Files)
        {
            arguments.AddRange(new[] { "-f", file });
        }
        var imageOverride = Path.Combine(options.Root, "compose.override.json");
        if (File.Exists(imageOverride))
        {
            arguments.AddRange(new[] { "-f", imageOverride });
        }
        arguments.AddRange(new[] { "ps", "--all", "--quiet", settings.Services["agent"] });
        try
        {
            while (true)
            {
                var ids = (await runner.RunAsync(arguments, timeout.Token)).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (ids.Length != 1)
                {
                    throw new IOException("Exactly one running agent container is required.");
                }
                using var inspect = JsonDocument.Parse(await runner.RunAsync(new[] { "inspect", ids[0] }, timeout.Token));
                var state = inspect.RootElement[0].GetProperty("State");
                if (state.GetProperty("Running").GetBoolean() && state.TryGetProperty("Health", out var health) && health.GetProperty("Status").GetString() == "healthy")
                {
                    return;
                }
                await Task.Delay(250, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException("The drained agent container did not become ready for replacement.");
        }
    }

    internal static async Task<GXUpdateRelease> LoadDescriptorAsync(HttpClient client, GXDeploymentOptions options, GXUpdateRelease release, CancellationToken token)
    {
        if (options.ProductId != "Gurux.DLMS.AMI.Agent" || release.Compose != null)
        {
            return release;
        }
        GXDeploymentQueue.ValidateVersion(release.Version);
        var tag = Uri.EscapeDataString("Gurux.DLMS.AMI.Agent/v" + release.Version);
        var uri = $"https://github.com/Gurux/Gurux.DLMS.AMI4/releases/download/{tag}/Gurux.DLMS.AMI.Agent-{release.Version}.compose.json";
        var descriptor = JsonNode.Parse(await client.GetStringAsync(uri, token))!;
        if (descriptor["schemaVersion"]?.GetValue<int>() != 1 || descriptor["version"]?.GetValue<string>() != release.Version)
        {
            throw new InvalidDataException("Agent image metadata does not match the requested release.");
        }
        var compose = descriptor["compose"]?.Deserialize<GXComposeRelease>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("Agent release has no immutable image metadata.");
        GXComposeDeploymentService.ValidateMetadata(compose);
        var copy = JsonSerializer.SerializeToNode(release)!;
        copy["compose"] = JsonSerializer.SerializeToNode(compose);
        return copy.Deserialize<GXUpdateRelease>()!;
    }
}
