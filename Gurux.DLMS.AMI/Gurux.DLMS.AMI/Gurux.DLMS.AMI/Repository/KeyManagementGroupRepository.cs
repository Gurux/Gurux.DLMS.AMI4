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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class KeyManagementGroupRepository : IKeyManagementGroupRepository
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
        public KeyManagementGroupRepository(
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

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            arg.Where.And<GXKeyManagementGroup>(where => where.Removed == null && where.Id == groupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXKeyManagement>> GetJoinedKeyManagements(GXDbConnection connection, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagement>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXKeyManagement, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementId);
            arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            if (User != null && !User.IsInRole(GXRoles.Admin))
            {
                var id = ServerHelpers.GetUserId(User);
                arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            }
            arg.Where.And<GXKeyManagementGroup>(where => where.Removed == null && where.Id == groupId);
            return (await connection.SelectAsync<GXKeyManagement>(arg));
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXKeyManagementGroup), id, action, GXKeyManagementGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXKeyManagementGroup>> GetJoinedKeyManagementGroups(Guid keyId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(a => a.KeyManagementId, b => b.Id);

            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXKeyManagement>(where => where.Removed == null && where.Id == keyId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXKeyManagementGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByKeyManagementGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXKeyManagementGroup), groupId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByKeyManagementGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), groupIds);
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
        public async Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXKeyManagementGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXKeyManagementGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXKeyManagementGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXKeyManagementGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetKeyManagementGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                     DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXKeyManagementGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            DateTime now = DateTime.Now;
            Dictionary<GXKeyManagementGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXKeyManagementGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (delete)
                    {
                        await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXKeyManagementGroup>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
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
                _notificationCache.Add(it.Value,
                    new GXTarget(GXKeyManagementGroupPolicies.Delete, it.Key.Id, it.Key.Name));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXKeyManagementGroup>> ListAsync(
            ListKeyManagementGroups? request,
            ListKeyManagementGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the key management groups.
                arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>();
                arg.Joins.AddLeftJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.UserGroupId);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetKeyManagementGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If key management groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXKeyManagementGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXKeyManagementGroup>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXKeyManagementGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXKeyManagementGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXKeyManagementGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXKeyManagementGroup>(q => q.CreationTime!);
            }
            IEnumerable<GXKeyManagementGroup> groups = await connection.SelectAsync<GXKeyManagementGroup>(arg);
            if (response != null)
            {
                response.KeyManagementGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Count();
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXKeyManagementGroup> ReadAsync(Guid id, Expression<Func<GXKeyManagementGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetKeyManagementGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXKeyManagementGroup>(e => e.KeyManagements!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXKeyManagementGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get key managements that belongs for this key management group.
                arg = GXSelectArgs.Select<GXKeyManagement>(s => new { s.Id, s.SystemTitle }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(j => j.KeyManagementId, j => j.Id);
                arg.Where.And<GXKeyManagementGroupKeyManagement>(q => q.Removed == null && q.KeyManagementGroupId == id);
                group.KeyManagements = await connection.SelectAsync<GXKeyManagement>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this key management group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupKeyManagementGroup>(q => q.Removed == null && q.KeyManagementGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXKeyManagementGroup>(w => w.Id == id);
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
            GXKeyManagementGroup group, CancellationToken cancellationToken = default)
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
            foreach (var child in group.KeyManagements ?? [])
            {
                if (child.Id == Guid.Empty)
                {
                    child.Creator = group.Creator;
                    child.CreationTime = group.CreationTime;
                }
            }
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXKeyManagementGroup>(group => new { group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupKeyManagementGroup
            {
                UserGroupId = userGroup.Id,
                KeyManagementGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXKeyManagementGroup> KeyManagementGroups,
            Expression<Func<GXKeyManagementGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            KeyManagementGroups = KeyManagementGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXKeyManagementGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, KeyManagementGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementGroupPolicies.Add, GXKeyManagementGroupPolicies.Delete);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXKeyManagementGroup it in KeyManagementGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default user groups.
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroupsAsync(
                            creator.Id);
                        if (it.UserGroups?.Count is not > 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (it.KeyManagements != null)
                    {
                        //Update creator and creation time for all key managements.
                        foreach (var key in it.KeyManagements)
                        {
                            key.CreationTime = now;
                            key.Creator = creator;
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

                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                           new GXTarget(GXKeyManagementGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXKeyManagementGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXKeyManagementGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.KeyManagements
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXKeyManagementGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to KeyManagement group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddKeyManagementGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveKeyManagementGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map key managements to KeyManagement group.
                        if (it.KeyManagements != null)
                        {
                            List<GXKeyManagement> list3 = await GetJoinedKeyManagements(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.KeyManagements.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddKeyManagementsToKeyManagementGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveKeyManagementsFromKeyManagementGroup(connection, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                                               new GXTarget(GXKeyManagementGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                return [.. KeyManagementGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(KeyManagementGroups, columns, linkedCts.Token);
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
        /// Map key management group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is added.</param>
        private static void AddKeyManagementGroupToUserGroups(GXDbConnection connection,
            Guid groupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupKeyManagementGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupKeyManagementGroup()
                {
                    KeyManagementGroupId = groupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key management group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is removed.</param>
        private static void RemoveKeyManagementGroupFromUserGroups(GXDbConnection connection,
            Guid groupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupKeyManagementGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupKeyManagementGroup>(w => w.UserGroupId == ug &&
                    w.KeyManagementGroupId == groupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map key managements to key management group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is added.</param>
        private static void AddKeyManagementsToKeyManagementGroup(GXDbConnection connection,
            Guid groupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXKeyManagementGroupKeyManagement> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXKeyManagementGroupKeyManagement()
                {
                    KeyManagementGroupId = groupId,
                    KeyManagementId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key managements and key management group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is removed.</param>
        private static void RemoveKeyManagementsFromKeyManagementGroup(GXDbConnection connection,
            Guid groupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXKeyManagementGroupKeyManagement>();
            foreach (var it in groups)
            {
                args.Where.Or<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == it &&
                    w.KeyManagementGroupId == groupId);
            }
            connection.Delete(args);
        }
    }
}
