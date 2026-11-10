using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.UI.Components.Help;
using Gurux.UI.Components;
using Gurux.UI.Components.Help;
using Gurux.UI.Components.Enums;
using Microsoft.Extensions.DependencyInjection;

var api = new HelpApi();
using var http = new HttpClient(api) { BaseAddress = new Uri("https://ami.test/") };
using var services = new ServiceCollection().AddSingleton(http).AddAmiHelp().BuildServiceProvider();
var help = services.GetRequiredService<GXHelpService>();
var request = new GXHelpRequest("cosem.GXDLMSClock", "0", "fi-FI");
var content = await help.GetHelpAsync(request);
Check(content?.Kind == HelpKind.Markdown && content.Value == "# Kello", "The shared help service must use the authenticated AMI proxy.");
Check(api.Last!.AbsolutePath == "/api/Help/Cosem" && api.Last.Query.Contains("culture=fi-FI"), "The browser must preserve the requested culture.");
Check(!services.GetServices<IGXHelpProvider>().Any(provider => provider is GXDLMSObjectHelpProvider), "Browser clients must never fall through to direct Gurux requests.");
Check(GXDLMSObjectHelpCatalog.GetAddress(request)?.AbsolutePath.StartsWith("/fi/") == true, "The referenced library must contain the new localized article support.");
var oldCulture = CultureInfo.CurrentUICulture;
try
{
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fi-FI");
    await help.GetHelpAsync(request with { Culture = null });
    Check(api.Last!.Query.Contains("culture=fi-FI"), "An implicit culture must follow the current UI language.");
}
finally
{
    CultureInfo.CurrentUICulture = oldCulture;
}
if (args is ["--live"])
{
    foreach (var culture in new[] { "en", "fi" })
    {
        var markdown = await GXDLMSObjectHelpCatalog.LoadAsync(request with { Culture = culture });
        Check(!string.IsNullOrWhiteSpace(markdown) && markdown.StartsWith(culture == "fi" ? "# Kello" : "# Clock"), "The live " + culture + " article must load.");
        Console.WriteLine("PASS live " + culture + " article: " + markdown!.Split('\n')[0]);
    }
}
Console.WriteLine("PASS AMI help proxy, culture propagation, shared library integration and no direct browser fallback.");
static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}
sealed class HelpApi : HttpMessageHandler
{
    internal Uri? Last;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Last = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new GXHelpContent(HelpKind.Markdown, "# Kello")) });
    }
}
