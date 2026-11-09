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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
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
    class AgentGroupRepository : IAgentGroupRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgentGroupRepository(
            IDbConnectionFactory dbFactory,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private static async Task<List<GXUserGroup>> GetUserGroupsByAgentGroupId(GXDbConnection connection,
            IDbTransaction transaction, Guid agentGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            arg.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == agentGroupId);
            return (await connection.SelectAsync<GXUserGroup>(transaction, arg));
        }

        private static List<GXAgent> GetAgentsByAgentGroupId(GXDbConnection connection,
            IDbTransaction transaction,
            Guid agentGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAgent, GXAgentGroupAgent>(a => a.Id, b => b.AgentId);
            arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            arg.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == agentGroupId);
            return connection.Select<GXAgent>(transaction, arg);
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAgentGroup), id, action, GXAgentGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAgentGroup(s => "*", ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXAgentGroup), groupId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAgentGroups(s => "*", ServerHelpers.GetUserId(User), agentIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> groups, bool delete, bool notify, CancellationToken cancellationToken)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXAgentGroup, Guid>(_dbFactory, User, _serviceProvider, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.Delete);
            groups.ValidateIds();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                IEnumerable<GXAgentGroup> exists;
                if (User.IsInRole(GXRoles.Admin))
                {
                    GXSelectArgs arg = GXSelectArgs.Select<GXAgentGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                    exists = connection.Select<GXAgentGroup>(transaction, arg, cancellationToken);
                }
                else
                {
                    //Check that the user has access rights to the removed agent groups.
                    string userId = ServerHelpers.GetUserId(User);
                    var arg = GXQuery.GetAgentGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                       DateTimeOffset.MinValue, null, groups);
                    exists = connection.Select<GXAgentGroup>(transaction, arg, cancellationToken);
                }
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
                DateTime now = DateTime.Now;
                foreach (GXAgentGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    if (notify)
                    {
                        List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                        _notificationCache.Add(users, new GXTarget(GXAgentGroupPolicies.Delete, it.Id, it.Name));
                    }
                    if (!delete)
                    {
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, q => q.Removed!), cancellationToken);
                    }
                }
                if (delete)
                {
                    //Remove agent group and all the maps to agents, user groups and device groups.
                    GXDeleteArgs args = GXDeleteArgs.Delete<GXAgentGroupDeviceGroup>(w => groups.Contains(w.AgentGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    args = GXDeleteArgs.Delete<GXAgentGroupAgent>(w => groups.Contains(w.AgentGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (GXDatabaseException ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);                        // If deadlock, wait max 1 seconds and retry.
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
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXAgentGroup>> ListAsync(ListAgentGroups? request,
            ListAgentGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agent groups.
                arg = GXSelectArgs.SelectAll<GXAgentGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            GXDateTimeFilters.Apply<GXAgentGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (request != null)
                {
                    //If agent groups are filtered by User.
                    if (request.Filter?.UserGroups != null)
                    {
                        var ug = request.Filter.UserGroups.FirstOrDefault();
                        if (ug?.Users != null && ug.Users.Count != 0)
                        {
                            var User = ug.Users.FirstOrDefault();
                            if (User != null)
                            {
                                arg.Joins.AddLeftJoin<GXUserGroupAgentGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                                arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                                arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                                arg.Where.FilterBy(User);
                            }
                        }
                        request.Filter.UserGroups = null;
                    }
                    arg.Where.FilterBy(request.Filter);
                }
                if (request != null && request.Count != 0)
                {
                    //Return total row count. This can be used for paging.
                    GXSelectArgs total = GXSelectArgs.Select<GXAgentGroup>(q => GXSql.Count(1));
                    total.Joins.Append(arg.Joins);
                    total.Where.Append(arg.Where);
                    if (response != null)
                    {
                        response.Count = connection.SingleOrDefault<long>(transaction, total);
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
                    arg.OrderBy.Add<GXAgentGroup>(request.OrderBy);
                }
                else
                {
                    arg.Descending = true;
                    arg.OrderBy.Add<GXAgentGroup>(q => q.CreationTime!);
                }
                var groups = await connection.SelectAsync<GXAgentGroup>(transaction, arg);
                if (response != null)
                {
                    response.AgentGroups = groups;
                    if (response.Count == 0)
                    {
                        response.Count = groups.Count;
                    }
                }
                return groups;
            }
            catch (GXDatabaseException ex)
            {
                transaction.Rollback();
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);                        // If deadlock, wait max 1 seconds and retry.
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await ListAsync(request, response, linkedCts.Token);
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
        }

        /// <inheritdoc />
        public async Task<GXAgentGroup> ReadAsync(Guid id, Expression<Func<GXAgentGroup, object>>? columns, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.View);
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXAgentGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentGroupsByUser(s => "*", userId, id);
            }
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXAgentGroup, GXUser>(j => j.Creator!, j => j.Id!);
            arg.Columns.Exclude<GXAgentGroup>(e => e.Agents!);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                var group = await connection.SingleOrDefaultAsync<GXAgentGroup>(transaction, arg) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get agents that belongs for this agent group.
                arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
                arg.Where.And<GXAgentGroupAgent>(q => q.Removed == null && q.AgentGroupId == id);
                group.Agents = await connection.SelectAsync<GXAgent>(transaction, arg);

                ////////////////////////////////////////////////////
                //Get User groups that belongs for this agent group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupAgentGroup>(q => q.Removed == null && q.AgentGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(transaction, arg);
                ////////////////////////////////////////////////////
                //Get device groups.
                arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                arg.Where.And<GXAgentGroupDeviceGroup>(q => q.Removed == null && q.AgentGroupId == id);
                group.DeviceGroups = await connection.SelectAsync<GXDeviceGroup>(transaction, arg);

                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAgentGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXAgentGroup>(w => w.Id == id);
                group.Creator = await connection.SingleOrDefaultAsync<GXUser>(transaction, arg);
                return group;
            }
            catch (GXDatabaseException ex)
            {
                transaction.Rollback();
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);                        // If deadlock, wait max 1 seconds and retry.
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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>Inserts a new group and its memberships in the caller's transaction without notifications or history.</summary>
        internal static async Task UpsertAsync(GXDbConnection connection, System.Data.IDbTransaction transaction,
            GXAgentGroup group, CancellationToken cancellationToken = default)
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
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXAgentGroup>(group => new { group.Updated, group.Removed, group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupAgentGroup
            {
                UserGroupId = userGroup.Id,
                AgentGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXAgentGroup> AgentGroups,
            Expression<Func<GXAgentGroup, object>>? columns, CancellationToken cancellationToken)
        {
            AgentGroups = AgentGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXAgentGroup, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, AgentGroups,
        async () =>
        {
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = AgentGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = AgentGroups.Where(w => w.Id != Guid.Empty).ToList();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXAgentGroup it in AgentGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default User groups.
                        defaultGroups ??= await _userGroupRepository.GetDefaultUserGroupsAsync(ServerHelpers.GetUserId(User));
                        it.UserGroups = defaultGroups;
                        if (it.UserGroups?.Count is not > 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                }
                if (newGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.Add);
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        await UpsertAsync(connection, transaction, it, cancellationToken);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction), new GXTarget(GXAgentGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                if (updatedGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentGroupPolicies.Edit);
                    foreach (var it in updatedGroups)
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXAgentGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXAgentGroup>(q => new
                        {
                            q.CreationTime,
                            q.UserGroups,
                            q.Agents,
                            q.DeviceGroups
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXAgentGroup>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);
                        //Map User group to Agent group.
                        {
                            List<GXUserGroup> userGroups = await GetUserGroupsByAgentGroupId(connection, transaction, it.Id);
                            var comparer = new UniqueComparer<GXUserGroup, Guid>();
                            List<GXUserGroup> removedUserGroups = [.. userGroups.Except(it.UserGroups!, comparer)];
                            List<GXUserGroup> addedUserGroups = [.. it.UserGroups!.Except(userGroups, comparer)];
                            if (removedUserGroups.Count != 0)
                            {
                                RemoveAgentGroupFromUserGroups(connection, transaction, it.Id, removedUserGroups);
                            }
                            if (addedUserGroups.Count != 0)
                            {
                                AddAgentGroupToUserGroups(connection, transaction, it.Id, addedUserGroups);
                            }
                        }
                        //Map agents to Agent group.
                        if (it.Agents != null)
                        {
                            List<GXAgent> agents = GetAgentsByAgentGroupId(connection, transaction, it.Id);
                            var comparer = new UniqueComparer<GXAgent, Guid>();
                            List<GXAgent> removedAgents = [.. agents.Except(it.Agents, comparer)];
                            List<GXAgent> addedAgents = [.. it.Agents.Except(agents, comparer)];
                            if (removedAgents.Count != 0)
                            {
                                RemoveAgentsFromAgentGroup(connection, transaction, it.Id, removedAgents);
                            }
                            if (addedAgents.Count != 0)
                            {
                                AddAgentsToAgentGroup(connection, transaction, it.Id, addedAgents);
                            }
                        }
                        //Map device groups to agent group.
                        if (it.DeviceGroups != null)
                        {
                            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceGroupPolicies.View);
                            // Read through the update transaction: another connection would wait on this group's write lock.
                            GXSelectArgs deviceGroupArgs = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
                            deviceGroupArgs.Joins.AddInnerJoin<GXDeviceGroup, GXAgentGroupDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
                            deviceGroupArgs.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
                            deviceGroupArgs.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == it.Id);
                            List<GXDeviceGroup> deviceGroups = await connection.SelectAsync<GXDeviceGroup>(transaction, deviceGroupArgs, cancellationToken);
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedDeviceGroups = [.. deviceGroups.Except(it.DeviceGroups, comparer)];
                            List<GXDeviceGroup> addedDeviceGroups = [.. it.DeviceGroups.Except(deviceGroups, comparer)];
                            if (removedDeviceGroups.Count != 0)
                            {
                                RemoveDeviceGroupsFromAgentGroup(connection, transaction, it.Id, removedDeviceGroups);
                            }
                            if (addedDeviceGroups.Count != 0)
                            {
                                AddDeviceGroupsToAgentGroup(connection, transaction, it.Id, addedDeviceGroups);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection, transaction),
                            new GXTarget(GXAgentGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                await transaction.CommitAsync(cancellationToken);
                return [.. AgentGroups.Select(s => s.Id)];
            }
            catch (GXDatabaseException ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);                        // If deadlock, wait max 1 seconds and retry.
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(AgentGroups, columns, linkedCts.Token);
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
        }, cancellationToken);
        }

        /// <summary>
        /// Map agent group to User groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">User groups where the agent is added.</param>
        private static void AddAgentGroupToUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXUserGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupAgentGroup> list = [];
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupAgentGroup()
                {
                    AgentGroupId = agentGroupId,
                    UserGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agent group and User groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">User groups where the agent is removed.</param>
        private static void RemoveAgentGroupFromUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXUserGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupAgentGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupAgentGroup>(w => w.UserGroupId == it.Id &&
                    w.AgentGroupId == agentGroupId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map agents to agent group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Added Agents.</param>
        private static void AddAgentsToAgentGroup(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXAgent> agents)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupAgent> list = [];
            foreach (var it in agents)
            {
                list.Add(new GXAgentGroupAgent()
                {
                    AgentGroupId = agentGroupId,
                    AgentId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agents and agent group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Removed agents.</param>
        private static void RemoveAgentsFromAgentGroup(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXAgent> agents)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupAgent>();
            foreach (var it in agents)
            {
                args.Where.Or<GXAgentGroupAgent>(w => w.AgentId == it.Id && w.AgentGroupId == agentGroupId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map agents to agent group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Device groups where the agent is added.</param>
        private static void AddDeviceGroupsToAgentGroup(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXDeviceGroup> agents)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupDeviceGroup> list = [];
            foreach (var it in agents)
            {
                list.Add(new GXAgentGroupDeviceGroup()
                {
                    AgentGroupId = agentGroupId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agents and agent group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">Device groups where the agent is removed.</param>
        private static void RemoveDeviceGroupsFromAgentGroup(GXDbConnection connection, IDbTransaction transaction, Guid agentGroupId, IEnumerable<GXDeviceGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupDeviceGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXAgentGroupDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                    w.AgentGroupId == agentGroupId);
            }
            connection.Delete(transaction, args);
        }
    }
}
