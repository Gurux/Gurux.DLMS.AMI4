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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using System.Diagnostics;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IWorkflowGroupRepository _workflowGroupRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IWorkflowHandler workflowHandler,
            IGXEventsNotifier eventsNotifier,
            IWorkflowGroupRepository workflowGroupRepository)
        {
            _host = host;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _workflowHandler = workflowHandler;
            _eventsNotifier = eventsNotifier;
            _workflowGroupRepository = workflowGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User, Guid? workflowId)
        {
            GXSelectArgs args = GXQuery.GetUsersByWorkflow(ServerHelpers.GetUserId(User), workflowId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User, IEnumerable<Guid>? workflowIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByWorkflows(ServerHelpers.GetUserId(User), workflowIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> workflows,
            bool delete)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.WorkflowManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXWorkflow>(a => a.Id, q => workflows.Contains(q.Id));
            List<GXWorkflow> list = _host.Connection.Select<GXWorkflow>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflow, List<string>> updates = new();
            foreach (GXWorkflow workflow in list)
            {
                _workflowHandler.Delete(workflow);
                workflow.Removed = now;
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXWorkflow>(workflow.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(workflow, q => q.Removed));
                }
                updates[workflow] = await GetUsersAsync(User, workflow.Id);
            }
            foreach (var it in updates)
            {
                GXWorkflow tmp = new GXWorkflow() { Id = it.Key.Id };
                await _eventsNotifier.WorkflowDelete(it.Value, new GXWorkflow[] { tmp });
            }
            if (!delete)
            {
                List<GXWorkflowLog> logs = new List<GXWorkflowLog>();
                foreach (var it in updates.Keys)
                {
                    logs.Add(new GXWorkflowLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Workflow = it,
                        Message = Properties.Resources.WorkflowRemoved
                    });
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    var workfloLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowLogRepository>();
                    await workfloLogRepository.AddAsync(User, logs);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXWorkflow[]> ListAsync(
            ClaimsPrincipal user,
            ListWorkflows? request,
            ListWorkflowsResponse? response,
            bool includeActivity)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the workflows.
                arg = GXSelectArgs.SelectAll<GXWorkflow>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetWorkflowsByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXWorkflow>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXWorkflow>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXWorkflow>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXWorkflow>(q => q.CreationTime);
            }
            if (includeActivity)
            {
                arg.Columns.Add<GXTrigger>();
                arg.Columns.Add<GXTriggerActivity>();
                arg.Columns.Exclude<GXTrigger>(e => e.Activities);
                arg.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(x => x.TriggerActivity, y => y.Id);
                arg.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(x => x.Trigger, y => y.Id);
            }
            //Get creator information.
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXWorkflow, GXUser>(j => j.Creator, j => j.Id);

            GXWorkflow[] workflows = (await _host.Connection.SelectAsync<GXWorkflow>(arg)).ToArray();
            if (response != null)
            {
                response.Workflows = workflows;
                if (response.Count == 0)
                {
                    response.Count = workflows.Length;
                }
            }
            return workflows;
        }

        /// <inheritdoc />
        public async Task<GXWorkflow> ReadAsync(
            ClaimsPrincipal user,
            Guid id,
            bool includeScripts)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the workflows.
                arg = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXWorkflow, GXWorkflowGroupWorkflow>(x => x.Id, y => y.WorkflowId);
                arg.Joins.AddLeftJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetWorkflowsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id);
            }
            arg.Columns.Add<GXWorkflowGroup>();
            arg.Columns.Exclude<GXWorkflowGroup>(e => e.Workflows);
            arg.Distinct = true;
            GXWorkflow workflow = await _host.Connection.SingleOrDefaultAsync<GXWorkflow>(arg);
            if (workflow == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            workflow.ScriptMethods = GetScriptMethods(workflow, includeScripts);
            //Get trigger activity
            arg = GXSelectArgs.SelectAll<GXTriggerActivity>();
            arg.Columns.Add<GXTrigger>();
            arg.Columns.Exclude<GXTrigger>(e => e.Activities);
            arg.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(x => x.TriggerActivity, y => y.Id);
            arg.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(x => x.Trigger, y => y.Id);
            arg.Where.And<GXWorkflow>(w => w.Id == workflow.Id);
            workflow.TriggerActivity = _host.Connection.SingleOrDefault<GXTriggerActivity>(arg);
            //Get user.
            return workflow;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXWorkflow> workflows,
            Expression<Func<GXWorkflow, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            Dictionary<GXWorkflow, List<string>> updates = new();
            foreach (GXWorkflow workflow in workflows)
            {
                //Verify start date time.
                if (string.IsNullOrEmpty(workflow.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (workflow.WorkflowGroups == null || !workflow.WorkflowGroups.Any())
                {
                    workflow.WorkflowGroups = new List<GXWorkflowGroup>();
                    ListWorkflowGroups request = new ListWorkflowGroups() { Filter = new GXWorkflowGroup() { Default = true } };
                    workflow.WorkflowGroups.AddRange(await _workflowGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    if (!workflow.WorkflowGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
                if (workflow.Id == Guid.Empty)
                {
                    workflow.CreationTime = now;
                    workflow.Creator = user;
                    GXInsertArgs args = GXInsertArgs.Insert(workflow);
                    args.Exclude<GXWorkflow>(q => new { q.Updated, q.WorkflowGroups, q.ScriptMethods });
                    _host.Connection.Insert(args);
                    list.Add(workflow.Id);
                    AddWorkflowToWorkflowGroups(workflow.Id, workflow.WorkflowGroups);
                    AddScriptMethodsToWorkflow(workflow, workflow.ScriptMethods);
                    _workflowHandler.Add(workflow);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXWorkflow>(q => q.ConcurrencyStamp, where => where.Id == workflow.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != workflow.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    workflow.Updated = now;
                    workflow.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(workflow, columns);
                    args.Exclude<GXWorkflow>(q => new { q.CreationTime, q.WorkflowGroups, q.ScriptMethods, q.Creator });
                    _host.Connection.Update(args);
                    //Map workflow groups to workflow.
                    List<GXWorkflowGroup> workflowGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IWorkflowGroupRepository workflowGroupRepository = scope.ServiceProvider.GetRequiredService<IWorkflowGroupRepository>();
                        workflowGroups = await workflowGroupRepository.GetJoinedWorkflowGroups(User, workflow.Id);
                    }
                    var comparer = new UniqueComparer<GXWorkflowGroup, Guid>();
                    List<GXWorkflowGroup> removedWorkflowGroups = workflowGroups.Except(workflow.WorkflowGroups, comparer).ToList();
                    List<GXWorkflowGroup> addedWorkflowGroups = workflow.WorkflowGroups.Except(workflowGroups, comparer).ToList();
                    if (removedWorkflowGroups.Any())
                    {
                        RemoveWorkflowsFromWorkflowGroup(workflow.Id, removedWorkflowGroups);
                    }
                    if (addedWorkflowGroups.Any())
                    {
                        AddWorkflowToWorkflowGroups(workflow.Id, addedWorkflowGroups);
                    }
                    List<GXScriptMethod> methods = GetScriptMethods(workflow, false);
                    var comparer2 = new UniqueComparer<GXScriptMethod, Guid>();
                    List<GXScriptMethod> removedScripts = methods.Except(workflow.ScriptMethods, comparer2).ToList();
                    List<GXScriptMethod> addedScripts = workflow.ScriptMethods.Except(methods, comparer2).ToList();
                    if (removedScripts.Any())
                    {
                        RemoveScriptMethodsFromWorkflow(workflow, removedScripts);
                    }
                    if (addedScripts.Any())
                    {
                        AddScriptMethodsToWorkflow(workflow, addedScripts);
                    }
                }
                updates[workflow] = await GetUsersAsync(User, workflow.Id);
            }
            foreach (var it in updates)
            {
                GXWorkflow tmp = new GXWorkflow() { Id = it.Key.Id };
                await _eventsNotifier.WorkflowUpdate(it.Value, new GXWorkflow[] { tmp });
            }
            List<GXWorkflowLog> logs = new List<GXWorkflowLog>();
            foreach (var it in updates.Keys)
            {
                logs.Add(new GXWorkflowLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Workflow = it,
                    Message = it.CreationTime == now ?
                    Properties.Resources.WorkflowInstalled :
                    Properties.Resources.WorkflowUpdated
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var workfloLogRepository = scope.ServiceProvider.GetRequiredService<IWorkflowLogRepository>();
                await workfloLogRepository.AddAsync(User, logs);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map workflow group to user groups.
        /// </summary>
        /// <param name="workflowId">Workflow ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is added.</param>
        public void AddWorkflowToWorkflowGroups(Guid workflowId, IEnumerable<GXWorkflowGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXWorkflowGroupWorkflow> list = new();
            foreach (GXWorkflowGroup it in groups)
            {
                list.Add(new GXWorkflowGroupWorkflow()
                {
                    WorkflowId = workflowId,
                    WorkflowGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflow group and workflow.
        /// </summary>
        /// <param name="workflowId">Workflow ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is removed.</param>
        public void RemoveWorkflowsFromWorkflowGroup(Guid workflowId, IEnumerable<GXWorkflowGroup> groups)
        {
            foreach (var it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXWorkflowGroupWorkflow>(w => w.WorkflowId == workflowId && w.WorkflowGroupId == it.Id));
            }
        }

        /// <summary>
        /// Select all scripts that belong for this workflow.
        /// </summary>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="includeScripts">Are scripts included.</param>
        /// <returns>Collection of scripts that belong for this workflow.</returns>
        private List<GXScriptMethod> GetScriptMethods(GXWorkflow workflow, bool includeScripts)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXScriptMethod>();
            args.Columns.Add<GXScript>();
            args.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script, j => j.Id);
            if (!includeScripts)
            {
                args.Columns.Exclude<GXScript>(e => new { e.Methods, e.ByteAssembly });
            }
            else
            {
                args.Columns.Exclude<GXScript>(e => e.Methods);
            }
            args.Joins.AddInnerJoin<GXScriptMethod, GXWorkflowScriptMethod>(j => j.Id, j => j.MethodId);
            args.Where.And<GXWorkflowScriptMethod>(q => q.WorkflowId == workflow.Id);
            return _host.Connection.Select<GXScriptMethod>(args);
        }

        /// <summary>
        /// Add scripts to workflow.
        /// </summary>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="methods">Added script methods.</param>
        public void AddScriptMethodsToWorkflow(GXWorkflow workflow, IEnumerable<GXScriptMethod> methods)
        {
            DateTime now = DateTime.Now;
            List<GXWorkflowScriptMethod> list = new();
            foreach (GXScriptMethod it in methods)
            {
                list.Add(new GXWorkflowScriptMethod()
                {
                    MethodId = it.Id,
                    WorkflowId = workflow.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove scripts from the workflow.
        /// </summary>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="methods">Removed script methods.</param>
        public void RemoveScriptMethodsFromWorkflow(GXWorkflow workflow, IEnumerable<GXScriptMethod> methods)
        {
            foreach (GXScriptMethod it in methods)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXWorkflowScriptMethod>(w => w.MethodId == it.Id && w.WorkflowId == workflow.Id));
            }
        }

        /// <inheritdoc />
        public async Task RunAsync(
            ClaimsPrincipal User,
            Guid id)
        {
            GXWorkflow workflow = await ReadAsync(User, id, false);
            if (workflow == null ||
                workflow.TriggerActivity == null ||
                workflow.TriggerActivity.Trigger == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            IWorkflowHandler workflowHandler = _serviceProvider.GetRequiredService<IWorkflowHandler>();
            Type? type = Type.GetType(workflow.TriggerActivity.Trigger.ClassName);
            if (type == null)
            {
                throw new ArgumentException(String.Format("Unknown service type {0}.", workflow.TriggerActivity.Trigger.ClassName));
            }
            GXSelectArgs args = GXSelectArgs.SelectById<GXUser>(Internal.ServerHelpers.GetUserId(User));
            GXUser user = await _host.Connection.SingleOrDefaultAsync<GXUser>(args);
            workflowHandler.Execute(type, workflow.TriggerActivity.Name, user);
        }
    }
}
