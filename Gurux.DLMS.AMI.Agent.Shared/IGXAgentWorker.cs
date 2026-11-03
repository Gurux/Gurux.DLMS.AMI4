//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Agent.Shared
{
    /// <summary>
    /// Defines initialization, registration, and lifecycle operations for an agent worker.
    /// </summary>
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
        /// <returns>A task whose result is the identifier of the registered agent.</returns>
        Task<Guid> AddAgentAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        /// <param name="cancellationToken">Cancels startup and the active worker lifetime.</param>
        /// <remarks>The first call supplies the lifetime token. Later calls share startup and their tokens cancel only their own waits.</remarks>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        /// <param name="cancellationToken">Limits how long the caller waits for shutdown. Cleanup continues if this wait is canceled.</param>
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
