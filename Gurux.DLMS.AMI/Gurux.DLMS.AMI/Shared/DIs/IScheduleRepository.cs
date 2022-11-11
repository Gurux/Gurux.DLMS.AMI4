﻿//
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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    public interface IScheduleRepository
    {
        /// <summary>
        /// List schedules.
        /// </summary>
        /// <returns>Schedules.</returns>
        Task<GXSchedule[]> ListAsync(
            ClaimsPrincipal user,
            ListSchedules? request,
            ListSchedulesResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read schedule.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Schedule id.</param>
        /// <returns></returns>
        Task<GXSchedule> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update schedule(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="schedulers">Updated schedule(s).</param>
        Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXSchedule> schedulers);

        /// <summary>
        /// Delete schedule(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="schedulers">Schedule(s) to delete.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> schedulers);

        /// <summary>
        /// Get all users that can access this scheduler.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="scheduleId">Schedule id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? scheduleId);

        /// <summary>
        /// Get all users that can access schedulers.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="scheduleIds">Schedule ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? scheduleIds);

        /// <summary>
        /// Update schedule execution start time.
        /// </summary>
        /// <param name="schedule">Schedule to update.</param>
        void UpdateExecutionTime(GXSchedule schedule);

        /// <summary>
        /// Run the schedule.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Schedule id.</param>
        Task RunAsync(
            ClaimsPrincipal User,
            Guid id);
    }
}
