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
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class NotificationLogRepository : INotificationLogRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.NotificationLog) &&
                !user.IsInRole(GXRoles.NotificationLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXNotificationLog> logs)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXNotificationLog, List<string>> updates = new Dictionary<GXNotificationLog, List<string>>();
            foreach (GXNotificationLog it in logs)
            {
                it.CreationTime = now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.NotificationLog, type);
                updates[it] = await _notificationRepository.GetUsersAsync(it.Notification.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(logs));
            foreach (var it in updates)
            {
                GXNotificationLog tmp = new GXNotificationLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level,
                };
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.NotificationLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddNotificationLogs(users, [tmp]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXNotificationLog> AddAsync(string type, GXNotification notification, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXNotificationLog, List<string>> updates = new Dictionary<GXNotificationLog, List<string>>();
            GXNotificationLog error = new GXNotificationLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Notification = notification,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.NotificationLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _notificationRepository.GetUsersAsync(notification.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddNotificationLogs(it.Value, new GXNotificationLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(Guid[]? notifications)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.NotificationLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetNotificationsByUser(s => s.Id, id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXNotification>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXNotification>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXNotification>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationLog>(y => y.Id, x => x.Notification);
            if (notifications != null && notifications.Any())
            {
                arg.Where.And<GXNotification>(w => notifications.Contains(w.Id));
            }
            List<GXNotification>? logs = await _host.Connection.SelectAsync<GXNotification>(arg);
            List<string>? users;
            if (User.IsInRole(GXRoles.Admin))
            {
                users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            }
            else
            {
                users = new List<string>();
            }
            //Notification users if their actions are cleared.
            users.AddDistinct(await _notificationRepository.GetUsersAsync(notifications));
            if (admin && (notifications == null || !notifications.Any()))
            {
                //Admin clears all notification logs.
                _host.Connection.Truncate<GXNotificationLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXNotificationLog>(w => logs.Contains(w.Notification));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.NotificationLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.NotificationLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearNotificationLogs(users, logs);
            }
            await _systemLogRepository.AddAsync(TargetType.NotificationLog,
                 [new GXSystemLog(TraceLevel.Info)
             {
                 Message = Properties.Resources.Clear
             }]);
        }

        /// <inheritdoc />
        public async Task<GXNotificationLog[]> ListAsync(
            ListNotificationLogs? request,
            ListNotificationLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXNotificationLog>();
                arg.Joins.AddInnerJoin<GXNotificationLog, GXNotification>(j => j.Notification, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetNotificationErrorsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXNotificationLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXNotificationLog>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXNotificationLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Columns.Add<GXNotification>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXNotification>(e => e.Logs);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXNotificationLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXNotificationLog>(q => q.CreationTime);
            }
            GXNotificationLog[] logs = (await _host.Connection.SelectAsync<GXNotificationLog>(arg, cancellationToken)).ToArray();
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
        public async Task<GXNotificationLog> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXNotificationLog>(where => where.Id == id);
            //Get notification.
            arg.Columns.Add<GXNotification>();
            arg.Joins.AddInnerJoin<GXNotificationLog, GXNotification>(x => x.Notification, y => y.Id);
            //Errors are ignored from the notification
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXNotification>(e => e.Logs);
            GXNotificationLog error = (await _host.Connection.SingleOrDefaultAsync<GXNotificationLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
