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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle schedule logs.
    /// </summary>
    public interface IScheduleLogRepository
    {
        /// <summary>
        /// List schedule logs.
        /// </summary>
        /// <returns>List of schedule logs.</returns>
        Task<GXScheduleLog[]> ListAsync(
            ClaimsPrincipal User,
            ListScheduleLogs? request,
            ListScheduleLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read schedule log information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Schedule log id.</param>
        /// <returns>Schedule information.</returns>
        Task<GXScheduleLog> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear schedule logs.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, IEnumerable<Guid>? schedules);

        /// <summary>
        /// Add schedule logs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="logs">New logs.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXScheduleLog> logs);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="schedule">Schedule.</param>
        /// <param name="ex">Exception.</param>
        Task<GXScheduleLog> AddAsync(ClaimsPrincipal User, GXSchedule schedule, Exception ex);

        /// <summary>
        /// Close schedule log(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="logs">Logs to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> logs);
    }
}
