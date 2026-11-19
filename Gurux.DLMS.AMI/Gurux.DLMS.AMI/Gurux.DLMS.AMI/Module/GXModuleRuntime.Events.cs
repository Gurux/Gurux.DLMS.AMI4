// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Coordinates module generations, dependency availability, event delivery, and lifecycle operations.</summary>
public sealed partial class GXModuleRuntime
{
    /// <summary>Event hub and whether this runtime is responsible for disposing it.</summary>
    private readonly (AmiEventHub Hub, bool Owned) _eventHubLifetime = CreateEventHub(host, logger);
    /// <summary>Typed event hub shared with modules running in this host.</summary>
    private AmiEventHub eventHub => _eventHubLifetime.Hub;

    /// <summary>Uses the host's shared event hub when available or creates an owned hub with logged subscriber failures.</summary>
    /// <param name="host">Host services providing an optional shared event hub.</param>
    /// <param name="logger">Logger receiving isolated event subscriber failures.</param>
    /// <returns>The selected event hub and whether this runtime must dispose it.</returns>
    private static (AmiEventHub Hub, bool Owned) CreateEventHub(IServiceProvider host, ILogger<GXModuleRuntime> logger)
    {
        var shared = host.GetService<AmiEventHub>();
        return (shared ?? new AmiEventHub(error => logger.LogError(error, "Module event subscriber failed.")), shared == null);
    }

    /// <summary>Host event publisher shared with module-owned subscriptions.</summary>
    public IGXAmiEvents Events => eventHub;

    /// <summary>Drains and disposes the event hub only when this runtime created and owns it.</summary>
    /// <returns>A task that completes after any owned event subscriptions have drained.</returns>
    private Task DisposeOwnedEventsAsync() => _eventHubLifetime.Owned
        ? eventHub.DisposeAsync().AsTask() : Task.CompletedTask;

    /// <summary>Creates an event facade that binds subscriptions and publications to the owning module generation.</summary>
    /// <param name="owner">Owner identity or module generation restricting the scoped operation.</param>
    /// <returns>The generation-scoped event service.</returns>
    private IGXAmiEvents CreateModuleEvents(Entry owner) => new ModuleEvents(this, owner);

    /// <summary>Restricts event subscriptions and publications to the owning module generation.</summary>
    /// <param name="runtime">Runtime coordinating module generations and execution leases.</param>
    /// <param name="owner">Principal or module generation that owns the scoped operation.</param>
    private sealed class ModuleEvents(GXModuleRuntime runtime, Entry owner) : IGXAmiEvents
    {
        /// <inheritdoc />
        public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            lock (runtime._sync)
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

        /// <inheritdoc />
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) =>
            runtime.eventHub.PublishAsync(message, cancellationToken);
    }
}
