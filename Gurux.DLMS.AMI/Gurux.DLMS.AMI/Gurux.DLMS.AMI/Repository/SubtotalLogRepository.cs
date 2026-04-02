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
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class SubtotalLogRepository : ISubtotalLogRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly ISubtotalRepository _subtotalRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            ISubtotalRepository subtotalRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.SubtotalLog) &&
                !user.IsInRole(GXRoles.SubtotalLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _subtotalRepository = subtotalRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXSubtotalLog> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotalLog, List<string>> updates = new Dictionary<GXSubtotalLog, List<string>>();
            foreach (GXSubtotalLog it in errors)
            {
                it.CreationTime = now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SubtotalLog, type);
                updates[it] = await _subtotalRepository.GetUsersAsync(it.Subtotal.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                GXSubtotalLog tmp = new GXSubtotalLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level,
                };
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                                  it.Value, TargetType.SubtotalLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddSubtotalLogs(users, [tmp]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXSubtotalLog> AddAsync(string type, GXSubtotal subtotal, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotalLog, List<string>> updates = new Dictionary<GXSubtotalLog, List<string>>();
            GXSubtotalLog error = new GXSubtotalLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Subtotal = subtotal,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SubtotalLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _subtotalRepository.GetUsersAsync(subtotal.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddSubtotalLogs(it.Value, new GXSubtotalLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(Guid[]? subtotals)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SubtotalLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetSubtotalsByUser(s => s.Id, id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXSubtotal>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXSubtotal>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXSubtotal>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalLog>(y => y.Id, x => x.Subtotal);
            if (subtotals != null && subtotals.Any())
            {
                arg.Where.And<GXSubtotal>(w => subtotals.Contains(w.Id));
            }
            List<GXSubtotal>? logs = await _host.Connection.SelectAsync<GXSubtotal>(arg);
            List<string>? users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            //Notification users if their actions are cleared.
            users.AddDistinct(await _subtotalRepository.GetUsersAsync(subtotals));
            if (admin && (subtotals == null || !subtotals.Any()))
            {
                //Admin clears all subtotal logs.
                _host.Connection.Truncate<GXSubtotalLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXSubtotalLog>(w => logs.Contains(w.Subtotal));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.SubtotalLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.SubtotalLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearSubtotalLogs(users, logs);
            }
            await _systemLogRepository.AddAsync(TargetType.SubtotalLog,
                 [new GXSystemLog(TraceLevel.Info)
             {
                 Message = Properties.Resources.Clear
             }]);
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SubtotalLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSubtotalLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXSubtotal>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXSubtotal>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXSubtotalLog, GXSubtotal>(j => j.Subtotal, j => j.Id);
            List<GXSubtotalLog> list = (await _host.Connection.SelectAsync<GXSubtotalLog>(arg));
            Dictionary<GXSubtotalLog, List<string>> updates = new Dictionary<GXSubtotalLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _subtotalRepository.GetUsersAsync(it.Subtotal.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXSubtotalLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseSubtotalLogs(it.Value, new GXSubtotalLog[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXSubtotalLog[]> ListAsync(

            ListSubtotalLogs? request,
            ListSubtotalLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXSubtotalLog>();
                arg.Joins.AddInnerJoin<GXSubtotalLog, GXSubtotal>(j => j.Subtotal, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetSubtotalErrorsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXSubtotalLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXSubtotalLog>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSubtotalLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Columns.Add<GXSubtotal>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXSubtotal>(e => e.Logs);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSubtotalLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXSubtotalLog>(q => q.CreationTime);
            }
            GXSubtotalLog[] logs = (await _host.Connection.SelectAsync<GXSubtotalLog>(arg, cancellationToken)).ToArray();
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
        public async Task<GXSubtotalLog> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSubtotalLog>(where => where.Id == id);
            //Get subtotal.
            arg.Columns.Add<GXSubtotal>();
            arg.Joins.AddInnerJoin<GXSubtotalLog, GXSubtotal>(x => x.Subtotal, y => y.Id);
            //Errors are ignored from the subtotal
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXSubtotal>(e => e.Logs);
            GXSubtotalLog error = (await _host.Connection.SingleOrDefaultAsync<GXSubtotalLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
