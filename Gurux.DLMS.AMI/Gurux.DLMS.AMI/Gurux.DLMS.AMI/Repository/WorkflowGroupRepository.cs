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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class WorkflowGroupRepository : IWorkflowGroupRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowGroupRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IUserGroupRepository userGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _userGroupRepository = userGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid workflowGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            arg.Where.And<GXWorkflowGroup>(where => where.Removed == null && where.Id == workflowGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private static async Task<List<GXWorkflow>> GetJoinedWorkflows(GXDbConnection connection, Guid workflowGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflow>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXWorkflow, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowId);
            arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            arg.Where.And<GXWorkflowGroup>(where => where.Removed == null && where.Id == workflowGroupId);
            return (await connection.SelectAsync<GXWorkflow>(arg));
        }


        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXWorkflowGroup), id, action, GXWorkflowGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXWorkflowGroup>> GetJoinedWorkflowGroups(Guid workflowId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflowGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowGroupId);
            arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(a => a.WorkflowId, b => b.Id);
            arg.Where.And<GXWorkflow>(where => where.Removed == null && where.Id == workflowId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXWorkflowGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByWorkflowGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXWorkflowGroup), groupId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByWorkflowGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), groupIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXWorkflowGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXWorkflowGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXWorkflowGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXWorkflowGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetWorkflowGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXWorkflowGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflowGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXWorkflowGroup it in exists)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(groups, delete, notify, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            foreach (var it in updates)
            {
                var users = it.Value;
                if (users == null)
                {
                    break;
                }
                _notificationCache.Add(users, new GXTarget(GXWorkflowGroupPolicies.Delete, it.Key.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXWorkflowGroup>> ListAsync(
            ListWorkflowGroups? request,
            ListWorkflowGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the workflow groups.
                arg = GXSelectArgs.SelectAll<GXWorkflowGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowGroupsByUser(s => "*", userId, null, request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If workflow groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupWorkflowGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXWorkflowGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXWorkflowGroup>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXWorkflowGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXWorkflowGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXWorkflowGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXWorkflowGroup>(q => q.CreationTime!);
            }
            GXWorkflowGroup[] groups = [.. (await connection.SelectAsync<GXWorkflowGroup>(arg))];
            if (response != null)
            {
                response.WorkflowGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXWorkflowGroup> ReadAsync(Guid id, Expression<Func<GXWorkflowGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXWorkflowGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetWorkflowGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXWorkflowGroup>(e => e.Workflows!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXWorkflowGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get workflows that belongs for this workflow group.
                arg = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(j => j.WorkflowId, j => j.Id);
                arg.Where.And<GXWorkflowGroupWorkflow>(q => q.Removed == null && q.WorkflowGroupId == id);
                group.Workflows = await connection.SelectAsync<GXWorkflow>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this workflow group.
                arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupWorkflowGroup>(q => q.Removed == null && q.WorkflowGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXWorkflowGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXWorkflowGroup>(w => w.Id == id);
                group.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return group;
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await ReadAsync(id, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>Inserts a new group and its memberships in the caller's transaction without notifications or history.</summary>
        internal static async Task UpsertAsync(GXDbConnection connection, System.Data.IDbTransaction transaction,
            GXWorkflowGroup group, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Id != Guid.Empty)
                {
                throw new ArgumentException("This overload only inserts new groups.", nameof(group));
            }
            if (string.IsNullOrEmpty(group.Name))
                {
                throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.InvalidName);
            }
            if (string.IsNullOrEmpty(group.Creator?.Id))
                {
                throw new ArgumentException("A group creator is required.", nameof(group));
            }
            group.CreationTime ??= DateTime.Now;
            if (group.UserGroups?.Count is not > 0 || group.UserGroups.Any(g => g.Id == Guid.Empty))
                {
                throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.TargetMustBelongToOneGroup);
            }
            foreach (var child in group.Workflows ?? [])
            {
                if (child.Id == Guid.Empty)
                {
                    child.Creator = group.Creator;
                    child.CreationTime = group.CreationTime;
                }
            }
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXWorkflowGroup>(group => new { group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupWorkflowGroup
            {
                UserGroupId = userGroup.Id,
                WorkflowGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXWorkflowGroup> WorkflowGroups,
            Expression<Func<GXWorkflowGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            WorkflowGroups = WorkflowGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXWorkflowGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, WorkflowGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXWorkflowGroupPolicies.Add, GXWorkflowGroupPolicies.Edit);
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                DateTime now = DateTime.Now;
                Dictionary<GXWorkflowGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXWorkflowGroup it in WorkflowGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default user groups.
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroupsAsync(
                            ServerHelpers.GetUserId(User));
                        if (it.UserGroups?.Count is not > 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (it.Workflows != null)
                    {
                        //Update creator and creation time for all workflows.
                        foreach (var workflow in it.Workflows)
                        {
                            workflow.CreationTime = now;
                            workflow.Creator = creator;
                        }
                    }

                    if (it.Id == Guid.Empty)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        using (var insertTransaction = connection.BeginTransaction())
                        {
                            await UpsertAsync(connection, insertTransaction, it, cancellationToken);
                            insertTransaction.Commit();
                        }
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXWorkflowGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXWorkflowGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXWorkflowGroup>(q => q.Creator!);
                        }

                        connection.Update(args);
                        //Map user group to Workflow group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddWorkflowGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveWorkflowGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map workflows to Workflow group.
                        if (it.Workflows != null && it.Workflows.Count != 0)
                        {
                            List<GXWorkflow> list3 = await GetJoinedWorkflows(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Workflows.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddWorkflowsToWorkflowGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveWorkflowsFromWorkflowGroup(connection, it.Id, removed);
                            }
                        }
                    }
                    updates[it] = await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection);
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(it.Value, new GXTarget(GXWorkflowGroupPolicies.Edit, it.Key.Id));
                }
                return [.. WorkflowGroups.Select(s => s.Id)];

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(WorkflowGroups, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Map workflow group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is added.</param>
        private static void AddWorkflowGroupToUserGroups(GXDbConnection connection,
            Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupWorkflowGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupWorkflowGroup()
                {
                    WorkflowGroupId = workflowGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflow group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is removed.</param>
        private static void RemoveWorkflowGroupFromUserGroups(GXDbConnection connection, Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupWorkflowGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupWorkflowGroup>(w => w.UserGroupId == ug &&
                    w.WorkflowGroupId == workflowGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map workflows to workflow group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is added.</param>
        private static void AddWorkflowsToWorkflowGroup(GXDbConnection connection,
            Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXWorkflowGroupWorkflow> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXWorkflowGroupWorkflow()
                {
                    WorkflowGroupId = workflowGroupId,
                    WorkflowId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflows and workflow group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="groups">Group IDs of the workflow groups where the workflow is removed.</param>
        private static void RemoveWorkflowsFromWorkflowGroup(GXDbConnection connection,
            Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXWorkflowGroupWorkflow>();
            foreach (var it in groups)
            {
                args.Where.Or<GXWorkflowGroupWorkflow>(w => w.WorkflowId == it &&
                    w.WorkflowGroupId == workflowGroupId);
            }
            connection.Delete(args);
        }
    }
}
