using System.Text.Json;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = new AmiModuleBuilder("Example", new ConfigurationBuilder().Build(), null!);
builder.AddHelp<HelpProvider, HelpContent>("module.view", static (provider, request, token) => provider.GetHelpAsync(request, token));
using var services = builder.Services.BuildServiceProvider();
var provider = services.GetRequiredService<HelpProvider>();
if (!ReferenceEquals(provider, services.GetRequiredService<HelpProvider>()))
{
    throw new Exception("Help provider registrations must resolve the same singleton.");
}
var endpoint = builder.Endpoints.Single();
if (endpoint.Pattern != "help" || !endpoint.Methods.SequenceEqual(new[] { "GET" }) ||
    endpoint.IsAnonymous || !endpoint.Policies.SequenceEqual(new[] { "module.view" }))
{
    throw new Exception("Help must expose a policy-protected GET endpoint.");
}
var context = new DefaultHttpContext();
context.Request.QueryString = new QueryString("?topic=module:Example:configuration&version=2.0&culture=fi");
context.Response.Body = new MemoryStream();
var request = new AmiModuleRequest(context, new AmiModuleContext
{
    ModuleId = "Example", Services = services, HostServices = services, Settings = null!,
    CancellationToken = default, JsonOptions = JsonSerializerOptions.Web
});
await endpoint.Handler(request, default);
context.Response.Body.Position = 0;
var content = await JsonSerializer.DeserializeAsync<HelpContent>(context.Response.Body, JsonSerializerOptions.Web);
if (content?.Format != "markdown" || content.Text != "# Example" ||
    context.Response.ContentType != "application/json; charset=utf-8" ||
    provider.LastRequest != new AmiModuleHelpRequest("module:Example:configuration", "2.0", "fi"))
{
    throw new Exception("The endpoint must forward topic, version and culture and return provider content.");
}
context.Request.QueryString = new QueryString("?topic=unknown");
await endpoint.Handler(request, default);
if (context.Response.StatusCode != StatusCodes.Status404NotFound)
{
    throw new Exception("An unknown topic must return 404.");
}
using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
try
{
    await endpoint.Handler(request, cancellation.Token);
    throw new Exception("Help requests must forward cancellation.");
}
catch (OperationCanceledException)
{
}
var invalid = new AmiModuleBuilder("Invalid", new ConfigurationBuilder().Build(), null!);
try
{
    invalid.AddHelp<HelpProvider, HelpContent>(" ", static (provider, request, token) => provider.GetHelpAsync(request, token));
    throw new Exception("Help registration must reject an empty access policy.");
}
catch (ArgumentException)
{
}
if (invalid.Services.Count != 0 || invalid.Endpoints.Count != 0)
{
    throw new Exception("Invalid help registration must not leave partial services or endpoints.");
}
try
{
    invalid.AddHelp<HelpProvider, HelpContent>("module.view", null!);
    throw new Exception("Help registration must reject a null callback.");
}
catch (ArgumentNullException)
{
}
if (invalid.Services.Count != 0 || invalid.Endpoints.Count != 0)
{
    throw new Exception("A null callback must not leave partial registrations.");
}
if (typeof(AmiModuleBuilder).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name == "Gurux.UI.Components"))
{
    throw new Exception("The Module contract must not depend on Gurux.UI.Components.");
}
Console.WriteLine("PASS module help registration, policy, JSON response, request metadata, unknown topics and cancellation.");

public sealed record HelpContent(string Format, string Text);

public sealed class HelpProvider
{
    public AmiModuleHelpRequest? LastRequest { get; private set; }
    public Task<HelpContent?> GetHelpAsync(AmiModuleHelpRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        return Task.FromResult(request.Topic == "unknown" ? null : new HelpContent("markdown", "# Example"));
    }
}
