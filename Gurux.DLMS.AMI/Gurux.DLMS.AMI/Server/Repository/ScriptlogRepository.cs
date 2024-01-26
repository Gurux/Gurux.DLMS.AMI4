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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Pages.User;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ScriptLogRepository : IScriptLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IScriptRepository _scriptRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IScriptRepository scriptRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _scriptRepository = scriptRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXScriptLog> errors)
        {
            Dictionary<GXScriptLog, List<string>> updates = new Dictionary<GXScriptLog, List<string>>();
            foreach (GXScriptLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _scriptRepository.GetUsersAsync(User, it.Script.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddScriptLogs(it.Value, new GXScriptLog[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXScriptLog> AddAsync(ClaimsPrincipal User, GXScript script, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXScriptLog, List<string>> updates = new Dictionary<GXScriptLog, List<string>>();
            GXScriptLog error = new GXScriptLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Script = script
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _scriptRepository.GetUsersAsync(User, script.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddScriptLogs(it.Value, new GXScriptLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc/>
        public async Task ClearAsync(ClaimsPrincipal user, Guid[]? scripts)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.ScriptLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScriptsByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXScript>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXScript>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXScript, GXScriptLog>(y => y.Id, x => x.Script);
            if (scripts != null && scripts.Any())
            {
                arg.Where.And<GXScript>(w => scripts.Contains(w.Id));
            }
            List<GXScript>? logs = await _host.Connection.SelectAsync<GXScript>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their scripts are cleared.
            list.AddDistinct(await _scriptRepository.GetUsersAsync(user, scripts));
            if (admin && (scripts == null || !scripts.Any()))
            {
                //Admin clears all schedule logs.
                _host.Connection.Truncate<GXScriptLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXScriptLog>(w => logs.Contains(w.Script));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearScriptLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScriptLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXScript>();
            arg.Joins.AddInnerJoin<GXScriptLog, GXScript>(j => j.Script, j => j.Id);
            List<GXScriptLog> list = (await _host.Connection.SelectAsync<GXScriptLog>(arg));
            Dictionary<GXScriptLog, List<string>> updates = new Dictionary<GXScriptLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _scriptRepository.GetUsersAsync(User, it.Script.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXScriptLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseScriptLogs(it.Value, new GXScriptLog[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXScriptLog[]> ListAsync(
            ClaimsPrincipal user,
            ListScriptLogs? request,
            ListScriptLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXScriptLog>();
                arg.Joins.AddInnerJoin<GXScriptLog, GXScript>(j => j.Script, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScriptLogsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXScriptLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXScriptLog>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScriptLog>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXScriptLog>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXScriptLog>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Columns.Add<GXScript>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXScript>(e => e.Logs);
            GXScriptLog[] logs = (await _host.Connection.SelectAsync<GXScriptLog>(arg)).ToArray();
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
        public async Task<GXScriptLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptLog>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXScript>();
            arg.Joins.AddInnerJoin<GXScriptLog, GXScript>(x => x.Script, y => y.Id);
            //Logs are ignored from the script 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXScript>(e => e.Logs);
            GXScriptLog error = (await _host.Connection.SingleOrDefaultAsync<GXScriptLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
