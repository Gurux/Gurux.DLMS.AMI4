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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class TriggerGroupRepository : ITriggerGroupRepository
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
        public TriggerGroupRepository(
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

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid triggerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            arg.Where.And<GXTriggerGroup>(where => where.Removed == null && where.Id == triggerGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private static async Task<List<GXTrigger>> GetJoinedTriggers(GXDbConnection connection, Guid triggerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTrigger>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXTrigger, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerId);
            arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            arg.Where.And<GXTriggerGroup>(where => where.Removed == null && where.Id == triggerGroupId);
            return (await connection.SelectAsync<GXTrigger>(arg));
        }


        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXTriggerGroup), id, action, GXTriggerGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXTriggerGroup>> GetJoinedTriggerGroups(Guid triggerId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTriggerGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerGroupId);
            arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(a => a.TriggerId, b => b.Id);
            arg.Where.And<GXTrigger>(where => where.Removed == null && where.Id == triggerId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXTriggerGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByTriggerGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXTriggerGroup), groupId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByTriggerGroups(s => s.Id!,
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
        public async Task DeleteAsync(IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXTriggerGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXTriggerGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXTriggerGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXTriggerGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetTriggerGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                   DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXTriggerGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }

            DateTime now = DateTime.Now;
            Dictionary<GXTriggerGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXTriggerGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
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
                GXTriggerGroup tmp = new() { Id = it.Key.Id };
                _notificationCache.Add(users, new GXTarget(GXTriggerGroupPolicies.Delete, tmp.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXTriggerGroup>> ListAsync(
            ListTriggerGroups? request,
            ListTriggerGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the trigger groups.
                arg = GXSelectArgs.SelectAll<GXTriggerGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetTriggerGroupsByUser(s => "*", userId, null, request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If trigger groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupTriggerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXTriggerGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXTriggerGroup>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXTriggerGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTriggerGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXTriggerGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXTriggerGroup>(q => q.CreationTime!);
            }
            GXTriggerGroup[] groups = [.. (await connection.SelectAsync<GXTriggerGroup>(arg))];
            if (response != null)
            {
                response.TriggerGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXTriggerGroup> ReadAsync(Guid id, Expression<Func<GXTriggerGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXTriggerGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetTriggerGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXTriggerGroup>(e => e.Triggers!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXTriggerGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get triggers that belongs for this trigger group.
                arg = GXSelectArgs.SelectAll<GXTrigger>(w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(j => j.TriggerId, j => j.Id);
                arg.Where.And<GXTriggerGroupTrigger>(q => q.Removed == null && q.TriggerGroupId == id);
                group.Triggers = await connection.SelectAsync<GXTrigger>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this trigger group.
                arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupTriggerGroup>(q => q.Removed == null && q.TriggerGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXTriggerGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXTriggerGroup>(w => w.Id == id);
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
            GXTriggerGroup group, CancellationToken cancellationToken = default)
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
            args.Exclude<GXTriggerGroup>(group => new { group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupTriggerGroup
            {
                UserGroupId = userGroup.Id,
                TriggerGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXTriggerGroup> TriggerGroups,
            Expression<Func<GXTriggerGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            TriggerGroups = TriggerGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXTriggerGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, TriggerGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.Add, GXTriggerGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXTriggerGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXTriggerGroup it in TriggerGroups)
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
                        GXSelectArgs m = GXSelectArgs.Select<GXTriggerGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXTriggerGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXTriggerGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to Trigger group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddTriggerGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveTriggerGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map triggers to Trigger group.
                        if (it.Triggers != null && it.Triggers.Count != 0)
                        {
                            List<GXTrigger> list3 = await GetJoinedTriggers(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Triggers.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddTriggersToTriggerGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveTriggersFromTriggerGroup(connection, it.Id, removed);
                            }
                        }
                    }
                    updates[it] = await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection);
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXTriggerGroupPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                return [.. TriggerGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(TriggerGroups, columns, linkedCts.Token);
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
        /// Map trigger group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        private static void AddTriggerGroupToUserGroups(GXDbConnection connection,
            Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupTriggerGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupTriggerGroup()
                {
                    TriggerGroupId = triggerGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between trigger group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        private static void RemoveTriggerGroupFromUserGroups(GXDbConnection connection,
            Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupTriggerGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupTriggerGroup>(w => w.UserGroupId == ug &&
                    w.TriggerGroupId == triggerGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map triggers to trigger group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        private static void AddTriggersToTriggerGroup(GXDbConnection connection,
            Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXTriggerGroupTrigger> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXTriggerGroupTrigger()
                {
                    TriggerGroupId = triggerGroupId,
                    TriggerId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between triggers and trigger group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        private static void RemoveTriggersFromTriggerGroup(GXDbConnection connection,
            Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXTriggerGroupTrigger>();
            foreach (var it in groups)
            {
                args.Where.Or<GXTriggerGroupTrigger>(w => w.TriggerId == it &&
                    w.TriggerGroupId == triggerGroupId);
            }
            connection.Delete(args);
        }
    }
}
