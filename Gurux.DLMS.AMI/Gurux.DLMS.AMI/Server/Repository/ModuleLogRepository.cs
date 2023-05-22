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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ModuleLogRepository : IModuleLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IModuleRepository _moduleRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IModuleRepository moduleRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _moduleRepository = moduleRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXModuleLog> errors)
        {
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            foreach (GXModuleLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _moduleRepository.GetUsersAsync(User, it.Module.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddModuleLogs(it.Value, new GXModuleLog[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXModuleLog> AddAsync(ClaimsPrincipal User, GXModule module, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            GXModuleLog error = new GXModuleLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Module = module
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _moduleRepository.GetUsersAsync(User, module.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddModuleLogs(it.Value, new GXModuleLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, string[]? modules)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.ModuleLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetModulesByUser(id);
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
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _moduleRepository.GetUsersAsync(user, modules));
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
            await _eventsNotifier.ClearModuleLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModuleLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXModule>();
            arg.Joins.AddInnerJoin<GXModuleLog, GXModule>(j => j.Module, j => j.Id);
            List<GXModuleLog> list = (await _host.Connection.SelectAsync<GXModuleLog>(arg));
            Dictionary<GXModuleLog, List<string>> updates = new Dictionary<GXModuleLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _moduleRepository.GetUsersAsync(User, it.Module.Id);
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
            ClaimsPrincipal user,
            ListModuleLogs? request,
            ListModuleLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the module logs.
                arg = GXSelectArgs.SelectAll<GXModuleLog>();
                arg.Columns.Add<GXModule>(c => c.Id);
                arg.Joins.AddInnerJoin<GXModuleLog, GXModule>(j => j.Module, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetModuleLogsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXModuleLog>(w => request.Exclude.Contains(w.Id) == false);
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
        public async Task<GXModuleLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
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
