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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class GatewayLogRepository : IGatewayLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IGatewayRepository _gatewayRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IGatewayRepository gatewayRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _gatewayRepository = gatewayRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXGatewayLog> errors)
        {
            Dictionary<GXGatewayLog, List<string>> updates = new Dictionary<GXGatewayLog, List<string>>();
            foreach (GXGatewayLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _gatewayRepository.GetUsersAsync(User, it.Gateway.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddGatewayLogs(it.Value, new GXGatewayLog[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXGatewayLog> AddAsync(ClaimsPrincipal User, GXGateway gateway, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXGatewayLog, List<string>> updates = new Dictionary<GXGatewayLog, List<string>>();
            GXGatewayLog error = new GXGatewayLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Gateway = gateway
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _gatewayRepository.GetUsersAsync(User, gateway.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddGatewayLogs(it.Value, new GXGatewayLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal User, Guid[]? gateways)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.GatewayLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewaysByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXGateway>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXGateway>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXGateway>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXGateway, GXGatewayLog>(y => y.Id, x => x.Gateway);
            if (gateways != null && gateways.Any())
            {
                arg.Where.And<GXGateway>(w => gateways.Contains(w.Id));
            }
            List<GXGateway>? logs = await _host.Connection.SelectAsync<GXGateway>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _gatewayRepository.GetUsersAsync(User, gateways));
            if (admin && (gateways == null || !gateways.Any()))
            {
                //Admin clears all gateway logs.
                _host.Connection.Truncate<GXGatewayLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXGatewayLog>(w => logs.Contains(w.Gateway));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearGatewayLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.GatewayLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGatewayLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXGateway>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXGateway>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXGatewayLog, GXGateway>(j => j.Gateway, j => j.Id);
            List<GXGatewayLog> list = (await _host.Connection.SelectAsync<GXGatewayLog>(arg));
            Dictionary<GXGatewayLog, List<string>> updates = new Dictionary<GXGatewayLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _gatewayRepository.GetUsersAsync(User, it.Gateway.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXGatewayLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseGatewayLogs(it.Value, new GXGatewayLog[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXGatewayLog[]> ListAsync(
            ClaimsPrincipal user,
            ListGatewayLogs? request,
            ListGatewayLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXGatewayLog>();
                arg.Joins.AddInnerJoin<GXGatewayLog, GXGateway>(j => j.Gateway, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetGatewayErrorsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXGatewayLog>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXGatewayLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Columns.Add<GXGateway>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXGateway>(e => e.Logs);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXGatewayLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXGatewayLog>(q => q.CreationTime);
            }
            GXGatewayLog[] logs = (await _host.Connection.SelectAsync<GXGatewayLog>(arg, cancellationToken)).ToArray();
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
        public async Task<GXGatewayLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGatewayLog>(where => where.Id == id);
            //Get gateway.
            arg.Columns.Add<GXGateway>();
            arg.Joins.AddInnerJoin<GXGatewayLog, GXGateway>(x => x.Gateway, y => y.Id);
            //Errors are ignored from the gateway
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXGateway>(e => e.Logs);
            GXGatewayLog error = (await _host.Connection.SingleOrDefaultAsync<GXGatewayLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
