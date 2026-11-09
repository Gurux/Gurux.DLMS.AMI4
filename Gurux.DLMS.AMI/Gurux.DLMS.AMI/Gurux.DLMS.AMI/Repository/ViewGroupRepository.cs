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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.View;
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
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ViewGroupRepository : IViewGroupRepository
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
        public ViewGroupRepository(
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
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXViewGroup), id, action, GXViewGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXViewGroup>> GetJoinedViewGroups(Guid moduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXViewGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXViewGroup, GXViewGroupView>(a => a.Id, b => b.ViewGroupId);
            arg.Joins.AddInnerJoin<GXViewGroupView, GXView>(a => a.ViewId, b => b.Id);
            arg.Where.And<GXView>(where => where.Removed == null && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXViewGroup>(arg));
        }


        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid ViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupViewGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupViewGroup, GXViewGroup>(a => a.ViewGroupId, b => b.Id);
            arg.Where.And<GXViewGroup>(where => where.Removed == null && where.Id == ViewGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXView>> GetJoinedViews(GXDbConnection connection, Guid ViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXView>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXView, GXViewGroupView>(a => a.Id, b => b.ViewId);
            arg.Joins.AddInnerJoin<GXViewGroupView, GXViewGroup>(a => a.ViewGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupViewGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXViewGroup>(where => where.Removed == null && where.Id == ViewGroupId);
            return (await connection.SelectAsync<GXView>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByViewGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXViewGroup), groupId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByViewGroups(s => s.Id!,
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
            await GXHistoryMutation.RunDeleteAsync<GXViewGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXViewGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXViewGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXViewGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetViewGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                   DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXViewGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            Dictionary<GXViewGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXViewGroup it in exists)
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
                GXViewGroup tmp = new() { Id = it.Key.Id, Name = it.Key.Name };
                _notificationCache.Add(users, new GXTarget(GXViewGroupPolicies.Delete, tmp.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXViewGroup>> ListAsync(
            ListViewGroups? request,
            ListViewGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXViewGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetViewGroupsByUser(s => "*", userId, null,
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
                            arg.Joins.AddLeftJoin<GXUserGroupViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXViewGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXViewGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXViewGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXViewGroup>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXViewGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXViewGroup>(q => GXSql.Count(1));
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
            GXViewGroup[] groups = [.. (await connection.SelectAsync<GXViewGroup>(arg))];
            if (response != null)
            {
                response.ViewGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXViewGroup> ReadAsync(Guid id, Expression<Func<GXViewGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXViewGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetViewGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Add<GXViewGroup>();
                arg.Columns.Exclude<GXViewGroup>(e => e.Views!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = await connection.SingleOrDefaultAsync<GXViewGroup>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get blocks that belong for this block group.
                arg = GXSelectArgs.Select<GXView>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddLeftJoin<GXViewGroupView, GXView>(j => j.ViewId, j => j.Id);
                arg.Where.And<GXViewGroupView>(q => q.Removed == null && q.ViewGroupId == id);
                group.Views = await connection.SelectAsync<GXView>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belong for this block group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no blocks in the group. For that reason left join is used.
                arg.Joins.AddInnerJoin<GXUserGroupViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupViewGroup>(q => q.Removed == null && q.ViewGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXViewGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXViewGroup>(w => w.Id == id);
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
            IEnumerable<GXViewGroup> ViewGroups,
            Expression<Func<GXViewGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            ViewGroups = ViewGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXViewGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ViewGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXViewGroupPolicies.Add, GXViewGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXViewGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXViewGroup it in ViewGroups)
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
                        args.Exclude<GXViewGroup>(e => new { e.UserGroups });
                        connection.Insert(args);
                        AddViewGroupToUserGroups(connection, it.Id, [.. it.UserGroups.Select(s => s.Id)]);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                            new GXTarget(GXViewGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXViewGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXViewGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Views
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXViewGroup>(q => q.Creator!);
                        }

                        connection.Update(args);
                        //Map user group to View group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddViewGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveViewGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map blocks to View group.
                        //Only adming can add blocks that are visible to all users.
                        if (it.Views != null && (User.IsInRole(GXRoles.Admin) || it.Views.Count != 0))
                        {
                            List<GXView> list3 = await GetJoinedViews(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Views.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddViewsToViewGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveViewsFromViewGroup(connection, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXViewGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                return [.. ViewGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(ViewGroups, columns, linkedCts.Token);
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
        /// <param name="ViewGroupId">View group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        private static void AddViewGroupToUserGroups(GXDbConnection connection,
            Guid ViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupViewGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupViewGroup()
                {
                    ViewGroupId = ViewGroupId,
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
        /// <param name="ViewGroupId">View group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        private static void RemoveViewGroupFromUserGroups(GXDbConnection connection,
            Guid ViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupViewGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupViewGroup>(w => w.UserGroupId == ug &&
                    w.ViewGroupId == ViewGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="ViewGroupId">View group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        private static void AddViewsToViewGroup(GXDbConnection connection,
            Guid ViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXViewGroupView> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXViewGroupView()
                {
                    ViewGroupId = ViewGroupId,
                    ViewId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="ViewGroupId">View group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        private static void RemoveViewsFromViewGroup(GXDbConnection connection,
            Guid ViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXViewGroupView>();
            foreach (var it in groups)
            {
                args.Where.Or<GXViewGroupView>(w => w.ViewId == it &&
                    w.ViewGroupId == ViewGroupId);
            }
            connection.Delete(args);
        }
    }
}
