using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Module.Testing;

/// <summary>Runs the production module runtime in process without an HTTP server or database.</summary>
public sealed class AmiModuleTestHost : IAsyncDisposable
{
    public AmiModuleTestHost(Action<IServiceCollection>? configureServices = null, IConfiguration? configuration = null)
    {
        Configuration = configuration ?? new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Configuration);
        services.AddSingleton<AmiEventHub>();
        services.AddSingleton<IGXAmiEvents>(provider => provider.GetRequiredService<AmiEventHub>());
        configureServices?.Invoke(services);
        HostServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Runtime = new GXModuleRuntime(HostServices, Configuration, HostServices.GetRequiredService<ILogger<GXModuleRuntime>>());
    }

    public ServiceProvider HostServices { get; }
    public IConfiguration Configuration { get; }
    public GXModuleRuntime Runtime { get; }
    public IGXAmiModuleServices Dispatcher => Runtime;
    public IGXAmiEvents Events => HostServices.GetRequiredService<IGXAmiEvents>();
    public AmiModuleManifest? GetManifest(string id) => Runtime.GetManifest(id);

    public Task<AmiModuleManifest> ActivateAsync(IGXAmiModule module, string? settings = null,
        string version = "0.0.1-local", CancellationToken cancellationToken = default) =>
        Runtime.ActivateAsync(module, module.GetType().Assembly.Location, version, settings, cancellationToken: cancellationToken);

    public Task<AmiModuleState> StopAsync(string id, TimeSpan? waitTimeout = null, CancellationToken cancellationToken = default) =>
        Runtime.StopAsync(id, waitTimeout, cancellationToken);

    public async Task<AmiModuleManifest> ReloadAsync(IGXAmiModule module, string? settings = null,
        string version = "0.0.1-local", CancellationToken cancellationToken = default)
    {
        var state = await StopAsync(module.Id, cancellationToken: cancellationToken);
        if (state != AmiModuleState.Stopped)
        {
            throw new InvalidOperationException($"Module '{module.Id}' did not finish stopping: {state}.");
        }
        return await ActivateAsync(module, settings, version, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Runtime.DisposeAsync();
        }
        finally
        {
            await HostServices.DisposeAsync();
        }
    }
}
