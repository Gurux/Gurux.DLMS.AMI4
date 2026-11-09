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
using Gurux.DLMS.AMI.Client.Pages.ComponentView;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ComponentViewRepository : IComponentViewRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IComponentViewGroupRepository _componentViewGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentViewRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IComponentViewGroupRepository componentViewGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _componentViewGroupRepository = componentViewGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXComponentView), id, action, GXComponentViewPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? componentViewId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByComponentView(s => s.Id!,
                ServerHelpers.GetUserId(User), componentViewId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (componentViewId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXComponentView), componentViewId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByComponentViews(s => s.Id!,
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
        public async Task DeleteAsync(
            IEnumerable<Guid> componentViews,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            componentViews = componentViews.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXComponentView, Guid>(_dbFactory, User, _serviceProvider, componentViews,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Delete);
            componentViews.ValidateIds();
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXComponentView>(a => a.Id);
                if (componentViews?.Any() == true)
                {
                    arg.Where.And<GXComponentView>(q => componentViews.Contains(q.Id));
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXComponentView> list = connection.Select<GXComponentView>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXComponentView, List<string>> updates = [];
                foreach (GXComponentView it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXComponentView>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                foreach (var it in updates)
                {
                    var users = it.Value;
                    if (users == null)
                    {
                        break;
                    }
                    GXComponentView tmp = new() { Id = it.Key.Id };
                    _notificationCache.Add(users, new GXTarget(GXComponentViewPolicies.Delete, tmp.Id));
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
                        await DeleteAsync(componentViews, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXComponentView>> ListAsync(
            ListComponentViews? request,
            ListComponentViewsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.View);
            GXSelectArgs arg;
            if (request != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the component views.
                arg = GXSelectArgs.SelectAll<GXComponentView>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetComponentViewsByUser(s => "*", userId);
                arg.Columns.Clear();
                arg.Columns.Add<GXComponentView>();
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXComponentView>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXComponentView>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXComponentView>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXComponentView>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXComponentView>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXComponentView>(q => q.Id);
            }
            GXComponentView[] componentViews = [.. (await connection.SelectAsync<GXComponentView>(arg))];
            if (response != null)
            {
                response.ComponentViews = componentViews;
                if (response.Count == 0)
                {
                    response.Count = componentViews.Length;
                }
            }
            return componentViews;
        }

        /// <inheritdoc />
        public async Task<GXComponentView> ReadAsync(Guid id, Expression<Func<GXComponentView, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the component views.
                    arg = GXSelectArgs.SelectAll<GXComponentView>(w => w.Id == id);
                    arg.Joins.AddLeftJoin<GXComponentView, GXComponentViewGroupComponentView>(x => x.Id, y => y.ComponentViewId);
                    arg.Joins.AddLeftJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetComponentViewsByUser(s => "*", userId, id);
                    arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id);
                }
                arg.Columns.Add<GXComponentViewGroup>();
                arg.Columns.Exclude<GXComponentViewGroup>(e => e.ComponentViews!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXComponentView? view = await connection.SingleOrDefaultAsync<GXComponentView>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXComponentView, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXComponentView>(w => w.Id == id);
                view.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return view;

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
            IEnumerable<GXComponentView> componentViews,
            Expression<Func<GXComponentView, object>>? columns, CancellationToken cancellationToken = default)
        {
            componentViews = componentViews.ToArray();
            return await GXHistoryMutation.RunAsync<GXComponentView, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, componentViews,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXComponentViewPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXComponentView, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXComponentView componentView in componentViews)
                {
                    if (string.IsNullOrEmpty(componentView.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (componentView.ComponentViewGroups == null || componentView.ComponentViewGroups.Count == 0)
                    {
                        ListComponentViewGroups request = new()
                        {
                            Filter = new GXComponentViewGroup() { Default = true }
                        };
                        componentView.ComponentViewGroups =
                        [
                            .. await _componentViewGroupRepository.ListAsync(request, null, CancellationToken.None),
                        ];
                        //It's OK if component doesn't belong to component group.
                    }
                    if (componentView.Id == Guid.Empty)
                    {
                        componentView.CreationTime = now;
                        componentView.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(componentView);
                        args.Exclude<GXComponentView>(q => new
                        {
                            q.Updated,
                            q.ComponentViewGroups
                        });
                        connection.Insert(args);
                        AddComponentViewToComponentViewGroups(connection, componentView.Id, componentView.ComponentViewGroups);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXComponentView>(q => q.Updated!, where => where.Id == componentView.Id);
                        DateTimeOffset? updated = connection.SingleOrDefault<DateTimeOffset?>(m);
                        if (updated != null && updated != componentView.Updated)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        List<string> users = await GetNotifiedUsers(componentView.Id, NotificationAction.Edit, connection);
                        GXUpdateArgs args = GXUpdateArgs.Update(componentView, columns);
                        args.Exclude<GXComponentView>(q => new
                        {
                            q.CreationTime,
                            q.ComponentViewGroups
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            componentView.Creator == null ||
                            string.IsNullOrEmpty(componentView.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXComponentViewGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map component view groups to component view.
                        List<GXComponentViewGroup> componentViewGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IComponentViewGroupRepository componentViewGroupRepository = scope.ServiceProvider.GetRequiredService<IComponentViewGroupRepository>();
                            componentViewGroups = await componentViewGroupRepository.GetJoinedComponentViewGroups(componentView.Id);
                        }
                        var comparer = new UniqueComparer<GXComponentViewGroup, Guid>();
                        List<GXComponentViewGroup> removedComponentViewGroups = [.. componentViewGroups.Except(componentView.ComponentViewGroups, comparer)];
                        List<GXComponentViewGroup> addedComponentViewGroups = [.. componentView.ComponentViewGroups.Except(componentViewGroups, comparer)];
                        if (removedComponentViewGroups.Count != 0)
                        {
                            RemoveComponentViewsFromComponentViewGroup(connection, componentView.Id, removedComponentViewGroups);
                        }
                        if (addedComponentViewGroups.Count != 0)
                        {
                            AddComponentViewToComponentViewGroups(connection, componentView.Id, addedComponentViewGroups);
                        }
                        updates[componentView] = users;
                    }
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXComponentViewPolicies.Edit, it.Key.Id, it.Key.Name));
                }
                return [.. componentViews.Select(s => s.Id)];


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
                        return await UpsertAsync(componentViews, columns, linkedCts.Token);
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
        /// Map component view group to component view.
        /// </summary>
        /// <param name="componentViewId">Component view ID.</param>
        /// <param name="groups">Group IDs of the component view groups where the component view is added.</param>
        /// <param name="connection">The database connection.</param>
        private static void AddComponentViewToComponentViewGroups(GXDbConnection connection, Guid componentViewId, IEnumerable<GXComponentViewGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXComponentViewGroupComponentView> list = [];
            foreach (GXComponentViewGroup it in groups)
            {
                list.Add(new GXComponentViewGroupComponentView()
                {
                    ComponentViewId = componentViewId,
                    ComponentViewGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between component view group and component view.
        /// </summary>
        /// <param name="componentViewId">Component view ID.</param>
        /// <param name="groups">Group IDs of the component view groups where the component view is removed.</param>
        /// <param name="connection">The database connection.</param>
        private static void RemoveComponentViewsFromComponentViewGroup(GXDbConnection connection, Guid componentViewId, IEnumerable<GXComponentViewGroup> groups)
        {
            foreach (var it in groups)
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXComponentViewGroupComponentView>(
                    w => w.ComponentViewId == componentViewId && w.ComponentViewGroupId == it.Id);
                connection.Delete(args);
            }
        }


        /// <summary>
        /// Refresh component view(s).
        /// </summary>
        public async Task<bool> RefrestAsync(CancellationToken cancellationToken = default)
        {
            Dictionary<GXComponentView, List<string>> updates = [];
            List<GXComponentView> insertList = [];
            List<GXComponentView> updateList = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (Type it in typeof(Client.Pages.Admin.ActiveDeviceErrors).Assembly.GetTypes())
            {
                if (typeof(IGXAmiComponent).IsAssignableFrom(it) && it.FullName != null)
                {
                    GXSelectArgs args = GXSelectArgs.SelectAll<GXComponentView>(where => where.ClassName == it.FullName);
                    GXComponentView? item = await connection.SingleOrDefaultAsync<GXComponentView>(args);
                    IGXAmiComponent tmp = (IGXAmiComponent?)Activator.CreateInstance(it)
                        ?? throw new InvalidOperationException($"Unable to create component {it.FullName}.");
                    string? configurationUI = null;
                    if (tmp.ConfigurationUI != null)
                    {
                        configurationUI = tmp.ConfigurationUI.FullName;
                    }
                    if (item == null)
                    {
                        item = new GXComponentView()
                        {
                            ClassName = it.FullName,
                            Name = tmp.Name,
                            ConfigurationUI = configurationUI,
                            Icon = tmp.Icon
                        };
                        insertList.Add(item);
                        updates[item] = await GetNotifiedUsers(item.Id, NotificationAction.Edit, connection);
                    }
                    else
                    {
                        if (item.Icon != tmp.Icon || item.Name != tmp.Name || item.ConfigurationUI != configurationUI)
                        {
                            item.Icon = tmp.Icon;
                            item.Name = tmp.Name;
                            item.ConfigurationUI = configurationUI;
                            updateList.Add(item);
                            updates[item] = await GetNotifiedUsers(item.Id, NotificationAction.Edit, connection);
                        }
                    }
                }
            }
            await connection.InsertAsync(GXInsertArgs.InsertRange(insertList), cancellationToken);
            await connection.UpdateAsync(GXUpdateArgs.UpdateRange(updateList), cancellationToken);
            foreach (var it in updates)
            {
                _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                    new GXTarget(GXComponentViewPolicies.Edit, it.Key.Id, it.Key.Name));
            }
            return insertList.Count != 0;
        }
    }
}
