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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
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
    public class GatewayGroupRepository : IGatewayGroupRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayGroupRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
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
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Get all notified users that can access gateway group repository.
        /// </summary>
        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXGatewayGroup), id, action, GXGatewayGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        private static async Task<List<GXUserGroup>> GetUserGroupsByGatewayGroupId(GXDbConnection connection,
            IDbTransaction transaction,
            Guid gatewayGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            arg.Where.And<GXGatewayGroup>(where => where.Removed == null && where.Id == gatewayGroupId);
            return await connection.SelectAsync<GXUserGroup>(transaction, arg);
        }

        private static List<GXGateway> GetGatewaysByGatewayGroupId(GXDbConnection connection,
             IDbTransaction transaction, Guid gatewayGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGateway>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXGateway, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayId);
            arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            arg.Where.And<GXGatewayGroup>(where => where.Removed == null && where.Id == gatewayGroupId);
            return connection.Select<GXGateway>(transaction, arg);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByGatewayGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXGatewayGroup), groupId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByGatewayGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), Ids);
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
        public async Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXGatewayGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXGatewayGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXGatewayGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXGatewayGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetGatewayGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                     DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXGatewayGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXGatewayGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    if (!delete)
                    {
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, q => q.Removed!), cancellationToken);
                    }
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction),
                        new GXTarget(GXGatewayGroupPolicies.Delete, it.Id, it.Name));
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
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXGatewayGroup>> ListAsync(
            ListGatewayGroups? request,
            ListGatewayGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the gateway groups.
                arg = GXSelectArgs.SelectAll<GXGatewayGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewayGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If gateway groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupGatewayGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXGatewayGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXGatewayGroup>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXGatewayGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXGatewayGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXGatewayGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXGatewayGroup>(q => q.CreationTime!);
            }
            GXGatewayGroup[] groups = [.. (await connection.SelectAsync<GXGatewayGroup>(arg))];
            if (response != null)
            {
                response.GatewayGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXGatewayGroup> ReadAsync(Guid id, Expression<Func<GXGatewayGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXGatewayGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetGatewayGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXGatewayGroup>(e => e.Gateways!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = await connection.SingleOrDefaultAsync<GXGatewayGroup>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get gateways that belongs for this gateway group.
                arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(j => j.GatewayId, j => j.Id);
                arg.Where.And<GXGatewayGroupGateway>(q => q.Removed == null && q.GatewayGroupId == id);
                group.Gateways = await connection.SelectAsync<GXGateway>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this gateway group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupGatewayGroup>(q => q.Removed == null && q.GatewayGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);

                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXGatewayGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXGatewayGroup>(w => w.Id == id);
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
            GXGatewayGroup group, CancellationToken cancellationToken = default)
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
            args.Exclude<GXGatewayGroup>(group => new { group.Updated, group.Removed, group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupGatewayGroup
            {
                UserGroupId = userGroup.Id,
                GatewayGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXGatewayGroup> GatewayGroups,
            Expression<Func<GXGatewayGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            GatewayGroups = GatewayGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXGatewayGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, GatewayGroups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayGroupPolicies.Add, GXGatewayGroupPolicies.Edit);
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = GatewayGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = GatewayGroups.Where(w => w.Id != Guid.Empty).ToList();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXGatewayGroup it in GatewayGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default user groups.
                        defaultGroups ??= await _userGroupRepository.GetDefaultUserGroupsAsync(
                            ServerHelpers.GetUserId(User));
                        it.UserGroups = defaultGroups;
                        if (it.UserGroups?.Count is not > 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                }
                if (newGroups.Count != 0)
                {
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        await UpsertAsync(connection, transaction, it, cancellationToken);
                    }
                }
                foreach (var it in updatedGroups)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXGatewayGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                    string? updated = connection.SingleOrDefault<string>(transaction, m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXGatewayGroup>(q => new
                    {
                        q.CreationTime,
                        q.UserGroups,
                        q.Gateways
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXGatewayGroup>(q => q.Creator!);
                    }

                    connection.Update(transaction, args);
                    //Map user group to Gateway group.
                    {
                        List<GXUserGroup> userGroups = await GetUserGroupsByGatewayGroupId(connection, transaction, it.Id);
                        var comparer = new UniqueComparer<GXUserGroup, Guid>();
                        List<GXUserGroup> removedUserGroups = [.. userGroups.Except(it.UserGroups!, comparer)];
                        List<GXUserGroup> addedUserGroups = [.. it.UserGroups!.Except(userGroups, comparer)];
                        if (removedUserGroups.Count != 0)
                        {
                            RemoveGatewayGroupFromUserGroups(connection, transaction, it.Id, removedUserGroups);
                        }
                        if (addedUserGroups.Count != 0)
                        {
                            AddGatewayGroupToUserGroups(connection, transaction, it.Id, addedUserGroups);
                        }
                    }
                    //Map gateways to Gateway group.
                    if (it.Gateways != null)
                    {
                        List<GXGateway> gateways = GetGatewaysByGatewayGroupId(connection, transaction, it.Id);
                        var comparer = new UniqueComparer<GXGateway, Guid>();
                        List<GXGateway> removedGateways = [.. gateways.Except(it.Gateways, comparer)];
                        List<GXGateway> addedGateways = [.. it.Gateways.Except(gateways, comparer)];
                        if (removedGateways.Count != 0)
                        {
                            RemoveGatewaysFromGatewayGroup(connection, transaction, it.Id, removedGateways);
                        }
                        if (addedGateways.Count != 0)
                        {
                            AddGatewaysToGatewayGroup(connection, transaction, it.Id, addedGateways);
                        }
                    }
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var it in updatedGroups)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXGatewayGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                }
                foreach (var it in newGroups)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                        new GXTarget(GXGatewayGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                }
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
                        return await UpsertAsync(GatewayGroups, columns, linkedCts.Token);
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
            return [.. GatewayGroups.Select(s => s.Id)];
        }, cancellationToken);
        }

        /// <summary>
        /// Map gateway group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="groups">User groups where the gateway is added.</param>
        public void AddGatewayGroupToUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXUserGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupGatewayGroup> list = [];
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupGatewayGroup()
                {
                    GatewayGroupId = gatewayGroupId,
                    UserGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateway group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="groups">User groups where the gateway is removed.</param>
        public void RemoveGatewayGroupFromUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXUserGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupGatewayGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupGatewayGroup>(w => w.UserGroupId == it.Id &&
                    w.GatewayGroupId == gatewayGroupId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map gateways to gateway group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="gateways">Added Gateways.</param>
        public void AddGatewaysToGatewayGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXGateway> gateways)
        {
            DateTime now = DateTime.Now;
            List<GXGatewayGroupGateway> list = [];
            foreach (var it in gateways)
            {
                list.Add(new GXGatewayGroupGateway()
                {
                    GatewayGroupId = gatewayGroupId,
                    GatewayId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateways and gateway group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="gateways">Removed gateways.</param>
        public void RemoveGatewaysFromGatewayGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXGateway> gateways)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayGroupGateway>();
            foreach (var it in gateways)
            {
                args.Where.Or<GXGatewayGroupGateway>(w => w.GatewayId == it.Id &&
                    w.GatewayGroupId == gatewayGroupId);
            }
            connection.Delete(transaction, args);
        }
    }
}
