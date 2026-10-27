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
    public interface IGXAgentWorker
    {
        /// <summary>
        /// Initializes the agent worker with services, settings, and change signals.
        /// </summary>
        /// <param name="services">The service collection used to configure worker dependencies.</param>
        /// <param name="options">The connection and worker settings for the agent.</param>
        /// <param name="newVersion">The event signaled when a new agent version is available.</param>
        /// <param name="updated">The event signaled when agent settings change.</param>
        void Init(IServiceCollection services,
            AgentOptions options,
            AutoResetEvent newVersion,
            AutoResetEvent updated);

        /// <summary>
        /// Registers a new agent with the server.
        /// </summary>
        /// <param name="name">The name of the agent to register.</param>
        /// <returns>A task whose result is the identifier of the registered agent.</returns>
        Task<Guid> AddAgentAsync(string name);

        /// <summary>
        /// Starts the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Stops the agent worker.
        /// </summary>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task StopAsync();

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
