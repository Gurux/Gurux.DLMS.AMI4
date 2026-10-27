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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle schedules.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle schedules.")]
    public interface IScheduleRepository
    {
        /// <summary>
        /// List schedules.
        /// </summary>
        /// <returns>Schedules.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List schedules.")]
        Task<IEnumerable<GXSchedule>> ListAsync(
            ListSchedules? request = null,
            ListSchedulesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read schedule.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Read schedule.</returns>
        /// <remarks>
        /// Required extra info can be used to read following extra information:
        /// TargetType.User: Creator.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read schedule.")]
        Task<GXSchedule> ReadAsync(Guid id, Expression<Func<GXSchedule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new schedules or updates existing ones.
        /// </summary>
        /// <param name="schedulers">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new schedules or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXSchedule> schedulers,
            Expression<Func<GXSchedule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete schedules.</summary>
        /// <param name="schedulers">Schedule(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete schedules.")]
        Task DeleteAsync(IEnumerable<Guid> schedulers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this scheduler.
        /// </summary>
        /// <param name="scheduleId">Schedule id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this scheduler.")]
        Task<List<string>> GetUsersAsync(Guid? scheduleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access schedulers.
        /// </summary>
        /// <param name="Ids">Schedule ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access schedulers.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update schedule execution start time.
        /// </summary>
        /// <param name="schedule">Schedule to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Update schedule execution start time.")]
        Task UpdateExecutionTimeAsync(GXSchedule schedule, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run the schedule.
        /// </summary>
        /// <param name="id">Schedule id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Run the schedule.")]
        Task RunAsync(
            Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get module settings for the schedule.
        /// </summary>
        /// <param name="settings">Schedule and module id.</param>
        /// <returns>Module settings for the schedule.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get module settings for the schedule.")]
        Task<GXScheduleModule?> GetModuleSettingsAsync(
            GXScheduleModule? settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update module settings for the schedule.
        /// </summary>
        /// <param name="settings">Module settings to the schedule.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Update module settings for the schedule.")]
        Task UpdateModuleSettingsAsync(
            GXScheduleModule? settings, CancellationToken cancellationToken = default);
    }
}
