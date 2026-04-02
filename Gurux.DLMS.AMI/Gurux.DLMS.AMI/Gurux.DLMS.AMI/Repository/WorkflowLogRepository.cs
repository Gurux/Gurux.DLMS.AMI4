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
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class WorkflowLogRepository : IWorkflowLogRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowLogRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IWorkflowRepository workflowRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.WorkflowLog) &&
                !user.IsInRole(GXRoles.WorkflowLogManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _workflowRepository = workflowRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXWorkflowLog> errors)
        {
            Dictionary<GXWorkflowLog, List<string>> updates = new Dictionary<GXWorkflowLog, List<string>>();
            foreach (GXWorkflowLog it in errors)
            {
                if (it?.Workflow?.Id == null || it.Workflow.Id == Guid.Empty)
                {
                    throw new ArgumentException("Invalid workflow ID.");
                }
                it.CreationTime = DateTime.Now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.WorkflowLog, type);
                updates[it] = await _workflowRepository.GetUsersAsync(it.Workflow.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                                  it.Value, TargetType.WorkflowLog, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.AddWorkflowLogs(users, [it.Key]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXWorkflowLog> AddAsync(string type, GXWorkflow workflow, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflowLog, List<string>> updates = new Dictionary<GXWorkflowLog, List<string>>();
            GXWorkflowLog error = new GXWorkflowLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Workflow = workflow,
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.WorkflowLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _workflowRepository.GetUsersAsync(workflow.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddWorkflowLogs(it.Value, [it.Key]);
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<Guid>? workflows)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.WorkflowLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowsByUser(s => s.Id, id);
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
            List<string>? list = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            //Notification users if their actions are cleared.
            list.AddDistinct(await _workflowRepository.GetUsersAsync(workflows));
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
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.WorkflowLog);
            list = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                list, TargetType.WorkflowLog, NotificationAction.Clear);
            if (list != null && list.Any())
            {
                await _eventsNotifier.ClearWorkflowLogs(list, logs);
            }
            await _systemLogRepository.AddAsync(TargetType.WorkflowLog,
                [new GXSystemLog(TraceLevel.Info)
             {
                 Message = Properties.Resources.Clear
             }]);
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> errors)
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
                updates[it] = await _workflowRepository.GetUsersAsync(it.Workflow.Id);
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

            ListWorkflowLogs? request,
            ListWorkflowLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXWorkflowLog>();
                arg.Joins.AddInnerJoin<GXWorkflowLog, GXWorkflow>(j => j.Workflow, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowLogsByUser(s => s.Id, userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXWorkflowLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXWorkflowLog>(w => request.Included.Contains(w.Id));
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
        public async Task<GXWorkflowLog> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflowLog>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXWorkflow>(c => new { c.Id, c.Name });
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
