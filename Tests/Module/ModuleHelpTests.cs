using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gurux.DLMS.AMI.Module.Tests;

[TestClass]
public sealed class ModuleHelpTests
{
    [TestMethod]
    public void HelpIsOptionalAndRequiresAnExplicitAccessPolicy()
    {
        var builder = new AmiModuleBuilder("help-test", new ConfigurationBuilder().Build(), null!);
        Assert.AreEqual(0, builder.Endpoints.Count);
        Assert.ThrowsExactly<ArgumentException>(() =>
            builder.AddHelp<HelpProvider, HelpContent>(" ", static (provider, request, token) => provider.ReadAsync(request, token)));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            builder.AddHelp<HelpProvider, HelpContent>("module.view", null!));
        Assert.AreEqual(0, builder.Endpoints.Count);
        Assert.AreEqual(0, builder.Services.Count);
    }

    [TestMethod]
    public async Task HelpEndpointForwardsMetadataAndReturnsJson()
    {
        using var fixture = new HelpFixture();
        var endpoint = fixture.Builder.Endpoints.Single();
        Assert.AreEqual("help", endpoint.Pattern);
        CollectionAssert.AreEqual(new[] { "GET" }, endpoint.Methods.ToArray());
        CollectionAssert.AreEqual(new[] { "module.view" }, endpoint.Policies.ToArray());
        Assert.IsFalse(endpoint.IsAnonymous);
        Assert.AreSame(fixture.Provider, fixture.Services.GetRequiredService<HelpProvider>());
        fixture.Http.Request.QueryString = new QueryString("?topic=configuration&version=2.0&culture=fi");

        await endpoint.Handler(fixture.Request, CancellationToken.None);

        Assert.AreEqual(new AmiModuleHelpRequest("configuration", "2.0", "fi"), fixture.Provider.LastRequest);
        Assert.AreEqual("application/json; charset=utf-8", fixture.Http.Response.ContentType);
        fixture.Http.Response.Body.Position = 0;
        var content = await JsonSerializer.DeserializeAsync<HelpContent>(fixture.Http.Response.Body, JsonSerializerOptions.Web);
        Assert.AreEqual(new HelpContent("markdown", "# Help"), content);
    }

    [TestMethod]
    public async Task UnknownHelpTopicReturnsNotFound()
    {
        using var fixture = new HelpFixture();
        fixture.Http.Request.QueryString = new QueryString("?topic=unknown");
        await fixture.Builder.Endpoints.Single().Handler(fixture.Request, CancellationToken.None);
        Assert.AreEqual(StatusCodes.Status404NotFound, fixture.Http.Response.StatusCode);
        Assert.AreEqual(0L, fixture.Http.Response.Body.Length);
        Assert.AreEqual(new AmiModuleHelpRequest("unknown"), fixture.Provider.LastRequest);
    }

    [TestMethod]
    public async Task FileHelpReadsContentAndSupportsCancellation()
    {
        string path = Path.GetTempFileName();
        try
        {
            const string text = "# Ohje\nKäyttöohje tiedostosta.";
            await File.WriteAllTextAsync(path, text);
            using var fixture = new HelpFixture();
            fixture.Provider.FilePath = path;
            var endpoint = fixture.Builder.Endpoints.Single();
            await endpoint.Handler(fixture.Request, CancellationToken.None);
            fixture.Http.Response.Body.Position = 0;
            var content = await JsonSerializer.DeserializeAsync<HelpContent>(fixture.Http.Response.Body, JsonSerializerOptions.Web);
            Assert.AreEqual(new HelpContent("markdown", text), content);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                endpoint.Handler(fixture.Request, cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class HelpFixture : IDisposable
    {
        public AmiModuleBuilder Builder { get; } = new("help-test", new ConfigurationBuilder().Build(), null!);
        public ServiceProvider Services { get; }
        public HelpProvider Provider => Services.GetRequiredService<HelpProvider>();
        public DefaultHttpContext Http { get; } = new();
        public AmiModuleRequest Request { get; }

        public HelpFixture()
        {
            Builder.AddHelp<HelpProvider, HelpContent>("module.view", static (provider, request, token) => provider.ReadAsync(request, token));
            Services = Builder.Services.BuildServiceProvider();
            Http.Response.Body = new MemoryStream();
            Request = new AmiModuleRequest(Http, new AmiModuleContext
            {
                ModuleId = "help-test", Services = Services, HostServices = Services,
                Settings = null!, CancellationToken = default, JsonOptions = JsonSerializerOptions.Web
            });
        }

        public void Dispose()
        {
            Http.Response.Body.Dispose();
            Services.Dispose();
        }
    }

    public sealed record HelpContent(string Format, string Text);

    public sealed class HelpProvider
    {
        public string? FilePath { get; set; }
        public AmiModuleHelpRequest? LastRequest { get; private set; }

        public async Task<HelpContent?> ReadAsync(AmiModuleHelpRequest request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            LastRequest = request;
            if (request.Topic == "unknown")
            {
                return null;
            }
            return new HelpContent("markdown", FilePath == null ? "# Help" : await File.ReadAllTextAsync(FilePath, token));
        }
    }
}
