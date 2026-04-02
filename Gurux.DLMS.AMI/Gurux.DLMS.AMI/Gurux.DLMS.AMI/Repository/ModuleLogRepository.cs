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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ModuleLogRepository : IModuleLogRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IModuleRepository _moduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IModuleRepository moduleRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _moduleRepository = moduleRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXModuleLog> errors)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ModuleLog) &&
                !User.HasScope(GXModuleLogPolicies.Add)))
            {
                throw new UnauthorizedAccessException();
            }

            DateTime now = DateTime.Now;
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            foreach (GXModuleLog it in errors)
            {
                it.CreationTime = now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.ModuleLog, type);
                if (it.Module?.Id is string id)
                {
                    updates[it] = await _moduleRepository.GetUsersAsync(id);
                }
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                GXModuleLog tmp = new GXModuleLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level,
                };
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.ModuleLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddModuleLogs(users, [tmp]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXModuleLog> AddAsync(string type, GXModule module, Exception ex)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ModuleLog) &&
                !User.HasScope(GXModuleLogPolicies.Add)))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            GXModuleLog error = new GXModuleLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Module = module,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.ModuleLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _moduleRepository.GetUsersAsync(module.Id);
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                                  it.Value, TargetType.ModuleLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddModuleLogs(users, [it.Key]);
                }
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(string[]? modules)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ModuleLog) &&
                !User.HasScope(GXModuleLogPolicies.Clear)))
            {
                throw new UnauthorizedAccessException();
            }
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ModuleLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModulesByUser(s => s.Id, id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXModule>();
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXModule>(s => s.Id);
            arg.Joins.AddInnerJoin<GXModule, GXModuleLog>(y => y.Id, x => x.Module);
            if (modules != null && modules.Any())
            {
                arg.Where.And<GXModule>(w => modules.Contains(w.Id));
            }
            List<GXModule>? logs = await _host.Connection.SelectAsync<GXModule>(arg);
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
            users.AddDistinct(await _moduleRepository.GetUsersAsync(modules));
            if (admin && (modules == null || !modules.Any()))
            {
                //Admin clears all module logs.
                _host.Connection.Truncate<GXModuleLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXModuleLog>(w => logs.Contains(w.Module));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.ModuleLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.ModuleLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearModuleLogs(users, logs);
            }
            await _systemLogRepository.AddAsync(TargetType.ModuleLog,
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
                !User.IsInRole(GXRoles.ModuleLog) &&
                !User.HasScope(GXModuleLogPolicies.Close)))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModuleLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXModule>();
            arg.Joins.AddInnerJoin<GXModuleLog, GXModule>(j => j.Module, j => j.Id);
            List<GXModuleLog> list = (await _host.Connection.SelectAsync<GXModuleLog>(arg));
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _moduleRepository.GetUsersAsync(it.Module.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXModuleLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    GXModuleLog tmp = new GXModuleLog(TraceLevel.Error) { Id = it.Key.Id };
                    await _eventsNotifier.CloseModuleLogs(it.Value, new GXModuleLog[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXModuleLog[]> ListAsync(
            ListModuleLogs? request,
            ListModuleLogsResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
               (!User.IsInRole(GXRoles.Admin) &&
               !User.IsInRole(GXRoles.ModuleLog) &&
               !User.HasScope(GXModuleLogPolicies.View)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the module logs.
                arg = GXSelectArgs.SelectAll<GXModuleLog>();
                arg.Columns.Add<GXModule>(c => c.Id);
                arg.Joins.AddInnerJoin<GXModuleLog, GXModule>(j => j.Module, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModuleLogsByUser(s => s.Id, userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXModuleLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXModuleLog>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXModuleLog>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXModuleLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXModuleLog>(q => q.CreationTime);
            }
            arg.Columns.Exclude<GXModule>(e => e.Logs);
            GXModuleLog[] logs = (await _host.Connection.SelectAsync<GXModuleLog>(arg)).ToArray();
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
        public async Task<GXModuleLog> ReadAsync(Guid id)
        {
            if (User == null ||
               (!User.IsInRole(GXRoles.Admin) &&
               !User.IsInRole(GXRoles.ModuleLog) &&
               !User.HasScope(GXModuleLogPolicies.View)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModuleLog>(where => where.Id == id);
            //Get module log.
            arg.Columns.Add<GXModule>();
            arg.Joins.AddInnerJoin<GXModuleLog, GXModule>(x => x.Module, y => y.Id);
            //Logs are ignored from the module. 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXModule>(e => e.Logs);
            GXModuleLog error = (await _host.Connection.SingleOrDefaultAsync<GXModuleLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
