using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Server.Module;

public sealed partial class GXModuleRuntime
{
    private readonly (AmiEventHub Hub, bool Owned) eventHubLifetime = CreateEventHub(host, logger);
    private AmiEventHub eventHub => eventHubLifetime.Hub;

    private static (AmiEventHub Hub, bool Owned) CreateEventHub(IServiceProvider host, ILogger<GXModuleRuntime> logger)
    {
        var shared = host.GetService<AmiEventHub>();
        return (shared ?? new AmiEventHub(error => logger.LogError(error, "Module event subscriber failed.")), shared == null);
    }

    /// <summary>Host event publisher shared with module-owned subscriptions.</summary>
    public IGXAmiEvents Events => eventHub;

    private Task DisposeOwnedEventsAsync() => eventHubLifetime.Owned
        ? eventHub.DisposeAsync().AsTask() : Task.CompletedTask;

    private IGXAmiEvents CreateModuleEvents(Entry owner) => new ModuleEvents(this, owner);

    private sealed class ModuleEvents(GXModuleRuntime runtime, Entry owner) : IGXAmiEvents
    {
        public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            lock (runtime.sync)
            {
                if (owner.Cleaned || owner.State is Gurux.DLMS.AMI.Module.Enums.AmiModuleState.Stopping or Gurux.DLMS.AMI.Module.Enums.AmiModuleState.Failed)
                {
                    throw new AmiModuleUnavailableException("The subscribing module is stopping.");
                }
                var subscription = runtime.eventHub.Subscribe<T>(async (message, token) =>
                {
                    var entered = false;
                    try
                    {
                        await runtime.CallAsync(owner.Manifest.Id, async (_, _, cancellationToken) =>
                        {
                            entered = true;
                            await handler(message, cancellationToken);
                            return true;
                        }, token, owner.Generation);
                    }
                    catch (AmiModuleUnavailableException) when (!entered)
                    {
                        // A queued publication must not enter a module generation that has stopped.
                    }
                });
                owner.Owned.Add(subscription);
                return subscription;
            }
        }

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) =>
            runtime.eventHub.PublishAsync(message, cancellationToken);
    }
}
