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
using Gurux.DLMS.AMI.Client.Pages.Agent;
using Gurux.DLMS.AMI.Client.Pages.Block;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ComponentViewGroupRepository : IComponentViewGroupRepository
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
        public ComponentViewGroupRepository(
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
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXComponentViewGroup), id, action, GXComponentViewPolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<GXComponentViewGroup>> GetJoinedComponentViewGroups(Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXComponentViewGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXComponentViewGroup, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewGroupId);
            arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(a => a.ComponentViewId, b => b.Id);
            arg.Where.And<GXComponentView>(where => where.Removed == null && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXComponentViewGroup>(arg));
        }

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid componentViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            arg.Where.And<GXComponentViewGroup>(where => where.Removed == null && where.Id == componentViewGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXComponentView>> GetJoinedComponentViews(GXDbConnection connection, Guid componentViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXComponentView>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXComponentView, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewId);
            arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            arg.Joins.AddInnerJoin<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXComponentViewGroup>(where => where.Removed == null && where.Id == componentViewGroupId);
            return (await connection.SelectAsync<GXComponentView>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByComponentViewGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXComponentViewGroup), groupId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByComponentViewGroups(s => s.Id!,
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
        public async Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXComponentViewGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.Delete);
            groups.ValidateIds();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXComponentViewGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXComponentViewGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXComponentViewGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed component view groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetComponentViewGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                     DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXComponentViewGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            DateTime now = DateTime.Now;
            Dictionary<GXComponentViewGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXComponentViewGroup it in exists)
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
                        await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXComponentViewGroup>(it.Id), cancellationToken);
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
                var users = it.Value;
                if (users == null)
                {
                    break;
                }
                GXComponentViewGroup tmp = new() { Id = it.Key.Id, Name = it.Key.Name };
                _notificationCache.Add(users, new GXTarget(GXComponentViewGroupPolicies.Delete, tmp.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXComponentViewGroup>> ListAsync(
            ListComponentViewGroups? request,
            ListComponentViewGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agent groups.
                arg = GXSelectArgs.SelectAll<GXComponentViewGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetComponentViewGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If component view groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupComponentViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXComponentViewGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXComponentViewGroup>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXComponentViewGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXComponentViewGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXComponentViewGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXComponentViewGroup>(q => q.CreationTime!);
            }
            GXComponentViewGroup[] groups = [.. (await connection.SelectAsync<GXComponentViewGroup>(arg))];
            if (response != null)
            {
                response.ComponentViewGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXComponentViewGroup> ReadAsync(Guid id, Expression<Func<GXComponentViewGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.View);

                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXComponentViewGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetComponentViewGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXComponentViewGroup>(e => e.ComponentViews!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXComponentViewGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get component views that belongs for this component view group.
                arg = GXSelectArgs.Select<GXComponentView>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(j => j.ComponentViewId, j => j.Id);
                arg.Where.And<GXComponentViewGroupComponentView>(q => q.Removed == null && q.ComponentViewGroupId == id);
                group.ComponentViews = await connection.SelectAsync<GXComponentView>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this component view group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupComponentViewGroup>(q => q.Removed == null && q.ComponentViewGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXComponentViewGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXComponentViewGroup>(w => w.Id == id);
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
            IEnumerable<GXComponentViewGroup> ComponentViewGroups,
            Expression<Func<GXComponentViewGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            ComponentViewGroups = ComponentViewGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXComponentViewGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ComponentViewGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewGroupPolicies.Add, GXComponentViewGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXComponentViewGroup, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXComponentViewGroup it in ComponentViewGroups)
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
                        args.Exclude<GXComponentViewGroup>(e => new
                        {
                            e.Updated,
                            e.UserGroups
                        });
                        connection.Insert(args);
                        AddComponentViewGroupToUserGroups(connection, it.Id, [.. it.UserGroups.Select(s => s.Id)]);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXComponentViewGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }

                        List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection);
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXComponentViewGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.ComponentViews
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXComponentViewGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to ComponentView group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddComponentViewGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveComponentViewGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map componentViews to ComponentView group.
                        if (it.ComponentViews != null && it.ComponentViews.Count != 0)
                        {
                            List<GXComponentView> list3 = await GetJoinedComponentViews(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.ComponentViews.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddComponentViewsToComponentViewGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveComponentViewsFromComponentViewGroup(connection, it.Id, removed);
                            }
                        }
                        updates[it] = users;
                    }
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXComponentViewGroupPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                return [.. ComponentViewGroups.Select(s => s.Id)];


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
                        return await UpsertAsync(ComponentViewGroups, columns, linkedCts.Token);
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
        /// Map componentView group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is added.</param>
        private static void AddComponentViewGroupToUserGroups(GXDbConnection connection, Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupComponentViewGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupComponentViewGroup()
                {
                    ComponentViewGroupId = componentViewGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between componentView group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is removed.</param>
        private static void RemoveComponentViewGroupFromUserGroups(GXDbConnection connection,
            Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupComponentViewGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupComponentViewGroup>(w => w.UserGroupId == ug &&
                    w.ComponentViewGroupId == componentViewGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map componentViews to componentView group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is added.</param>
        private static void AddComponentViewsToComponentViewGroup(GXDbConnection connection,
            Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXComponentViewGroupComponentView> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXComponentViewGroupComponentView()
                {
                    ComponentViewGroupId = componentViewGroupId,
                    ComponentViewId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between componentViews and componentView group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is removed.</param>
        private static void RemoveComponentViewsFromComponentViewGroup(GXDbConnection connection,
            Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXComponentViewGroupComponentView>();
            foreach (var it in groups)
            {
                args.Where.Or<GXComponentViewGroupComponentView>(w => w.ComponentViewId == it &&
                    w.ComponentViewGroupId == componentViewGroupId);
            }
            connection.Delete(args);
        }
    }
}
