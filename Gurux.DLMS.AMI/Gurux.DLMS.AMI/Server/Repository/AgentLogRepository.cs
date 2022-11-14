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
using Gurux.DLMS.AMI.Client.Pages.Device;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class AgentLogRepository : IAgentLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IAgentRepository _agentRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgentLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IAgentRepository agentRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _agentRepository = agentRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXAgentLog> errors)
        {
            Dictionary<GXAgentLog, List<string>> updates = new Dictionary<GXAgentLog, List<string>>();
            foreach (GXAgentLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _agentRepository.GetUsersAsync(User, it.Agent.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddAgentLogs(it.Value, new GXAgentLog[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXAgentLog> AddAsync(ClaimsPrincipal User, GXAgent agent, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXAgentLog, List<string>> updates = new Dictionary<GXAgentLog, List<string>>();
            GXAgentLog error = new GXAgentLog()
            {
                CreationTime = now,
                Message = ex.Message,
                Agent = agent
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _agentRepository.GetUsersAsync(User, agent.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddAgentLogs(it.Value, new GXAgentLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal User, Guid[]? agents)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.AgentLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentsByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXAgent>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXAgent>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXAgent>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXAgent, GXAgentLog>(y => y.Id, x => x.Agent);
            if (agents != null && agents.Any())
            {
                arg.Where.And<GXAgent>(w => agents.Contains(w.Id));
            }
            List<GXAgent>? logs = await _host.Connection.SelectAsync<GXAgent>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _agentRepository.GetUsersAsync(User, agents));
            if (admin && (agents == null || !agents.Any()))
            {
                //Admin clears all agent logs.
                _host.Connection.Truncate<GXAgentLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXAgentLog>(w => logs.Contains(w.Agent));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearAgentLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.AgentLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXAgent>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXAgent>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXAgentLog, GXAgent>(j => j.Agent, j => j.Id);
            List<GXAgentLog> list = (await _host.Connection.SelectAsync<GXAgentLog>(arg));
            Dictionary<GXAgentLog, List<string>> updates = new Dictionary<GXAgentLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _agentRepository.GetUsersAsync(User, it.Agent.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXAgentLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseAgentLogs(it.Value, new GXAgentLog[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXAgentLog[]> ListAsync(
            ClaimsPrincipal user,
            ListAgentLogs? request,
            ListAgentLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXAgentLog>();
                arg.Joins.AddInnerJoin<GXAgentLog, GXAgent>(j => j.Agent, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetAgentErrorsByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Descending = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgentLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Columns.Add<GXAgent>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXAgent>(e => e.Logs);
            arg.OrderBy.Add<GXAgentLog>(q => q.CreationTime);
            GXAgentLog[] logs = (await _host.Connection.SelectAsync<GXAgentLog>(arg, cancellationToken)).ToArray();
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
        public async Task<GXAgentLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentLog>(where => where.Id == id);
            //Get agent.
            arg.Columns.Add<GXAgent>();
            arg.Joins.AddInnerJoin<GXAgentLog, GXAgent>(x => x.Agent, y => y.Id);
            //Errors are ignored from the agent
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXAgent>(e => e.Logs);
            GXAgentLog error = (await _host.Connection.SingleOrDefaultAsync<GXAgentLog>(arg));
            if (error == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
