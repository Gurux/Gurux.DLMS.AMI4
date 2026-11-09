// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IWorkflowGroupRepository _workflowGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IWorkflowHandler workflowHandler,
            IWorkflowGroupRepository workflowGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _workflowHandler = workflowHandler;
            _workflowGroupRepository = workflowGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXWorkflow), id, action, GXWorkflowPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? workflowId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByWorkflow(s => s.Id!,
                ServerHelpers.GetUserId(User), workflowId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (workflowId.HasValue && workflowId.Value != Guid.Empty)
                {
                await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), workflowId.Value, cancellationToken);
            }
            if (workflowId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXWorkflow), workflowId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByWorkflows(s => s.Id!,
                ServerHelpers.GetUserId(User), Ids);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (Ids != null)
                {
                foreach (var resourceId in Ids.Distinct())
                    {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), resourceId, cancellationToken);
                }
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> workflows, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default)
        {
            workflows = workflows.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXWorkflow, Guid>(_dbFactory, User, _serviceProvider, workflows,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.Delete);
            GXSelectArgs arg = GXSelectArgs.Select<GXWorkflow>(a => a.Id, q => workflows.Contains(q.Id));
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXWorkflow> list = connection.Select<GXWorkflow>(arg);
                foreach (var resource in list)
                    {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), resource.Id, cancellationToken);
                }
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflow, List<string>> updates = [];

            foreach (var workflow in list)
            {
                updates[workflow] = await GetNotifiedUsers(workflow.Id, NotificationAction.Remove, connection);
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (GXWorkflow workflow in list)
            {
                GXHistoryRuntimeEffects.EnqueueWorkflow(workflow, _workflowHandler, deleted: true);
                await GXLogStore.DeleteOwnedAsync(connection, transaction, "workflow", [workflow.Id], cancellationToken);
                workflow.Removed = now;
                if (delete)
                {
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXWorkflow>(workflow.Id), cancellationToken);
                }
                else
                {
                    connection.Update(transaction, GXUpdateArgs.Update(workflow, q => q.Removed!));
                }

            }
            await transaction.CommitAsync(cancellationToken);
            foreach (var it in updates)
            {
                var users = it.Value;
                if (users == null)
                {
                    break;
                }
                GXWorkflow tmp = new() { Id = it.Key.Id };
                _notificationCache.Add(users, new GXTarget(GXWorkflowPolicies.Delete, tmp.Id));
            }

        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXWorkflow>> ListAsync(
            ListWorkflows? request,
            ListWorkflowsResponse? response,
            bool includeActivity,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the workflows.
                arg = GXSelectArgs.SelectAll<GXWorkflow>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowsByUser(s => "*", userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXWorkflow>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXWorkflow>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXWorkflow>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await GXAutomationPolicyAccess.ApplyAsync(connection, _authorizationService, User, typeof(GXWorkflow), arg, cancellationToken);
            arg.Columns.Exclude<GXWorkflow>(w => w.Properties!);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXWorkflow>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXWorkflow>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXWorkflow>(q => q.CreationTime!);
            }
            if (includeActivity)
            {
                arg.Columns.Add<GXTrigger>();
                arg.Columns.Add<GXTriggerActivity>();
                arg.Columns.Exclude<GXTrigger>(e => e.Activities!);
                arg.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(x => x.TriggerActivity!, y => y.Id);
                arg.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(x => x.Trigger!, y => y.Id);
            }
            //Get creator information.
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Columns.Exclude<GXUser>(e => new { e.Workflows, e.WorkflowGroups });
            arg.Joins.AddInnerJoin<GXWorkflow, GXUser>(j => j.Creator!, j => j.Id!);

            GXWorkflow[] workflows = [.. (await connection.SelectAsync<GXWorkflow>(arg))];
            foreach (var resource in workflows)
                {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXWorkflow), resource.Id, cancellationToken);
            }
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
        public async Task<GXWorkflow> ReadAsync(Guid id, bool includeScripts, Expression<Func<GXWorkflow, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.View);
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the workflows.
                arg = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXWorkflow, GXWorkflowGroupWorkflow>(x => x.Id, y => y.WorkflowId);
                arg.Joins.AddLeftJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowsByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXWorkflow, GXWorkflowGroupWorkflow>(x => x.Id, y => y.WorkflowId);
                arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id);
            }
            arg.Columns.Add<GXWorkflowGroup>();
            arg.Columns.Exclude<GXWorkflowGroup>(e => e.Workflows!);
            arg.Distinct = true;
            arg.Joins.AddLeftJoin<GXWorkflow, GXWorkflowProperty>(w => w.Id, p => p.Workflow!);
            arg.Joins.AddLeftJoin<GXWorkflowProperty, Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule>(p => p.Module!, m => m.Id);
            arg.Columns.Add<GXWorkflowProperty>();
            arg.Columns.Exclude<GXWorkflowProperty>(p => p.Workflow!);
            arg.Columns.Add<Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule>(m => m.Id);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXWorkflow? workflow = await connection.SingleOrDefaultAsync<GXWorkflow>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
            workflow.Properties = workflow.Properties?.Where(p => p.Removed == null).ToList();
            await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), id, cancellationToken);
            workflow.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXWorkflow), id, cancellationToken);
            workflow.ScriptMethods = GetScriptMethods(connection, workflow, includeScripts);
            //Get trigger activity
            arg = GXSelectArgs.SelectAll<GXTriggerActivity>();
            arg.Columns.Add<GXTrigger>();
            arg.Columns.Exclude<GXTrigger>(e => e.Activities!);
            arg.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(x => x.TriggerActivity!, y => y.Id);
            arg.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(x => x.Trigger!, y => y.Id);
            arg.Where.And<GXWorkflow>(w => w.Id == workflow.Id);
            workflow.TriggerActivity = connection.SingleOrDefault<GXTriggerActivity>(arg);
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXWorkflow, GXUser>(s => s.Creator!, o => o.Id!);
            arg.Where.And<GXWorkflow>(w => w.Id == id);
            workflow.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
            //Get user.
            return workflow;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXWorkflow> workflows,
            Expression<Func<GXWorkflow, object>>? columns, CancellationToken cancellationToken = default)
        {
            workflows = workflows.ToArray();
            return await GXHistoryMutation.RunAsync<GXWorkflow, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, workflows,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.Add, GXWorkflowPolicies.Edit);
            DateTime now = DateTime.Now;
            List<Guid> list = [];
            GXUser user = new() { Id = ServerHelpers.GetUserId(User) };
            Dictionary<GXWorkflow, List<string>> updates = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (GXWorkflow workflow in workflows)
            {
                if (workflow.Id != Guid.Empty)
                    {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), workflow.Id, cancellationToken);
                }
                await _authorizationService.EnsureAnyPolicyAsync(User, workflow.Id == Guid.Empty ? GXWorkflowPolicies.Add : GXWorkflowPolicies.Edit);
                await GXResourcePolicyAccess.ValidateAsync(connection, workflow.Policies, cancellationToken);
                await GXAutomationPolicyAccess.EnsureWorkflowReferencesAsync(connection, _authorizationService, User, workflow, cancellationToken);
                workflow.Enabled ??= true;
                await GXWorkflowIdentityAccess.ValidateAsync(connection, User, workflow, cancellationToken);
                await GXWorkflowEventFilter.ValidateAsync(connection, _authorizationService, User, workflow, cancellationToken);
                GXWorkflowService.ValidateExecutionSettings(workflow);
                // The queued runner currently publishes After only. Never accept Before and silently run it after a change.
                if (workflow.Phase != TriggerPhase.After)
                    {
                    throw new ArgumentException("Only the After workflow phase is currently executable.", nameof(workflows));
                }
                if (workflow.TriggerActivity != null)
                {
                    var activityQuery = GXSelectArgs.SelectAll<GXTriggerActivity>(a => a.Id == workflow.TriggerActivity.Id);
                    activityQuery.Columns.Add<GXTrigger>();
                    activityQuery.Columns.Exclude<GXTrigger>(t => t.Activities!);
                    activityQuery.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
                    var activity = await connection.SingleOrDefaultAsync<GXTriggerActivity>(activityQuery, cancellationToken);
                    if (activity == null || !activity.SupportedPhases.HasFlag(workflow.Phase))
                        {
                        throw new ArgumentException("The trigger activity does not support the selected workflow phase.", nameof(workflows));
                    }
                    workflow.TriggerActivity = activity;
                }
                //Verify start date time.
                if (string.IsNullOrEmpty(workflow.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (workflow.WorkflowGroups == null || workflow.WorkflowGroups.Count == 0)
                {
                    workflow.WorkflowGroups = [];
                    ListWorkflowGroups request = new() { Filter = new GXWorkflowGroup() { Default = true } };
                    workflow.WorkflowGroups.AddRange(await _workflowGroupRepository.ListAsync(request, cancellationToken: cancellationToken));
                    if (workflow.WorkflowGroups.Count == 0)
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                if (workflow.Id == Guid.Empty)
                {
                    workflow.CreationTime = now;
                    workflow.Creator = user;
                    GXInsertArgs args = GXInsertArgs.Insert(workflow);
                    args.Exclude<GXWorkflow>(q => new
                    {
                        q.Updated,
                        q.Properties,
                        q.WorkflowGroups,
                        q.ScriptMethods
                    });
                    connection.Insert(transaction, args);
                    list.Add(workflow.Id);
                    AddWorkflowToWorkflowGroups(connection, workflow.Id, workflow.WorkflowGroups, transaction);
                    AddScriptMethodsToWorkflow(connection, workflow, workflow.ScriptMethods, transaction);

                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXWorkflow>(q => q.ConcurrencyStamp!, where => where.Id == workflow.Id);
                    string? updated = connection.SingleOrDefault<string>(transaction, m);
                    if (!string.IsNullOrEmpty(updated) && updated != workflow.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    workflow.Updated = now;
                    workflow.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(workflow, columns);
                    args.Exclude<GXWorkflow>(q => new
                    {
                        q.CreationTime,
                        q.Properties,
                        q.WorkflowGroups,
                        q.ScriptMethods,
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        workflow.Creator == null ||
                        string.IsNullOrEmpty(workflow.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXWorkflow>(q => q.Creator!);
                    }
                    connection.Update(transaction, args);
                    //Map workflow groups to workflow.
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
                    var joined = GXSelectArgs.SelectAll<GXWorkflowGroup>(g => g.Removed == null);
                    joined.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(g => g.Id, map => map.WorkflowGroupId);
                    joined.Where.And<GXWorkflowGroupWorkflow>(map => map.WorkflowId == workflow.Id && map.Removed == null);
                    var workflowGroups = await connection.SelectAsync<GXWorkflowGroup>(transaction, joined, cancellationToken);
                    var comparer = new UniqueComparer<GXWorkflowGroup, Guid>();
                    List<GXWorkflowGroup> removedWorkflowGroups = [.. workflowGroups.Except(workflow.WorkflowGroups, comparer)];
                    List<GXWorkflowGroup> addedWorkflowGroups = [.. workflow.WorkflowGroups.Except(workflowGroups, comparer)];
                    if (removedWorkflowGroups.Count != 0)
                    {
                        RemoveWorkflowsFromWorkflowGroup(connection, workflow.Id, removedWorkflowGroups, transaction);
                    }
                    if (addedWorkflowGroups.Count != 0)
                    {
                        AddWorkflowToWorkflowGroups(connection, workflow.Id, addedWorkflowGroups, transaction);
                    }
                    List<GXScriptMethod> methods = GetScriptMethods(connection, workflow, false, transaction);
                    var comparer2 = new UniqueComparer<GXScriptMethod, Guid>();
                    List<GXScriptMethod>? removedScripts = methods?.Except(workflow.ScriptMethods ?? [], comparer2).ToList();
                    List<GXScriptMethod>? addedScripts;
                    if (methods?.Count > 0)
                    {
                        addedScripts = workflow.ScriptMethods?.Except(methods, comparer2).ToList();
                    }
                    else
                    {
                        addedScripts = workflow.ScriptMethods;
                    }
                    if (removedScripts?.Count > 0)
                    {
                        RemoveScriptMethodsFromWorkflow(connection, workflow, removedScripts, transaction);
                    }
                    if (addedScripts?.Count > 0)
                    {
                        AddScriptMethodsToWorkflow(connection, workflow, addedScripts, transaction);
                    }
                }
                if (workflow.Properties != null)
                    {
                    await GXPropertyStores.Workflow.ReplaceAsync(connection, transaction, workflow,
                        workflow.Properties.Where(p => p.Removed == null), cancellationToken);
                }
                if (workflow.Policies != null)
                    {
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXWorkflow), workflow.Id, workflow.Policies, cancellationToken, transaction);
                }
                await transaction.CommitAsync(cancellationToken);
                GXHistoryRuntimeEffects.EnqueueWorkflow(workflow, _workflowHandler);
                updates[workflow] = await GetNotifiedUsers(workflow.Id, NotificationAction.Edit, connection);
            }
            foreach (var it in updates)
            {
                GXWorkflow tmp = new() { Id = it.Key.Id };
                _notificationCache.Add(it.Value, new GXTarget(GXWorkflowPolicies.Edit, tmp.Id));
            }
            List<GXLog> logs = [];
            foreach (var it in updates.Keys)
            {
                logs.Add(new GXLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Creator = it.Id,
                    Message = it.CreationTime == now ?
                    Properties.Resources.WorkflowInstalled :
                    Properties.Resources.WorkflowUpdated
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var workfloLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("workflow");
                await workfloLogRepository.AddAsync("Update", logs, cancellationToken);
            }
            return [.. list];
        }, cancellationToken);
        }

        /// <summary>
        /// Map workflow group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowId">Workflow ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is added.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void AddWorkflowToWorkflowGroups(GXDbConnection connection,
            Guid workflowId, IEnumerable<GXWorkflowGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            DateTime now = DateTime.Now;
            List<GXWorkflowGroupWorkflow> list = [];
            foreach (GXWorkflowGroup it in groups)
            {
                list.Add(new GXWorkflowGroupWorkflow()
                {
                    WorkflowId = workflowId,
                    WorkflowGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflow group and workflow.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowId">Workflow ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is removed.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void RemoveWorkflowsFromWorkflowGroup(GXDbConnection connection,
            Guid workflowId, IEnumerable<GXWorkflowGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            foreach (var it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXWorkflowGroupWorkflow>(w => w.WorkflowId == workflowId && w.WorkflowGroupId == it.Id));
            }
        }

        /// <summary>
        /// Select all scripts that belong for this workflow.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="includeScripts">Are scripts included.</param>
        /// <returns>Collection of scripts that belong for this workflow.</returns>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static List<GXScriptMethod> GetScriptMethods(GXDbConnection connection,
            GXWorkflow workflow, bool includeScripts, System.Data.IDbTransaction? transaction = null)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXScriptMethod>();
            args.Columns.Add<GXScript>();
            args.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script!, j => j.Id);
            if (!includeScripts)
            {
                args.Columns.Exclude<GXScript>(e => new { e.Methods, e.ByteAssembly });
            }
            else
            {
                args.Columns.Exclude<GXScript>(e => e.Methods!);
            }
            args.Joins.AddInnerJoin<GXScriptMethod, GXWorkflowScriptMethod>(j => j.Id, j => j.MethodId);
            args.Where.And<GXWorkflowScriptMethod>(q => q.WorkflowId == workflow.Id);
            return connection.Select<GXScriptMethod>(transaction, args);
        }

        /// <summary>
        /// Add scripts to workflow.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="methods">Added script methods.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void AddScriptMethodsToWorkflow(GXDbConnection connection,
            GXWorkflow workflow, IEnumerable<GXScriptMethod>? methods, System.Data.IDbTransaction? transaction = null)
        {
            if (methods?.Any() == true)
            {
                DateTime now = DateTime.Now;
                List<GXWorkflowScriptMethod> list = [];
                foreach (GXScriptMethod it in methods)
                {
                    list.Add(new GXWorkflowScriptMethod()
                    {
                        MethodId = it.Id,
                        WorkflowId = workflow.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove scripts from the workflow.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflow">Workflow where scripts belongs.</param>
        /// <param name="methods">Removed script methods.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void RemoveScriptMethodsFromWorkflow(GXDbConnection connection,
            GXWorkflow workflow, IEnumerable<GXScriptMethod> methods, System.Data.IDbTransaction? transaction = null)
        {
            foreach (GXScriptMethod it in methods)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXWorkflowScriptMethod>(w => w.MethodId == it.Id && w.WorkflowId == workflow.Id));
            }
        }

        /// <inheritdoc />
        public async Task RunAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowPolicies.Run);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXWorkflow), id, cancellationToken);
            _workflowHandler.ExecuteWorkflow(id, User);
        }
    }
}
