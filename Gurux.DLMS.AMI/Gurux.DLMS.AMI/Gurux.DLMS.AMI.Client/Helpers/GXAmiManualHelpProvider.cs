using System.Net;
using System.Net.Http.Json;
using Gurux.UI.Components;
using Gurux.UI.Components.Help;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Loads general AMI documentation through the same-origin help proxy.</summary>
public sealed class GXAmiManualHelpProvider(HttpClient http) : IGXHelpProvider
{
    public async Task<GXHelpContent?> GetHelpAsync(GXHelpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Topic.StartsWith("cosem.", StringComparison.Ordinal) || request.Topic.StartsWith("module:", StringComparison.Ordinal))
        {
            return null;
        }
        using var response = await http.GetAsync("api/Help/Manual?topic=" + Uri.EscapeDataString(request.Topic), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) { return null; }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GXHelpContent>(cancellationToken);
    }
}
