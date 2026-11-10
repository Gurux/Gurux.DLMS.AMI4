using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Pages.KeyManagement;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class PasswordChangeRenderingTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PasswordEditorMasksNewMaterialAndGatesRecoverySeparately(bool recovery)
    {
        var collection = new ServiceCollection().AddLogging();
        collection.AddAuthorizationCore(options =>
        {
            options.AddPolicy(GXKeyPermissions.Lifecycle, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(GXKeyPermissions.Recovery, policy => policy.RequireAssertion(_ => recovery));
        });
        collection.AddCascadingAuthenticationState();
        collection.AddSingleton<AuthenticationStateProvider, SignedIn>();
        collection.AddSingleton(new HttpClient(new MetadataOnly()) { BaseAddress = new Uri("https://ami.test/") });
        collection.AddSingleton<IGXAmiConfirmation, Confirmation>();
        await using var services = collection.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<PasswordChanges>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(PasswordChanges.ManagementId)] = Guid.NewGuid(),
                [nameof(PasswordChanges.DeviceId)] = Guid.NewGuid()
            }));
            return component.ToHtmlString();
        });
        StringAssert.Contains(html, "type=\"password\"");
        StringAssert.Contains(html, "autocomplete=\"new-password\"");
        StringAssert.Contains(html, "You do not need to enter it.");
        StringAssert.Contains(html, "0.0.40.0.0.255");
        Assert.AreEqual(recovery, html.Contains("Restore password used before this change"));
        Assert.IsFalse(html.Contains("old-password"));
        Assert.IsFalse(html.Contains("current-password"));
    }

    private sealed class MetadataOnly : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method, "Rendering must not request or write password material.");
            object body = request.RequestUri!.AbsolutePath.Contains("Associations")
                ? new[] { new GXPasswordAssociation("0.0.40.0.0.255", "Current association") }
                : new[] { new GXPasswordChangeInfo { Id = Guid.NewGuid(), State = "Verified", Created = DateTimeOffset.UtcNow } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body, body.GetType()) });
        }
    }

    private sealed class SignedIn : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "operator")], "test"))));
    }

    private sealed class Confirmation : IGXAmiConfirmation
    {
        public Task<bool> ConfirmAsync(string title, string message) => throw new InvalidOperationException("Rendering cannot change a password.");
        public Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options)
            => throw new InvalidOperationException("Rendering cannot change a password.");
    }
}
