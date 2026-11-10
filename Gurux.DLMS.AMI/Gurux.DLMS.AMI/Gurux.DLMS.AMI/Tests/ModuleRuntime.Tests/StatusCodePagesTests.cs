using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Gurux.DLMS.AMI.Server.Midlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class StatusCodePagesTests
{
    [TestMethod]
    public async Task ApiAndHubErrorsNeverExecuteTheUiErrorPage()
    {
        await using var host = await StartAsync();
        foreach (var path in new[] { "/api/check", "/API/check", "/guruxami", "/guruxami/negotiate" })
        {
            foreach (var status in new[] { 400, 401, 403, 404, 405, 500 })
            {
                using var response = await host.Client.GetAsync($"{path}?status={status}");
                Assert.AreEqual(status, (int)response.StatusCode, path);
                Assert.IsNull(response.Headers.Location, path);
                Assert.AreEqual("", await response.Content.ReadAsStringAsync(), path);
                Assert.AreEqual(0, host.UiCalls, path);
            }
        }
        using var missing = await host.Client.GetAsync("/api/missing-route");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual(0, host.UiCalls);
    }

    [TestMethod]
    public async Task BearerChallengesAndForbidsDoNotRenderTheUi()
    {
        await using var host = await StartAsync();
        foreach (var path in new[] { "/api/protected", "/guruxami/protected" })
        {
            using var challenge = await host.Client.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.Unauthorized, challenge.StatusCode);
            Assert.IsNull(challenge.Headers.Location);
            Assert.AreEqual("", await challenge.Content.ReadAsStringAsync());
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new("Bearer", host.Token);
            using var forbidden = await host.Client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);
            Assert.IsNull(forbidden.Headers.Location);
            Assert.AreEqual("", await forbidden.Content.ReadAsStringAsync());
        }
        Assert.AreEqual(0, host.UiCalls);
    }

    [TestMethod]
    public async Task CookieApiChallengesAndForbidsKeepJsonResponses()
    {
        await using var host = await StartAsync();
        foreach (var path in new[] { "/api/cookie", "/guruxami/cookie" })
        {
            using var challenge = await host.Client.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.Unauthorized, challenge.StatusCode);
            Assert.IsNull(challenge.Headers.Location);
            Assert.AreEqual("application/json", challenge.Content.Headers.ContentType?.MediaType);
        }
        using var signIn = await host.Client.GetAsync("/test-cookie");
        Assert.AreEqual(HttpStatusCode.OK, signIn.StatusCode);
        foreach (var path in new[] { "/api/cookie", "/guruxami/cookie" })
        {
            using var forbidden = await host.Client.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);
            Assert.IsNull(forbidden.Headers.Location);
            Assert.AreEqual("application/json", forbidden.Content.Headers.ContentType?.MediaType);
        }
        Assert.AreEqual(0, host.UiCalls);
    }

    [TestMethod]
    public async Task BrowserRequestsKeepLoginRedirectAndUiNotFoundPage()
    {
        await using var host = await StartAsync();
        using var login = await host.Client.GetAsync("/browser/private");
        Assert.AreEqual(HttpStatusCode.Redirect, login.StatusCode);
        StringAssert.Contains(login.Headers.Location!.AbsolutePath, "/Account/Login");
        using var missing = await host.Client.GetAsync("/browser/missing");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual("UI error page", await missing.Content.ReadAsStringAsync());
        Assert.AreEqual(1, host.UiCalls);
    }

    private static async Task<TestHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.Events = new GXCompactCookieEvents();
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, ValidateAudience = false, IssuerSigningKey = key
                };
            });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAmiStatusCodePages();
        app.UseAuthorization();
        var host = new TestHost(app, new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, "test-agent") }, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256))));
        app.MapGet("/404", async context =>
        {
            ++host.UiCalls;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("UI error page");
        });
        foreach (var path in new[] { "/api/check", "/guruxami", "/guruxami/negotiate" })
        {
            app.MapGet(path, context =>
            {
                context.Response.StatusCode = int.Parse(context.Request.Query["status"]!);
                return Task.CompletedTask;
            });
        }
        var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireClaim("missing-permission").Build();
        app.MapGet("/api/protected", () => "API").RequireAuthorization(policy);
        app.MapGet("/guruxami/protected", () => "Hub").RequireAuthorization(policy);
        var cookiePolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme).RequireClaim("missing-permission").Build();
        app.MapGet("/api/cookie", () => "API").RequireAuthorization(cookiePolicy);
        app.MapGet("/guruxami/cookie", () => "Hub").RequireAuthorization(cookiePolicy);
        app.MapGet("/test-cookie", async context =>
        {
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-browser") },
                    CookieAuthenticationDefaults.AuthenticationScheme)));
        });
        app.MapGet("/browser/private", () => "Browser").RequireAuthorization();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        host.Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
        return host;
    }

    private sealed class TestHost(WebApplication app, string token) : IAsyncDisposable
    {
        public HttpClient Client = null!;
        public string Token { get; } = token;
        public int UiCalls;
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}
