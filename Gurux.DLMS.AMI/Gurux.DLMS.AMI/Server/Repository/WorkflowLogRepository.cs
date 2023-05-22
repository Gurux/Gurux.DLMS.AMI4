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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Pages.Module;
using Gurux.DLMS.AMI.Client.Pages.User;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class WorkflowLogRepository : IWorkflowLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IWorkflowRepository workflowRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _workflowRepository = workflowRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXWorkflowLog> errors)
        {
            Dictionary<GXWorkflowLog, List<string>> updates = new Dictionary<GXWorkflowLog, List<string>>();
            foreach (GXWorkflowLog it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _workflowRepository.GetUsersAsync(User, it.Workflow.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddWorkflowLogs(it.Value, new GXWorkflowLog[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXWorkflowLog> AddAsync(ClaimsPrincipal User, GXWorkflow workflow, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflowLog, List<string>> updates = new Dictionary<GXWorkflowLog, List<string>>();
            GXWorkflowLog error = new GXWorkflowLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Workflow = workflow
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _workflowRepository.GetUsersAsync(User, workflow.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddWorkflowLogs(it.Value, new GXWorkflowLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? workflows)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.WorkflowLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetWorkflowsByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXWorkflow>();
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXWorkflow>(s => s.Id);
            arg.Joins.AddInnerJoin<GXWorkflow, GXWorkflowLog>(y => y.Id, x => x.Workflow);
            if (workflows != null && workflows.Any())
            {
                arg.Where.And<GXWorkflow>(w => workflows.Contains(w.Id));
            }
            List<GXWorkflow>? logs = await _host.Connection.SelectAsync<GXWorkflow>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _workflowRepository.GetUsersAsync(user, workflows));
            if (admin && (workflows == null || !workflows.Any()))
            {
                //Admin clears all module logs.
                _host.Connection.Truncate<GXWorkflowLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXWorkflowLog>(w => logs.Contains(w.Workflow));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearWorkflowLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.WorkflowLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflowLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXWorkflow>();
            arg.Joins.AddInnerJoin<GXWorkflowLog, GXWorkflow>(j => j.Workflow, j => j.Id);
            List<GXWorkflowLog> list = (await _host.Connection.SelectAsync<GXWorkflowLog>(arg));
            Dictionary<GXWorkflowLog, List<string>> updates = new Dictionary<GXWorkflowLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _workflowRepository.GetUsersAsync(User, it.Workflow.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXWorkflowLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    GXWorkflowLog tmp = new GXWorkflowLog(TraceLevel.Error) { Id = it.Key.Id };
                    await _eventsNotifier.CloseWorkflowLogs(it.Value, new GXWorkflowLog[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXWorkflowLog[]> ListAsync(
            ClaimsPrincipal user,
            ListWorkflowLogs? request,
            ListWorkflowLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXWorkflowLog>();
                arg.Joins.AddInnerJoin<GXWorkflowLog, GXWorkflow>(j => j.Workflow, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetWorkflowLogsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXWorkflowLog>(w => request.Exclude.Contains(w.Id) == false);
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXWorkflowLog>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXWorkflowLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXWorkflowLog>(q => q.CreationTime);
            }
            arg.Columns.Add<GXWorkflow>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXWorkflow>(e => e.Logs);
            GXWorkflowLog[] errors = (await _host.Connection.SelectAsync<GXWorkflowLog>(arg)).ToArray();
            if (response != null)
            {
                response.Logs = errors;
                if (response.Count == 0)
                {
                    response.Count = errors.Length;
                }
            }
            return errors;
        }

        /// <inheritdoc />
        public async Task<GXWorkflowLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflowLog>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXWorkflow>(c => new {c.Id, c.Name});
            arg.Joins.AddInnerJoin<GXWorkflowLog, GXWorkflow>(x => x.Workflow, y => y.Id);
            GXWorkflowLog error = (await _host.Connection.SingleOrDefaultAsync<GXWorkflowLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
