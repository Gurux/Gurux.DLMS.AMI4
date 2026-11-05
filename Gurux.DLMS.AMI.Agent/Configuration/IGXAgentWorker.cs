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

using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Agent.Shared
{
    /// <summary>
    /// Defines initialization, registration, and lifecycle operations for an agent worker.
    /// </summary>
    /// <remarks>
    /// Configure dependencies, build the host service provider, then initialize the worker before registration or startup.
    /// A stopped worker cannot be restarted; create a new instance. Stop the worker before disposing host-owned services.
    /// </remarks>
    public interface IGXAgentWorker : IAsyncDisposable
    {
        /// <summary>
        /// Registers worker dependencies before the host builds its service provider. Call once per service collection.
        /// </summary>
        /// <param name="services">The service collection used to configure worker dependencies.</param>
        void ConfigureServices(IServiceCollection services);

        /// <summary>Initializes one worker instance exactly once. The provider and notifications remain owned by the host.</summary>
        /// <param name="services">The host-owned service provider.</param>
        /// <param name="options">Validated configuration; an empty Id is permitted until registration.</param>
        /// <param name="notifications">Host change notifications.</param>
        /// <remarks>ConfigureServices and Init are sequential host setup; finish initialization before invoking lifecycle operations.</remarks>
        /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The instance has already been initialized or shutdown has been requested.</exception>
        /// <exception cref="ObjectDisposedException">The worker has been disposed.</exception>
        /// <exception cref="GXAgentOptionsValidationException">The supplied configuration is invalid.</exception>
        void Init(IServiceProvider services, GXAgentOptions options, IGXAgentHostNotifications notifications);

        /// <summary>Gets discovered runtime capabilities, separate from persisted configuration.</summary>
        GXAgentCapabilities Capabilities { get; }

        /// <summary>Gets the currently published validated configuration. Available after initialization.</summary>
        GXAgentOptionsSnapshot Configuration { get; }

        /// <summary>Gets the worker lifecycle state, independently of server connectivity.</summary>
        GXAgentWorkerState State { get; }

        /// <summary>Gets the server notification connection state.</summary>
        GXAgentConnectionState ConnectionState { get; }

        /// <summary>
        /// Registers a new agent with the server.
        /// </summary>
        /// <param name="name">The name of the agent to register.</param>
        /// <param name="cancellationToken">Cancels registration.</param>
        /// <returns>A task whose result is the registered identifier, or <see cref="Guid.Empty"/> if the server supplies no identifier.</returns>
        /// <remarks>A successful registration publishes the identifier to <see cref="Configuration"/>.</remarks>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty or contains only whitespace.</exception>
        /// <exception cref="InvalidOperationException">The worker is not initialized or shutdown has been requested.</exception>
        /// <exception cref="OperationCanceledException">Registration or the worker lifetime is canceled.</exception>
        Task<Guid> AddAgentAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        /// <param name="cancellationToken">Cancels startup and the active worker lifetime.</param>
        /// <remarks>The first call supplies the lifetime token. Later calls share startup and their tokens cancel only their own waits.</remarks>
        /// <exception cref="InvalidOperationException">The worker is not initialized or shutdown has been requested.</exception>
        /// <exception cref="GXAgentOptionsValidationException">The configuration is invalid or registration has not supplied an identifier.</exception>
        /// <exception cref="OperationCanceledException">Startup or this caller's wait is canceled.</exception>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        /// <param name="cancellationToken">Limits how long the caller waits for shutdown. Cleanup continues if this wait is canceled.</param>
        /// <remarks>
        /// Concurrent calls share shutdown. Even an already canceled token requests shutdown before canceling the caller's wait.
        /// Shutdown can remain pending while completed task results exist only in memory and storage is unavailable.
        /// Keep the process and host services alive until cleanup finishes to preserve those results.
        /// </remarks>
        /// <exception cref="OperationCanceledException">This caller stops waiting; the shared cleanup operation continues.</exception>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets or sets whether the agent polls the server for tasks.
        /// </summary>
        /// <remarks>
        /// If <see langword="true"/>, the agent polls for tasks.
        /// If <see langword="false"/>, the server notifies the agent when new tasks are available.
        /// </remarks>
        bool PollTasks { get; set; }
    }
}
