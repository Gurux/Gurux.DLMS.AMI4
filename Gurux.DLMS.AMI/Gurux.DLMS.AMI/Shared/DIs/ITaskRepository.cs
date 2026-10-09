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
// This file is a part of Gurux Task Framework.
//
// Gurux Task Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Task Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle tasks.
    /// </summary>
    public interface ITaskRepository
    {
        /// <summary>
        /// List tasks.
        /// </summary>
        /// <returns>Tasks.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXTask>> ListAsync(
            ListTasks? request = null,
            ListTasksResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read task details.
        /// </summary>
        /// <param name="id">Task id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXTask> ReadAsync(Guid id, Expression<Func<GXTask, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new tasks.
        /// </summary>
        /// <param name="tasks">Updated task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken);

        /// <summary>
        /// Delete task(s).
        /// </summary>
        /// <param name="tasks">Deleted task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> tasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark task(s) to complete.
        /// </summary>
        /// <param name="tasks">Completed task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DoneAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restart task(s).
        /// </summary>
        /// <param name="tasks">Restarted task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RestartAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Claim all pending tasks for one idle meter, preferring mapped meters before the oldest unmapped meter.
        /// </summary>
        /// <param name="connectionInfo">Connection information.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="DeviceId">Device Id</param>
        /// <param name="gatewayId">GatewayId Id</param>
        /// <param name="listener">Is agent in listener mode.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Collections of tasks to execute.</returns>
        Task<IEnumerable<GXTask>> GetNextAsync(
            string? connectionInfo,
            Guid agentId,
            Guid? DeviceId,
            Guid? gatewayId,
            bool listener, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear tasks.
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
