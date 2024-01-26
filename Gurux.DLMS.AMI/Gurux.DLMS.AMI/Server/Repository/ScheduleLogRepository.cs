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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ScheduleLogRepository : IScheduleLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXScheduleLog> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleLog, List<string>> updates = new Dictionary<GXScheduleLog, List<string>>();
            foreach (GXScheduleLog it in errors)
            {
                it.CreationTime = now;
                if (it.Schedule != null)
                {
                    updates[it] = await _scheduleRepository.GetUsersAsync(User, it.Schedule.Id);
                }
                else
                {
                    //Get users to notify.
                    updates[it] = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin, GXRoles.ScheduleLog });
                }
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                GXScheduleLog tmp = new GXScheduleLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level
                };
                await _eventsNotifier.AddScheduleLog(it.Value,
                    new GXScheduleLog[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXScheduleLog> AddAsync(ClaimsPrincipal User, GXSchedule schedule, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleLog, List<string>> updates = new Dictionary<GXScheduleLog, List<string>>();
            GXScheduleLog error = new GXScheduleLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Schedule = schedule
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _scheduleRepository.GetUsersAsync(User, schedule.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddScheduleLog(it.Value, new GXScheduleLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? schedules)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.DeviceActionManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSchedulesByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXSchedule>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXSchedule>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXSchedule, GXScheduleLog>(y => y.Id, x => x.Schedule);
            if (schedules != null && schedules.Any())
            {
                arg.Where.And<GXSchedule>(w => schedules.Contains(w.Id));
            }
            List<GXSchedule>? logs = await _host.Connection.SelectAsync<GXSchedule>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users that log is cleared.
            list.AddDistinct(await _scheduleRepository.GetUsersAsync(user, schedules));
            if (admin && (schedules == null || !schedules.Any()))
            {
                //Admin clears all schedule logs.
                _host.Connection.Truncate<GXScheduleLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXScheduleLog>(w => logs.Contains(w.Schedule));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearScheduleLog(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScheduleLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXSchedule>();
            arg.Joins.AddInnerJoin<GXScheduleLog, GXSchedule>(j => j.Schedule, j => j.Id);
            List<GXScheduleLog> list = (await _host.Connection.SelectAsync<GXScheduleLog>(arg));
            Dictionary<GXScheduleLog, List<string>> updates = new Dictionary<GXScheduleLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _scheduleRepository.GetUsersAsync(User, it.Schedule.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXScheduleLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    GXScheduleLog tmp = new GXScheduleLog(TraceLevel.Error) { Id = it.Key.Id };
                    await _eventsNotifier.CloseScheduleLog(it.Value, new GXScheduleLog[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXScheduleLog[]> ListAsync(
            ClaimsPrincipal user,
            ListScheduleLogs? request,
            ListScheduleLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXScheduleLog>();
                arg.Joins.AddInnerJoin<GXScheduleLog, GXSchedule>(j => j.Schedule, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScheduleLogsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXScheduleLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXScheduleLog>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScheduleLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXScheduleLog>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXScheduleLog>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Columns.Add<GXSchedule>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXSchedule>(e => e.Logs);

            GXScheduleLog[] logs = (await _host.Connection.SelectAsync<GXScheduleLog>(arg)).ToArray();
            if (response != null)
            {
                response.Logs = logs;
                if (response.Count == 0)
                {
                    response.Count = logs.Length;
                }
            }
            return logs;
        }

        /// <inheritdoc />
        public async Task<GXScheduleLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleLog>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXSchedule>();
            arg.Joins.AddInnerJoin<GXScheduleLog, GXSchedule>(x => x.Schedule, y => y.Id);
            //Logs are ignored from the schedule 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXSchedule>(e => e.Logs);
            GXScheduleLog error = (await _host.Connection.SingleOrDefaultAsync<GXScheduleLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
