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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class SystemLogRepository : ISystemLogRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SystemLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
        }

        /// <summary>
        /// Get all users that can access system errors.
        /// </summary>
        /// <returns></returns>
        List<string> GetUsers()
        {
            string[] names = { GXRoles.Admin, GXRoles.SystemLogManager };
            GXSelectArgs args = GXSelectArgs.Select<GXUserRole>(s => s.UserId);
            args.Joins.AddInnerJoin<GXUserRole, GXRole>(a => a.RoleId, b => b.Id);
            args.Where.And<GXRole>(where => GXSql.In(where.Name, names));
            List<GXUserRole> users = _host.Connection.Select<GXUserRole>(args);
            return users.Select(s => s.UserId).ToList();
        }

        /// <inheritdoc />
        public async Task<GXSystemLog[]> ListAsync(
            ListSystemLogs? request,
            ListSystemLogsResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLog) &&
                !User.HasScope(GXSystemLogPolicies.View)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSystemLog>();
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXSystemLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXSystemLog>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSystemLog>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSystemLog>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSystemLog>(q => GXSql.DistinctCount(q.Id));
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXSystemLog[] logs = (await _host.Connection.SelectAsync<GXSystemLog>(arg)).ToArray();
            if (response != null)
            {
                response.Logs = logs;
            }
            return logs;
        }

        /// <inheritdoc/>
        public async Task<GXSystemLog> ReadAsync(Guid id)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSystemLog>(w => w.Id == id);
            arg.Distinct = true;
            GXSystemLog log = await _host.Connection.SingleOrDefaultAsync<GXSystemLog>(arg);
            if (log == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return log;
        }

        /// <inheritdoc/>
        public async Task ClearAsync()
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            _host.Connection.Truncate<GXSystemLog>();
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.SystemLog);
            var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                GetUsers(), TargetType.SystemLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearSystemLogs(users);
            }
            await AddAsync(TargetType.SystemLog, [new GXSystemLog(TraceLevel.Info)
            {
                Message = Properties.Resources.Clear
            }]);
        }

        /// <inheritdoc/>
        public async Task AddAsync(string type, IEnumerable<GXSystemLog> logs)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            foreach (var it in logs)
            {
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, type);
                it.CreationTime = DateTime.Now;
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(logs));
            var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                GetUsers(), TargetType.SystemLog, NotificationAction.Add);
            if (users != null && users.Any())
            {
                foreach (var it in logs)
                {
                    if (it.Message != null)
                    {
                        int pos = it.Message.IndexOf(Environment.NewLine);
                        if (pos != -1)
                        {
                            it.Message = it.Message.Substring(0, pos);
                        }
                    }
                }
                await _eventsNotifier.AddSystemLogs(users, logs);
            }
        }

        /// <inheritdoc/>
        public async Task<GXSystemLog> AddAsync(string type, Exception error)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSystemLog e = new GXSystemLog()
            {
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, type),
                CreationTime = DateTime.Now,
                Message = error.Message,
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(e));
            var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                GetUsers(), TargetType.SystemLog, NotificationAction.Add);
            if (users != null && users.Any())
            {
                int pos = e.Message.IndexOf(Environment.NewLine);
                if (pos != -1)
                {
                    e.Message = e.Message.Substring(0, pos);
                }
                await _eventsNotifier.AddSystemLogs(users, [e]);
            }
            return e;
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid>? errors)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.SystemLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            List<GXSystemLog> list = new List<GXSystemLog>();
            DateTime now = DateTime.Now;
            foreach (Guid it in errors)
            {
                list.Add(new GXSystemLog() { Id = it, Closed = now });
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    GetUsers(), TargetType.SystemLog, NotificationAction.Close);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.CloseSystemLogs(users, list);
                }
            }
        }

        public async Task DeleteAsync(IEnumerable<GXSystemLog>? logs)
        {
            throw new NotImplementedException();
        }
    }
}
