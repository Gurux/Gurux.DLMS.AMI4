using System.Reflection;
using System.Security.Claims;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public class UserActivityAvailabilityTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingOrDisappearingProviderRunsDownstreamExactlyOnce(bool disappears)
    {
        var dispatcher = DispatchProxy.Create<IGXAmiModuleServices, OptionalDispatcher>();
        ((OptionalDispatcher)(object)dispatcher).Available = disappears;
        using var diagnostics = Diagnostics();
        var calls = 0;
        var middleware = Middleware(dispatcher, diagnostics, _ => { ++calls; return Task.CompletedTask; });
        await middleware.Invoke(Context());
        await middleware.Invoke(Context());
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public async Task SameMiddlewareFollowsProviderActivationStopRestartReplacementAndRemoval()
    {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(services, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        using var diagnostics = Diagnostics();
        var calls = 0;
        var middleware = Middleware(runtime, diagnostics, _ => { ++calls; return Task.CompletedTask; });
        async Task Check(bool available, string? generation = null)
        {
            var context = Context();
            context.RequestServices = services;
            var before = calls;
            Assert.AreEqual(available, runtime.IsAvailable<IGXAmiUserActivity>());
            await middleware.Invoke(context);
            Assert.AreEqual(before + 1, calls);
            Assert.AreEqual(generation ?? "", context.Response.Headers["X-Activity-Generation"].ToString());
        }
        async Task Start(string generation)
        {
            await runtime.ActivateAsync(new ActivityModule(generation), typeof(ActivityModule).Assembly.Location, "1.0.0", null);
        }
        await Check(false);
        await Start("first");
        await Check(true, "first");
        await runtime.StopAsync("Availability.Activity");
        await Check(false);
        await Start("second");
        await Check(true, "second");
        await runtime.RemoveAsync("Availability.Activity");
        await Check(false);
    }

    [TestMethod]
    public async Task ModuleLifecycleMutationBypassesItsOwnActivityProvider()
    {
        var dispatcher = DispatchProxy.Create<IGXAmiModuleServices, OptionalDispatcher>();
        ((OptionalDispatcher)(object)dispatcher).Available = true;
        using var diagnostics = Diagnostics();
        var calls = 0;
        var middleware = Middleware(dispatcher, diagnostics, _ => { ++calls; return Task.CompletedTask; });
        var context = Context();
        context.Request.Path = "/api/Module/Update";
        await middleware.Invoke(context);
        Assert.AreEqual(1, calls);
    }

    private static DefaultHttpContext Context() => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "test")),
        Request = { Path = "/api/Device/Read" }
    };
    private static GXDiagnosticsService Diagnostics()
        => new(null!, DispatchProxy.Create<IConfigurationRepository, ConfigurationProxy>(), NullLogger<GXDiagnosticsService>.Instance);
    private static RequestResponseLoggingMiddleware Middleware(IGXAmiModuleServices modules, GXDiagnosticsService diagnostics, RequestDelegate next)
        => new(next, null!, NullLogger<RequestResponseLoggingMiddleware>.Instance, diagnostics, modules);

    public class OptionalDispatcher : DispatchProxy
    {
        public bool Available;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "IsAvailable" => Available,
            "TryInvokeWithContextAsync" when Available => Task.FromResult(false),
            _ => throw new InvalidOperationException("An absent provider must not enter module dispatch.")
        };
    }
    public class ConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => null;
    }
    private sealed class ActivityModule(string generation) : GXAmiModuleBase
    {
        public override string Id => "Availability.Activity";
        public override string Name => "Activity";
        public override string Description => "Availability lifecycle fixture.";
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddScoped<IGXAmiUserActivity>(_ => new Activity(generation));
            builder.Export<IGXAmiUserActivity>();
        }
    }
    private sealed class Activity(string generation) : IGXAmiUserActivity
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next, string target, byte action, long startedAt, CancellationToken cancellationToken)
        {
            context.Response.Headers["X-Activity-Generation"] = generation;
            return next(context);
        }
    }
}
