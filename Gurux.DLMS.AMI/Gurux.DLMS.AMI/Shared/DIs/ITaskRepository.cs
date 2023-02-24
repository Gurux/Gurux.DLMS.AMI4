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
        Task<GXTask[]> ListAsync(
            ClaimsPrincipal user,
            ListTasks? request,
            ListTasksResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read task information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Task id.</param>
        /// <returns></returns>
        Task<GXTask> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Add new tasks.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="tasks">Updated task(s).</param>
        Task<Guid[]> AddAsync(
            ClaimsPrincipal user,
            IEnumerable<GXTask> tasks);

        /// <summary>
        /// Delete task(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="tasks">Deleted task(s).</param>
        Task DeleteAsync(
            ClaimsPrincipal user,
            IEnumerable<Guid> tasks);

        /// <summary>
        /// Mark task(s) to complete.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="tasks">Completed task(s).</param>
        Task DoneAsync(
            ClaimsPrincipal user,
            IEnumerable<GXTask> tasks);

        /// <summary>
        /// Restart task(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="tasks">Restarted task(s).</param>
        Task RestartAsync(
            ClaimsPrincipal user,
            IEnumerable<GXTask> tasks);

        /// <summary>
        /// Agent asks for the next tasks to execute.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="DeviceId">Device Id</param>
        /// <param name="listener">Is agent in listener mode.</param>
        /// <returns>Collections of tasks to execute.</returns>
        Task<GXTask[]> GetNextAsync(ClaimsPrincipal user, Guid agentId, Guid? DeviceId, bool listener);

        /// <summary>
        /// Clear tasks.
        /// </summary>
        /// <param name="user">Current user.</param>
        Task ClearAsync(ClaimsPrincipal user);
    }
}
