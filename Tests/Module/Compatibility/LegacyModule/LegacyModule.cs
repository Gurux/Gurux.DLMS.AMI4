using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.DependencyInjection;

namespace AMI.Compatibility.LegacyModule;

public sealed class LegacySettings : IAmiModuleSettings
{
    private string? value;
    public void Initialize(string? settings) => value = settings;
    public Task InitializeAsync(string? settings)
    {
        Initialize(settings);
        return Task.CompletedTask;
    }
    public string? Save() => "legacy:" + value;
    public Task<string?> SaveAsync() => Task.FromResult(Save());
    public void Cancel() { }
    public Task CancelAsync() => Task.CompletedTask;
}

public sealed class LegacyModule : GXAmiModuleBase
{
    public override string Id => "Compatibility.LegacyModule";
    public override string Name => "Published contract compatibility fixture";
    public override string Description => "Compiled against Module4.1.2611.502.";
    public int Starts { get; private set; }
    public int Stops { get; private set; }
    public override void Configure(AmiModuleBuilder builder)
    {
        builder.Services.AddScoped<IAmiModuleSettings, LegacySettings>();
        builder.Export<IAmiModuleSettings>();
    }
    public override Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken)
    {
        ++Starts;
        return Task.CompletedTask;
    }
    public override Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken)
    {
        ++Stops;
        return Task.CompletedTask;
    }
}
