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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
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
    public class ModuleGroupRepository : IModuleGroupRepository
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
        public ModuleGroupRepository(
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

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXModuleGroup), id, action, GXModuleGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid moduleGroupId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            arg.Where.And<GXModuleGroup>(where => where.Removed == null && where.Id == moduleGroupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <inheritdoc />
        private static async Task<List<GXModule>> GetJoinedModules(GXDbConnection connection, Guid moduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>();
            arg.Joins.AddInnerJoin<GXModule, GXModuleGroupModule>(a => a.Id, b => b.ModuleId);
            arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            arg.Where.And<GXModuleGroup>(where => where.Removed == null && where.Id == moduleGroupId);
            return (await connection.SelectAsync<GXModule>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXModuleGroup>> GetJoinedModuleGroups(string moduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModuleGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(a => a.Id, b => b.ModuleGroupId);
            arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(a => a.ModuleId, b => b.Id);
            arg.Where.And<GXModule>(where => where.Enabled == true && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXModuleGroup>(arg));
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByModuleGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXModuleGroup), groupId, token: cancellationToken);
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

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByModuleGroups(s => s.Id!,
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

        /// <inheritdoc/>
        public async Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXModuleGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXModuleGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXModuleGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXModuleGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetModuleGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                     DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXModuleGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXModuleGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    _notificationCache.Add(users, new GXTarget(GXModuleGroupPolicies.Delete, it.Id));
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
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
        public async Task<IEnumerable<GXModuleGroup>> ListAsync(
            ListModuleGroups? request,
            ListModuleGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the module groups.
                arg = GXSelectArgs.SelectAll<GXModuleGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModuleGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If module groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupModuleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXModuleGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXModuleGroup>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXModuleGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXModuleGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXModuleGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXModuleGroup>(q => q.CreationTime!);
            }
            GXModuleGroup[] groups = [.. (await connection.SelectAsync<GXModuleGroup>(arg))];
            if (response != null)
            {
                response.ModuleGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXModuleGroup> ReadAsync(Guid id, Expression<Func<GXModuleGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXModuleGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetModuleGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXModuleGroup>(e => e.Modules!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXModuleGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get modules that belongs for this module group.
                arg = GXSelectArgs.Select<GXModule>(s => s.Id);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(j => j.ModuleId, j => j.Id);
                arg.Where.And<GXModuleGroupModule>(q => q.Removed == null && q.ModuleGroupId == id);
                group.Modules = await connection.SelectAsync<GXModule>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this module group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupModuleGroup>(q => q.Removed == null && q.ModuleGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXModuleGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXModuleGroup>(w => w.Id == id);
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
            GXModuleGroup group, CancellationToken cancellationToken = default)
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
            args.Exclude<GXModuleGroup>(group => new { group.UserGroups, group.Modules, group.Updated });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupModuleGroup
            {
                UserGroupId = userGroup.Id,
                ModuleGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
            if (group.Modules?.Count > 0)
            {
                var modules = group.Modules.Select(module => new GXModuleGroupModule
                {
                    ModuleGroupId = group.Id, ModuleId = module.Id, CreationTime = DateTime.Now
                }).ToArray();
                await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(modules), cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXModuleGroup> ModuleGroups,
            Expression<Func<GXModuleGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            ModuleGroups = ModuleGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXModuleGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ModuleGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXModuleGroupPolicies.Add, GXModuleGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                List<Guid> list = [];
                Dictionary<GXModuleGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXModuleGroup it in ModuleGroups)
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
                        list.Add(it.Id);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXModuleGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }

                        List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection);
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXModuleGroup>(q => new
                        {
                            q.Modules,
                            q.UserGroups,
                            q.CreationTime
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            string.IsNullOrEmpty(it.Creator?.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXModuleGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to Module group.
                        List<GXUserGroup> userGroups = await GetJoinedUserGroups(it.Id);
                        var comparer = new UniqueComparer<GXUserGroup, Guid>();
                        List<GXUserGroup> removed = [.. userGroups.Except(it.UserGroups, comparer)];
                        List<GXUserGroup> added = [.. it.UserGroups.Except(userGroups, comparer)];

                        if (added.Count != 0)
                        {
                            AddModuleGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Count != 0)
                        {
                            RemoveModuleGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map modules to Module group.
                        if (it.Modules != null)
                        {
                            List<GXModule> list3 = await GetJoinedModules(connection, it.Id);
                            List<string> groups2 = [.. list3.Select(s => s.Id)];
                            string[] tmp2 = [.. it.Modules.Select(s => s.Id)];
                            string[] removed2 = [.. groups2.Except(tmp2)];
                            string[] added2 = [.. tmp2.Except(groups2)];
                            if (added2.Length != 0)
                            {
                                AddModulesToModuleGroup(connection, it.Id, added2);
                            }
                            if (removed2.Length != 0)
                            {
                                RemoveModulesFromModuleGroup(connection, it.Id, removed2);
                            }
                        }
                        list.Add(it.Id);
                        updates[it] = users;
                    }
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXModuleGroupPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                return [.. list];

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
                        return await UpsertAsync(ModuleGroups, columns, linkedCts.Token);
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
        /// Map module group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">User groups where the module is added.</param>
        private static void AddModuleGroupToUserGroups(GXDbConnection connection,
            Guid moduleGroupId,
            IEnumerable<GXUserGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupModuleGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupModuleGroup()
                {
                    ModuleGroupId = moduleGroupId,
                    UserGroupId = ug.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between module group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        private static void RemoveModuleGroupFromUserGroups(GXDbConnection connection,
            Guid moduleGroupId,
            IEnumerable<GXUserGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupModuleGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupModuleGroup>(w => w.UserGroupId == ug.Id &&
                    w.ModuleGroupId == moduleGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map modules to module group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is added.</param>
        private static void AddModulesToModuleGroup(GXDbConnection connection,
            Guid moduleGroupId, IEnumerable<string> groups)
        {
            DateTime now = DateTime.Now;
            List<GXModuleGroupModule> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXModuleGroupModule()
                {
                    ModuleGroupId = moduleGroupId,
                    ModuleId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between modules and module group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        private static void RemoveModulesFromModuleGroup(GXDbConnection connection,
            Guid moduleGroupId, IEnumerable<string> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXModuleGroupModule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXModuleGroupModule>(w => w.ModuleId == it &&
                    w.ModuleGroupId == moduleGroupId);
            }
            connection.Delete(args);
        }
    }
}
