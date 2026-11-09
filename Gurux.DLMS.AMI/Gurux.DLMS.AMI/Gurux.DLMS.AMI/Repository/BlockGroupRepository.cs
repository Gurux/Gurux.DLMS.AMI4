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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
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
    public class BlockGroupRepository : IBlockGroupRepository
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
        public BlockGroupRepository(
            IDbConnectionFactory dbFactory,
            IAuthorizationService authorizationService,
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

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXBlockGroup), id, action, GXBlockGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXBlockGroup>> GetJoinedBlockGroups(Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXBlockGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXBlockGroup, GXBlockGroupBlock>(a => a.Id, b => b.BlockGroupId);
            arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(a => a.BlockId, b => b.Id);
            arg.Where.And<GXBlock>(where => where.Removed == null && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            var groups = await connection.SelectAsync<GXBlockGroup>(arg);
            foreach (var group in groups)
            {
                group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), group.Id);
            }

            return groups;
        }

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid blockGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            arg.Where.And<GXBlockGroup>(where => where.Removed == null && where.Id == blockGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXBlock>> GetJoinedBlocks(GXDbConnection connection, Guid blockGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXBlock>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXBlock, GXBlockGroupBlock>(a => a.Id, b => b.BlockId);
            arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXBlockGroup>(where => where.Removed == null && where.Id == blockGroupId);
            return (await connection.SelectAsync<GXBlock>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlockGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXBlockGroup), groupId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlockGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), agentIds);
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
        public async Task DeleteAsync(IEnumerable<Guid> blockGroups, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default)
        {
            blockGroups = blockGroups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXBlockGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, blockGroups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockGroupPolicies.Delete);
            blockGroups.ValidateIds();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXBlockGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXBlockGroup>(s => new { s.Id, s.Name }, w => blockGroups.Contains(w.Id));
                exists = connection.Select<GXBlockGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed block groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetBlockGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                   DateTimeOffset.MinValue, null, blockGroups);
                exists = connection.Select<GXBlockGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", blockGroups));
                }
            }
            DateTime now = DateTime.Now;
            Dictionary<GXBlockGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXBlockGroup it in exists)
                {
                    if (!blockGroups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (delete)
                    {
                        await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXBlockGroup>(it.Id), cancellationToken);
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
                        await DeleteAsync(blockGroups, delete, notify, linkedCts.Token);
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
                    new GXTarget(GXBlockGroupPolicies.Delete, it.Key.Id, it.Key.Name));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXBlockGroup>> ListAsync(
            ListBlockGroups? request,
            ListBlockGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXBlockGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetBlockGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If block groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupBlockGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user2);
                        }
                    }
                    request.Filter.UserGroups = null;
                }

                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXBlockGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXBlockGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXBlockGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXBlockGroup>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXBlockGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (!(request?.AllUsers == true && User.IsInRole(GXRoles.Admin)))
            {
                var candidates = await connection.SelectAsync<GXBlockGroup>(arg);
                var allowedIds = new List<Guid>();
                foreach (var candidate in candidates)
                {
                    candidate.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), candidate.Id, cancellationToken);
                    if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, candidate.Policies, cancellationToken))
                    {
                        allowedIds.Add(candidate.Id);
                    }
                }
                if (allowedIds.Count == 0)
                {
                    if (response != null)
                    {
                        response.Count = 0;
                        response.BlockGroups = [];
                    }
                    return [];
                }
                arg.Where.And<GXBlockGroup>(w => allowedIds.Contains(w.Id));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXBlockGroup>(q => GXSql.Count(1));
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
            GXBlockGroup[] groups = [.. (await connection.SelectAsync<GXBlockGroup>(arg))];
            foreach (var resource in groups)
            {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), resource.Id, cancellationToken);
            }

            if (response != null)
            {
                response.BlockGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXBlockGroup> ReadAsync(Guid id, Expression<Func<GXBlockGroup, object>>? columns,
            CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin) == true)
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXBlockGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetBlockGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Add<GXBlockGroup>();
                arg.Columns.Exclude<GXBlockGroup>(e => e.Blocks!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXBlockGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), group.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, group.Policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
                ////////////////////////////////////////////////////
                //Get blocks that belong for this block group.
                arg = GXSelectArgs.Select<GXBlock>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddLeftJoin<GXBlockGroupBlock, GXBlock>(j => j.BlockId, j => j.Id);
                arg.Where.And<GXBlockGroupBlock>(q => q.Removed == null && q.BlockGroupId == id);
                group.Blocks = await connection.SelectAsync<GXBlock>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belong for this block group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupBlockGroup>(q => q.Removed == null && q.BlockGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXBlockGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXBlockGroup>(w => w.Id == id);
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
            GXBlockGroup group, CancellationToken cancellationToken = default)
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
            args.Exclude<GXBlockGroup>(group => new { group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupBlockGroup
            {
                UserGroupId = userGroup.Id,
                BlockGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXBlockGroup), group.Id, group.Policies, cancellationToken, transaction);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXBlockGroup> BlockGroups,
            Expression<Func<GXBlockGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            BlockGroups = BlockGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXBlockGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, BlockGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockGroupPolicies.Add, GXBlockGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXBlockGroup it in BlockGroups)
                {
                    await GXResourcePolicyAccess.ValidateAsync(connection, it.Policies, cancellationToken);
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default user groups.
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroupsAsync(ServerHelpers.GetUserId(User));
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

                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                            new GXTarget(GXBlockGroupPolicies.Add, it.Id, it.Name));
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXBlockGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXBlockGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Blocks
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXBlockGroup>(q => q.Creator!);
                        }

                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Update(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXBlockGroup), it.Id, it.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        {
                            //Map user group to Block group.
                            List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                            List<Guid> groups = [.. list2.Select(s => s.Id)];
                            Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                            Guid[] removed = [.. groups.Except(tmp)];
                            Guid[] added = [.. tmp.Except(groups)];
                            if (added.Length != 0)
                            {
                                AddBlockGroupToUserGroups(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveBlockGroupFromUserGroups(connection, it.Id, removed);
                            }
                        }
                        //Map blocks to Block group.
                        //Only adming can add blocks that are visible to all users.
                        if (it.Blocks != null && (User.IsInRole(GXRoles.Admin) || it.Blocks.Count != 0))
                        {
                            var comparer = new UniqueComparer<GXBlock, Guid>();
                            List<GXBlock> blocks = await GetJoinedBlocks(connection, it.Id);
                            List<GXBlock> removed = [.. blocks.Except(it.Blocks!, comparer)];
                            List<GXBlock> added = [.. it.Blocks!.Except(blocks, comparer)];
                            AddBlocksToBlockGroup(connection, it.Id, added);
                            RemoveBlocksFromBlockGroup(connection, it.Id, removed);
                        }

                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXBlockGroupPolicies.Edit, it.Id, it.Name));
                    }
                }
                return [.. BlockGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(BlockGroups, columns, linkedCts.Token);
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
        /// Map block group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        private static void AddBlockGroupToUserGroups(GXDbConnection connection, Guid blockGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupBlockGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupBlockGroup()
                {
                    BlockGroupId = blockGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        private static void RemoveBlockGroupFromUserGroups(GXDbConnection connection, Guid blockGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupBlockGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupBlockGroup>(w => w.UserGroupId == ug &&
                    w.BlockGroupId == blockGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="blocks">Group IDs of the block groups where the block is added.</param>
        private static void AddBlocksToBlockGroup(GXDbConnection connection, Guid blockGroupId,
            IEnumerable<GXBlock> blocks)
        {
            DateTime now = DateTime.Now;
            List<GXBlockGroupBlock> list = [];
            foreach (var block in blocks)
            {
                list.Add(new GXBlockGroupBlock()
                {
                    BlockGroupId = blockGroupId,
                    BlockId = block.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="blocks">Blocks of the block group where the block is removed.</param>
        private static void RemoveBlocksFromBlockGroup(GXDbConnection connection, Guid blockGroupId, IEnumerable<GXBlock> blocks)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXBlockGroupBlock>();
            foreach (var it in blocks)
            {
                args.Where.Or<GXBlockGroupBlock>(w => w.BlockId == it.Id &&
                    w.BlockGroupId == blockGroupId);
            }
            connection.Delete(args);
        }

    }
}
