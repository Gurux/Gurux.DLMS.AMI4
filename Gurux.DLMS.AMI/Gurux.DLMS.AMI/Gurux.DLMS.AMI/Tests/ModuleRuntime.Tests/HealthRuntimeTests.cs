using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace ModuleRuntime.Tests;
[TestClass]
public sealed class HealthRuntimeTests
{
    public interface IHealthService { }
    public sealed class HealthService : IHealthService
    {
        public static int Created;
        public HealthService() { Interlocked.Increment(ref Created); }
    }
    private sealed class TestModule : GXAmiModuleBase
    {
        public override string Id => "health-test";
        public override string Name => "Health test";
        public override string Description => "Health test";
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddScoped<IHealthService, HealthService>();
            builder.Export<IHealthService>();
        }
    }
    [TestMethod]
    public async Task ReportsLeasesForcedShutdownAndRetainsGenerationUntilReload()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        await runtime.ActivateAsync(new TestModule(), typeof(TestModule).Assembly.Location, "1", null);
        HealthService.Created = 0;
        var first = runtime.GetHealth("health-test")!;
        Assert.AreEqual(0, HealthService.Created);
        Assert.IsNotNull(first.StartedAt);
        Assert.AreEqual(0, first.ActiveCalls);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocation = runtime.InvokeAsync<IHealthService>(async (_, _) => { entered.SetResult(); await release.Task; });
        await entered.Task;
        Assert.AreEqual(1, runtime.GetHealth("health-test")!.ActiveCalls);
        await runtime.StopAsync("health-test", TimeSpan.Zero);
        var stopping = runtime.GetHealth("health-test")!;
        Assert.IsTrue(stopping.PendingShutdown);
        Assert.IsTrue(stopping.ForcedCancellation);
        Assert.IsNotNull(stopping.StopRequestedAt);
        release.SetResult();
        await invocation;
        await runtime.StopAsync("health-test");
        var stopped = runtime.GetHealth("health-test")!;
        Assert.AreEqual(AmiModuleState.Stopped, stopped.State);
        Assert.AreEqual(0, stopped.ActiveCalls);
        Assert.AreEqual(first.Generation, stopped.Generation);
        Assert.IsNotNull(stopped.StoppedAt);
        await runtime.ActivateAsync(new TestModule(), typeof(TestModule).Assembly.Location, "2", null);
        Assert.AreNotEqual(first.Generation, runtime.GetHealth("health-test")!.Generation);
    }
    [TestMethod]
    public async Task StartupFailuresNeverExposeExceptionContents()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        runtime.RecordStartupFailure("missing", "Missing", new Exception("password=secret"));
        var health = runtime.GetHealth("missing")!;
        Assert.AreEqual(AmiModuleState.Failed, health.State);
        Assert.IsFalse(health.LastError!.Contains("secret"));
        Assert.IsNull(runtime.GetHealth("unknown"));
    }

    public sealed class FailingBackground : Microsoft.Extensions.Hosting.BackgroundService
    {
        public static TaskCompletionSource Trigger = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Trigger.Task.WaitAsync(stoppingToken);
            throw new InvalidOperationException("credential=secret");
        }
    }
    private sealed class BackgroundModule : GXAmiModuleBase
    {
        public override string Id => "background-health";
        public override string Name => "Background health";
        public override string Description => "Background health";
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.Services.AddHostedService<FailingBackground>();
        }
    }
    [TestMethod]
    public async Task BackgroundFailureRemainsVisibleAfterCleanupWithoutSecretDetails()
    {
        FailingBackground.Trigger = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        var fault = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Faulted += _ => { fault.TrySetResult(); return Task.CompletedTask; };
        await runtime.ActivateAsync(new BackgroundModule(), typeof(BackgroundModule).Assembly.Location, "1", null);
        Assert.AreEqual("Running", runtime.GetHealth("background-health")!.BackgroundServices.Single().State);
        FailingBackground.Trigger.SetResult();
        await fault.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await runtime.StopAsync("background-health");
        var health = runtime.GetHealth("background-health")!;
        Assert.AreEqual(AmiModuleState.Failed, health.State);
        Assert.AreEqual("Faulted", health.BackgroundServices.Single().State);
        Assert.IsFalse(health.LastError!.Contains("secret"));
    }
    [TestMethod]
    public void HealthEndpointRequiresModuleViewAndHasNoAnonymousOverride()
    {
        var method = typeof(Gurux.DLMS.AMI.Server.Repository.ModuleController).GetMethod("Health")!;
        var authorization = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();
        Assert.AreEqual(GXModulePolicies.View, authorization.Policy);
        Assert.AreEqual(0, method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true).Length);
        var route = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), true)
            .Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>().Single();
        Assert.AreEqual("Health/{id}", route.Template);
    }
}
