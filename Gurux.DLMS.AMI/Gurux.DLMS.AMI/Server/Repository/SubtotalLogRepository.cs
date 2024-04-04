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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class SubtotalLogRepository : ISubtotalLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly ISubtotalRepository _subtotalRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            ISubtotalRepository subtotalRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _subtotalRepository = subtotalRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXSubtotalLog> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotalLog, List<string>> updates = new Dictionary<GXSubtotalLog, List<string>>();
            foreach (GXSubtotalLog it in errors)
            {
                it.CreationTime = now;
                updates[it] = await _subtotalRepository.GetUsersAsync(User, it.Subtotal.Id);
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
                await _eventsNotifier.AddSubtotalLogs(it.Value, new GXSubtotalLog[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXSubtotalLog> AddAsync(ClaimsPrincipal User, GXSubtotal subtotal, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotalLog, List<string>> updates = new Dictionary<GXSubtotalLog, List<string>>();
            GXSubtotalLog error = new GXSubtotalLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Subtotal = subtotal
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _subtotalRepository.GetUsersAsync(User, subtotal.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddSubtotalLogs(it.Value, new GXSubtotalLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal User, Guid[]? subtotals)
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
                arg = GXQuery.GetSubtotalsByUser(id);
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
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _subtotalRepository.GetUsersAsync(User, subtotals));
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
            await _eventsNotifier.ClearSubtotalLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
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
                updates[it] = await _subtotalRepository.GetUsersAsync(User, it.Subtotal.Id);
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
            ClaimsPrincipal user,
            ListSubtotalLogs? request,
            ListSubtotalLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXSubtotalLog>();
                arg.Joins.AddInnerJoin<GXSubtotalLog, GXSubtotal>(j => j.Subtotal, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
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
        public async Task<GXSubtotalLog> ReadAsync(ClaimsPrincipal User, Guid id)
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
