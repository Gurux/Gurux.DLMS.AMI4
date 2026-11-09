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
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
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
    public class MenuGroupRepository : IMenuGroupRepository
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
        public MenuGroupRepository(
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

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXMenuGroup), id, action, GXMenuGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXMenuGroup>> GetJoinedMenuGroups(Guid moduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXMenuGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXMenuGroup, GXMenuGroupMenu>(a => a.Id, b => b.MenuGroupId);
            arg.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenu>(a => a.MenuId, b => b.Id);
            arg.Where.And<GXMenu>(where => where.Removed == null && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXMenuGroup>(arg));
        }


        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid MenuGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupMenuGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXMenuGroup>(a => a.MenuGroupId, b => b.Id);
            arg.Where.And<GXMenuGroup>(where => where.Removed == null && where.Id == MenuGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXMenu>> GetJoinedMenus(GXDbConnection connection, Guid MenuGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXMenu>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXMenu, GXMenuGroupMenu>(a => a.Id, b => b.MenuId);
            arg.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenuGroup>(a => a.MenuGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXMenuGroup>(where => where.Removed == null && where.Id == MenuGroupId);
            return (await connection.SelectAsync<GXMenu>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByMenuGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXMenuGroup), groupId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByMenuGroups(s => s.Id!,
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
        public async Task DeleteAsync(IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXMenuGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXMenuGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXMenuGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXMenuGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetMenuGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                   DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXMenuGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            Dictionary<GXMenuGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXMenuGroup it in exists)
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
                    var ids = exists.Select(g => g.Id).ToArray();
                    await connection.DeleteAsync(transaction,
                        GXDeleteArgs.Delete<GXMenuGroupMenu>(m => ids.Contains(m.MenuGroupId)), cancellationToken);
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
                GXMenuGroup tmp = new() { Id = it.Key.Id, Name = it.Key.Name };
                _notificationCache.Add(users, new GXTarget(GXMenuGroupPolicies.Delete, tmp.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXMenuGroup>> ListAsync(
            ListMenuGroups? request,
            ListMenuGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXMenuGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetMenuGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If menu groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupMenuGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXMenuGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXMenuGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXMenuGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXMenuGroup>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXMenuGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXMenuGroup>(q => GXSql.Count(1));
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
            GXMenuGroup[] groups = [.. (await connection.SelectAsync<GXMenuGroup>(arg))];
            if (response != null)
            {
                response.MenuGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXMenuGroup> ReadAsync(Guid id, Expression<Func<GXMenuGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXMenuGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetMenuGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Add<GXMenuGroup>();
                arg.Columns.Exclude<GXMenuGroup>(e => e.Menus!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = await connection.SingleOrDefaultAsync<GXMenuGroup>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get blocks that belong for this block group.
                arg = GXSelectArgs.Select<GXMenu>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddLeftJoin<GXMenuGroupMenu, GXMenu>(j => j.MenuId, j => j.Id);
                arg.Where.And<GXMenuGroupMenu>(q => q.Removed == null && q.MenuGroupId == id);
                group.Menus = await connection.SelectAsync<GXMenu>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belong for this block group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupMenuGroup>(q => q.Removed == null && q.MenuGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXMenuGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXMenuGroup>(w => w.Id == id);
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

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXMenuGroup> MenuGroups,
            Expression<Func<GXMenuGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            MenuGroups = MenuGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXMenuGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, MenuGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuGroupPolicies.Add, GXMenuGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXMenuGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXMenuGroup it in MenuGroups)
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
                        GXInsertArgs args = GXInsertArgs.Insert(it);
                        //User groups must hanlde separetly because users are identified with name and not Guid.
                        args.Exclude<GXMenuGroup>(e => new { e.UserGroups, e.Menus });
                        connection.Insert(args);
                        AddMenuGroupToUserGroups(connection, it.Id, [.. it.UserGroups.Select(s => s.Id)]);
                        if (it.Menus?.Count > 0)
                        {
                            AddMenusToMenuGroup(connection, it.Id, it.Menus.Select(menu => menu.Id).Distinct());
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                            new GXTarget(GXMenuGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXMenuGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXMenuGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Menus
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXMenuGroup>(q => q.Creator!);
                        }

                        connection.Update(args);
                        //Map user group to Menu group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddMenuGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveMenuGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map blocks to Menu group.
                        //Only adming can add blocks that are visible to all users.
                        if (it.Menus != null && (User.IsInRole(GXRoles.Admin) || it.Menus.Count != 0))
                        {
                            List<GXMenu> list3 = await GetJoinedMenus(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Menus.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddMenusToMenuGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveMenusFromMenuGroup(connection, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXMenuGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                return [.. MenuGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(MenuGroups, columns, linkedCts.Token);
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
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        private static void AddMenuGroupToUserGroups(GXDbConnection connection,
            Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupMenuGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupMenuGroup()
                {
                    MenuGroupId = MenuGroupId,
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
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        private static void RemoveMenuGroupFromUserGroups(GXDbConnection connection,
            Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupMenuGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupMenuGroup>(w => w.UserGroupId == ug &&
                    w.MenuGroupId == MenuGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        private static void AddMenusToMenuGroup(GXDbConnection connection,
            Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXMenuGroupMenu> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXMenuGroupMenu()
                {
                    MenuGroupId = MenuGroupId,
                    MenuId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        private static void RemoveMenusFromMenuGroup(GXDbConnection connection,
            Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXMenuGroupMenu>();
            foreach (var it in groups)
            {
                args.Where.Or<GXMenuGroupMenu>(w => w.MenuId == it &&
                    w.MenuGroupId == MenuGroupId);
            }
            connection.Delete(args);
        }
    }
}
