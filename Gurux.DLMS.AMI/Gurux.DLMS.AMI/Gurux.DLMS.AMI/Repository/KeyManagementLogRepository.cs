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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class KeyManagementLogRepository : IKeyManagementLogRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IKeyManagementRepository _deviceKeyRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IKeyManagementRepository deviceKeyRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.KeyManagementLog) &&
                !user.IsInRole(GXRoles.KeyManagementLogManager) &&
                //If agent
                !user.Claims.Where(w => w.Type.Equals("scope") && w.Value.Equals(GXAgentPolicies.View)).Any()))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _deviceKeyRepository = deviceKeyRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXKeyManagementLog> errors)
        {
            Dictionary<GXKeyManagementLog, List<string>> updates = new Dictionary<GXKeyManagementLog, List<string>>();
            foreach (GXKeyManagementLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.KeyManagementLog, type);
                updates[it] = await _deviceKeyRepository.GetUsersAsync(it.KeyManagement.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.KeyManagementLog, NotificationAction.Add);
                if (users == null)
                {
                    break;
                }
                await _eventsNotifier.AddKeyManagementLogs(users, [it.Key]);
            }
        }

        /// <inheritdoc />
        public async Task<GXKeyManagementLog> AddAsync(string type, GXKeyManagement key, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXKeyManagementLog, List<string>> updates = new Dictionary<GXKeyManagementLog, List<string>>();
            GXKeyManagementLog log = new GXKeyManagementLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                KeyManagement = key,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.KeyManagementLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(log));
            updates[log] = await _deviceKeyRepository.GetUsersAsync(key.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddKeyManagementLogs(it.Value, [it.Key]);
            }
            return log;
        }

        /// <inheritdoc/>
        public async Task ClearAsync(Guid[]? deviceKeys)
        {
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetKeyManagementsByUser(s => "*", id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXKeyManagement>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXKeyManagement>(s => new { s.Id, s.SystemTitle });
            arg.Joins.AddInnerJoin<GXKeyManagement, GXKeyManagementLog>(y => y.Id, x => x.KeyManagement);
            if (deviceKeys != null && deviceKeys.Any())
            {
                arg.Where.And<GXKeyManagement>(w => deviceKeys.Contains(w.Id));
            }
            List<GXKeyManagement>? logs = await _host.Connection.SelectAsync<GXKeyManagement>(arg);
            List<string>? users;
            if (User.IsInRole(GXRoles.Admin))
            {
                users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            }
            else
            {
                users = new List<string>();
            }
            //Notification users if their deviceKeys are cleared.
            users.AddDistinct(await _deviceKeyRepository.GetUsersAsync(deviceKeys));
            if (admin && (deviceKeys == null || !deviceKeys.Any()))
            {
                //Admin clears all schedule logs.
                _host.Connection.Truncate<GXKeyManagementLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXKeyManagementLog>(w => logs.Contains(w.KeyManagement));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.KeyManagementLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.KeyManagementLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearKeyManagementLogs(users, logs);
            }
            await _systemLogRepository.AddAsync(TargetType.KeyManagementLog,
                [new GXSystemLog(TraceLevel.Info)
             {
                 Message = Properties.Resources.Clear
             }]);
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> errors)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagementLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXKeyManagement>();
            arg.Joins.AddInnerJoin<GXKeyManagementLog, GXKeyManagement>(j => j.KeyManagement, j => j.Id);
            List<GXKeyManagementLog> list = (await _host.Connection.SelectAsync<GXKeyManagementLog>(arg));
            Dictionary<GXKeyManagementLog, List<string>> updates = new Dictionary<GXKeyManagementLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _deviceKeyRepository.GetUsersAsync(it.KeyManagement.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXKeyManagementLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseKeyManagementLogs(it.Value, [it.Key]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXKeyManagementLog[]> ListAsync(

            ListKeyManagementLogs? request,
            ListKeyManagementLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXKeyManagementLog>();
                arg.Joins.AddInnerJoin<GXKeyManagementLog, GXKeyManagement>(j => j.KeyManagement, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetKeyManagementLogsByUser(s => "*", userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXKeyManagementLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXKeyManagementLog>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXKeyManagementLog>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXKeyManagementLog>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXKeyManagementLog>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Columns.Add<GXKeyManagement>(c => new { c.Id, c.SystemTitle });
            arg.Columns.Exclude<GXKeyManagement>(e => e.Logs);
            GXKeyManagementLog[] logs = (await _host.Connection.SelectAsync<GXKeyManagementLog>(arg)).ToArray();
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
        public async Task<GXKeyManagementLog> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagementLog>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXKeyManagement>();
            arg.Joins.AddInnerJoin<GXKeyManagementLog, GXKeyManagement>(x => x.KeyManagement, y => y.Id);
            //Logs are ignored from the key management 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXKeyManagement>(e => e.Logs);
            GXKeyManagementLog error = (await _host.Connection.SingleOrDefaultAsync<GXKeyManagementLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
