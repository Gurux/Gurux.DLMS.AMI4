using System.Text.Json;
using System.Net;
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.ContainerUpdater;

/// <summary>Adapts the connected agent's fresh runtime heartbeat to the updater's local version probe.</summary>
internal sealed class AgentVersionHandler(string root) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.RequestUri is { IsLoopback: true, AbsolutePath: "/agent-version" })
        {
            try
            {
                var value = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(root, "queue", "agent-health.json"), token));
                var updated = value?["updatedAt"]?.GetValue<DateTimeOffset>() ?? DateTimeOffset.MinValue;
                var version = value?["version"]?.GetValue<string>();
                if ((value?["running"]?.GetValue<bool>() == true || value?["drained"]?.GetValue<bool>() == true) && NuGet.Versioning.NuGetVersion.TryParse(version, out _) &&
                    DateTimeOffset.UtcNow - updated < TimeSpan.FromSeconds(30) && updated <= DateTimeOffset.UtcNow.AddSeconds(5))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(version!) };
                }
            }
            catch (Exception error) when (error is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException) { }
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
        return await base.SendAsync(request, token);
    }
}
