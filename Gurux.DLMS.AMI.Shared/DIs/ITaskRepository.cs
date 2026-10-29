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
    [System.ComponentModel.Description("This interface is used to handle tasks.")]
    public interface ITaskRepository
    {
        /// <summary>
        /// List tasks.
        /// </summary>
        /// <returns>Tasks.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List tasks.")]
        Task<IEnumerable<GXTask>> ListAsync(
            ListTasks? request = null,
            ListTasksResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read task details.
        /// </summary>
        /// <param name="id">Task id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested task.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read task details.")]
        Task<GXTask> ReadAsync(Guid id, Expression<Func<GXTask, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new tasks.
        /// </summary>
        /// <param name="tasks">Updated task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the added records.</returns>
        [System.ComponentModel.Description("Add new tasks.")]
        Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken);

        /// <summary>Delete tasks.</summary>
        /// <param name="tasks">Deleted task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete tasks.</returns>
        [System.ComponentModel.Description("Delete tasks.")]
        Task DeleteAsync(IEnumerable<Guid> tasks, CancellationToken cancellationToken = default);

        /// <summary>Records task completion and finishes the corresponding workflow operations.</summary>
        /// <param name="tasks">Completed tasks with their identifiers, original claim Start timestamps, and optional Result errors and Data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        /// The caller must have access to the assigned agent. Stale or missing claim timestamps are rejected.
        /// An empty Result denotes success. The first persisted result is retained across repeated reports.
        /// Task and operation state are committed together before registered lifecycle events are published.
        /// </remarks>
        /// <returns>A task representing completion of the request to record task completion and finish the corresponding workflow operations.</returns>
        [System.ComponentModel.Description("Records task completion and finishes the corresponding workflow operations.")]
        Task DoneAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken = default);

        /// <summary>Restart tasks.</summary>
        /// <param name="tasks">Restarted task(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to restart tasks.</returns>
        [System.ComponentModel.Description("Restart tasks.")]
        Task RestartAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Claim all pending tasks for one idle meter, preferring mapped meters before the oldest unmapped meter.
        /// </summary>
        /// <remarks>
        /// Claiming sets Start and OperatingAgent and creates a workflow operation in the same transaction.
        /// Registered start events publish after commit. Start identifies task allocation, not physical device communication.
        /// Completion reports must return the unchanged Start timestamp.
        /// </remarks>
        /// <param name="connectionInfo">Connection information.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="DeviceId">Device Id</param>
        /// <param name="gatewayId">GatewayId Id</param>
        /// <param name="listener">Is agent in listener mode.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Collections of tasks to execute.</returns>
        [System.ComponentModel.Description("Claim all pending tasks for one idle meter, preferring mapped meters before the oldest unmapped meter.")]
        Task<IEnumerable<GXTask>> GetNextAsync(
            string? connectionInfo,
            Guid agentId,
            Guid? DeviceId,
            Guid? gatewayId,
            bool listener, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the selected tasks.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear tasks.</returns>
        [System.ComponentModel.Description("Clear tasks. The cancellation token.")]
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
