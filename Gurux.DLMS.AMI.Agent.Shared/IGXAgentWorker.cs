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
    /// This interface is used to handle agent worker.
    /// </summary>
    public interface IGXAgentWorker
    {
        /// <summary>
        /// Initialize agent worker.
        /// </summary>
        /// <param name="services">Services.</param>
        /// <param name="options">Agent options.</param>
        /// <param name="newVersion">New version is released.</param>
        void Init(IServiceCollection services, AgentOptions options, AutoResetEvent newVersion);

        /// <summary>
        /// Add new agent.
        /// </summary>
        /// <param name="name">Agent name.</param>
        /// <returns>Agent ID.</returns>
        Task<Guid> AddAgentAsync(string name);

        /// <summary>
        /// Start the worker agent.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop the worker agent.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Are taks poll.
        /// </summary>
        /// <remarks>
        /// If true, tasks are polled. If false, the server sends notification from the new tasks.
        /// </remarks>
        bool PollTasks
        {
            get; set;
        }
    }
}