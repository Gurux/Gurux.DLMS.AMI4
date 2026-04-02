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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ScheduleLogRepository : IScheduleLogRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IScheduleRepository scheduleRepository,
            IEnumTypeRepository enumTypeRepository,
            IUserRepository userRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXScheduleLog> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleLog, List<string>> updates = new Dictionary<GXScheduleLog, List<string>>();
            foreach (GXScheduleLog it in errors)
            {
                it.CreationTime = now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.ScheduleLog, type);
                if (it.Schedule != null)
                {
                    updates[it] = await _scheduleRepository.GetUsersAsync(it.Schedule.Id);
                }
                else
                {
                    //Get users to notification.
                    if (User.IsInRole(GXRoles.Admin))
                    {
                        updates[it] = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin, GXRoles.ScheduleLog]);
                    }
                    else
                    {
                        updates[it] = await _userRepository.GetUserIdsInRoleAsync([GXRoles.ScheduleLog]);
                    }

                }
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                GXScheduleLog tmp = new GXScheduleLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level,
                    Message = it.Key.Message,
                };
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                                  it.Value, TargetType.ScheduleLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddScheduleLog(users, [tmp]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXScheduleLog> AddAsync(string type,
            GXSchedule schedule,
            Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleLog, List<string>> updates = new Dictionary<GXScheduleLog, List<string>>();
            GXScheduleLog error = new GXScheduleLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Schedule = schedule,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.ScheduleLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _scheduleRepository.GetUsersAsync(schedule.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddScheduleLog(it.Value, [it.Key]);
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<Guid>? schedules)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetSchedulesByUser(s => s.Id, id);
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
            List<GXSchedule>? list = await _host.Connection.SelectAsync<GXSchedule>(arg);
            List<string>? users;
            if (User.IsInRole(GXRoles.Admin))
            {
                users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            }
            else
            {
                users = new List<string>();
            }
            //Notification users that log is cleared.
            users.AddDistinct(await _scheduleRepository.GetUsersAsync(schedules));
            if (admin && (schedules == null || !schedules.Any()))
            {
                //Admin clears all schedule logs.
                _host.Connection.Truncate<GXScheduleLog>();
                list = null;
            }
            else if (list.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXScheduleLog>(w => list.Contains(w.Schedule));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.ScheduleLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.ScheduleLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearScheduleLog(users, list);
            }
            await _systemLogRepository.AddAsync(TargetType.ScheduleLog,
             [new GXSystemLog(TraceLevel.Info)
         {
             Message = Properties.Resources.Clear
         }]);
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> errors)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleLogManager)))
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
                updates[it] = await _scheduleRepository.GetUsersAsync(it.Schedule.Id);
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
            ListScheduleLogs? request,
            ListScheduleLogsResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleLog) &&
                !User.IsInRole(GXRoles.ScheduleLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXScheduleLog>();
                arg.Joins.AddInnerJoin<GXScheduleLog, GXSchedule>(j => j.Schedule, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScheduleLogsByUser(s => s.Id, userId);
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
        public async Task<GXScheduleLog> ReadAsync(Guid id)
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
