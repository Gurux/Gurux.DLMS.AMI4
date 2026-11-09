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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ViewRepository : IViewRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewGroupRepository _viewGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ViewRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IViewGroupRepository viewGroupRepository,
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
            _viewGroupRepository = viewGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXView), id, action, GXViewPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? viewId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByView(s => s.Id!,
                ServerHelpers.GetUserId(User), viewId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (viewId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXView), viewId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? viewIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByViews(s => s.Id!,
                ServerHelpers.GetUserId(User), viewIds);
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
            IEnumerable<Guid> views,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            views = views.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXView, Guid>(_dbFactory, User, _serviceProvider, views,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXView>(a => a.Id, q => views.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXView> list = connection.Select<GXView>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXView, List<string>> updates = [];
                foreach (GXView it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXView>(it.Id), cancellationToken);
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
                    GXView tmp = new() { Id = it.Key.Id };
                    _notificationCache.Add(users, new GXTarget(GXViewPolicies.Delete, tmp.Id));
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
                        await DeleteAsync(views, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXView>> ListAsync(
            ListViews? request,
            ListViewsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.View);

            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the views.
                arg = GXSelectArgs.SelectAll<GXView>();
                if (request?.Filter?.Removed == null)
                {
                    arg.Where.And<GXView>(view => view.Removed == null);
                }
            
                if (request?.Exclude?.Any() == true)
                {
                    arg.Where.And<GXView>(w => !request.Exclude.Contains(w.Id));
                }
            
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXView>(w => request.Included.Contains(w.Id));
                }
            }
            else if (User.Identity?.IsAuthenticated == true)
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetViewsByUser(s => "*", userId, null, request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            else
            {
                return [];
            //Anonymous user can see only views in public view groups.
            }
            if (request?.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.Creator;
                try
                {
                    request.Filter.Creator = null;
                    arg.Where.FilterBy(request.Filter);
                }
                finally
                {
                    request.Filter.Creator = orig;
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXView>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXView>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXView>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (!User.IsInRole(GXRoles.Admin))
            {
                var allowed = new List<Guid>();
                foreach (var candidate in await connection.SelectAsync<GXView>(GXSelectArgs.Select<GXView>(m => m.Id), cancellationToken))
                {
                    candidate.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXView), candidate.Id, cancellationToken);
                    if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, candidate.Policies, cancellationToken))
                    {
                        allowed.Add(candidate.Id);
                    }
                }
                if (allowed.Count == 0)
                {
                    if (response != null)
                    {
                        response.Count = 0;
                        response.Views = [];
                    }
                    return [];
                }
                arg.Where.And<GXView>(m => allowed.Contains(m.Id));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXView>(q => GXSql.Count(1));
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
            GXView[] views = [.. (await connection.SelectAsync<GXView>(arg))];
            foreach (var resource in views)
            {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXView), resource.Id, cancellationToken);
            }

            if (request?.Filter?.Enabled.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                views = [.. views.Where(w => w.Enabled == true)];
            }
            if (response != null)
            {
                response.Views = views;
                if (response.Count == 0)
                {
                    response.Count = views.Length;
                }
            }
            return views;
        }

        /// <inheritdoc />
        public async Task<GXView> ReadAsync(string name, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXView>(s => s.Id, w => w.Name == name && w.Removed == null);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXView? view = await connection.SingleOrDefaultAsync<GXView>(arg);
                if (view == null)
                {
                    //View is null if admin is not created.
                    return new GXView();
                }
                return await ReadAsync(view.Id, columns, cancellationToken);

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
                        return await ReadAsync(name, columns, linkedCts.Token);
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
        public async Task<GXView> ReadAsync(Guid id, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var arg = User?.Identity?.IsAuthenticated == true && !User.IsInRole(GXRoles.Admin)
                ? GXQuery.GetViewsByUser(q => "*", ServerHelpers.GetUserId(User), id)
                : GXSelectArgs.SelectAll<GXView>(m => m.Id == id && m.Removed == null);
            var view = await connection.SingleOrDefaultAsync<GXView>(arg, cancellationToken);
            view?.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXView), view.Id, cancellationToken);

            if (view == null || !await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, view.Policies, cancellationToken))
            {
                return new GXView();
            }

            var cols = GXSelectArgs.SelectAll<GXViewColumn>(l => l.View == view && l.Removed == null);
            cols.Columns.Exclude<GXViewColumn>(l => l.View!);
            cols.OrderBy.Add<GXViewColumn>(l => l.Position);
            var visible = new List<GXViewColumn>();
            foreach (var link in await connection.SelectAsync<GXViewColumn>(cols, cancellationToken))
            {
                link.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXViewColumn), link.Id, cancellationToken);
                if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, link.Policies, cancellationToken))
                {
                    visible.Add(link);
                }
            }
            view.Columns = visible;
            var groups = GXSelectArgs.SelectAll<GXViewGroup>();
            groups.Joins.AddInnerJoin<GXViewGroup, GXViewGroupView>(g => g.Id, m => m.ViewGroupId);
            groups.Where.And<GXViewGroupView>(m => m.ViewId == id);
            groups.Where.And<GXViewGroup>(g => g.Removed == null);
            groups.Columns.Exclude<GXViewGroup>(g => g.Views!);
            view.ViewGroups = await connection.SelectAsync<GXViewGroup>(groups, cancellationToken);
            var creator = GXSelectArgs.Select<GXUser>(u => new { u.Id, u.UserName });
            creator.Joins.AddInnerJoin<GXView, GXUser>(m => m.Creator!, u => u.Id!);
            creator.Where.And<GXView>(m => m.Id == id);
            view.Creator = await connection.SingleOrDefaultAsync<GXUser>(creator, cancellationToken);
            return view;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXView> views,
            Expression<Func<GXView, object>>? columns, CancellationToken cancellationToken = default)
        {
            views = views.ToArray();
            return await GXHistoryMutation.RunAsync<GXView, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, views,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.Add, GXViewPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                List<Guid> list = [];
                Dictionary<GXView, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXView? view in views)
                {
                    if (view.Columns is { Count: 0 })
                    {
                        // No deviations from the code-defined defaults: do not persist a view.
                        if (view.Id != Guid.Empty)
                        {
                            var reset = await DeleteDefaultOverrideAsync(connection, view, cancellationToken);
                            if (reset != null)
                            {
                                updates[reset] = [];
                            }
                        }
                        continue;
                    }
                    await GXResourcePolicyAccess.ValidateAsync(connection, view.Policies, cancellationToken);
                    foreach (var link in view.Columns ?? [])
                    {
                        await GXResourcePolicyAccess.ValidateAsync(connection, link.Policies, cancellationToken);
                    }

                    if (string.IsNullOrEmpty(view.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (string.IsNullOrEmpty(view.Title))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidTitle);
                    }
                    if (view.ViewGroups == null || view.ViewGroups.Count == 0)
                    {
                        ListViewGroups request = new()
                        {
                            Filter = new GXViewGroup()
                        };
                        view.ViewGroups = [.. await _viewGroupRepository.ListAsync(request, null, CancellationToken.None)];
                        if (view.ViewGroups.Count == 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (view.Id == Guid.Empty)
                    {
                        view.Creator = creator;
                        view.CreationTime = now;
                        GXInsertArgs args = GXInsertArgs.Insert(view);
                        args.Exclude<GXView>(q => new
                        {
                            q.ViewGroups,
                            q.Columns
                        });
                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Insert(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXView), view.Id, view.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        list.Add(view.Id);
                        AddViewToViewGroups(connection, view.Id, view.ViewGroups);
                        await AddColumnsToViewAsync(connection, view, view.Columns, cancellationToken);
                        updates[view] = await GetNotifiedUsers(view.Id, NotificationAction.Edit, connection);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXView>(q => q.ConcurrencyStamp!, where => where.Id == view.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != view.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        view.Updated = now;
                        view.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(view, columns);
                        args.Exclude<GXView>(q => new
                        {
                            q.CreationTime,
                            q.ViewGroups,
                            q.Columns
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            view.Creator == null ||
                            string.IsNullOrEmpty(view.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXView>(q => q.Creator!);
                        }
                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Update(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXView), view.Id, view.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        //Map view groups to view.
                        List<GXViewGroup> viewGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IViewGroupRepository viewGroupRepository = scope.ServiceProvider.GetRequiredService<IViewGroupRepository>();
                            viewGroups = await viewGroupRepository.GetJoinedViewGroups(view.Id);
                            var comparer = new UniqueComparer<GXViewGroup, Guid>();
                            List<GXViewGroup> removedViewGroups = [.. viewGroups.Except(view.ViewGroups, comparer)];
                            List<GXViewGroup> addedViewGroups = [.. view.ViewGroups.Except(viewGroups, comparer)];
                            if (removedViewGroups.Count != 0)
                            {
                                RemoveViewsFromViewGroup(connection, view.Id, removedViewGroups);
                            }
                            if (addedViewGroups.Count != 0)
                            {
                                AddViewToViewGroups(connection, view.Id, addedViewGroups);
                            }
                        }

                        updates[view] = await GetNotifiedUsers(view.Id, NotificationAction.Edit, connection);

                        //Update columns.
                        GXView iView = new()
                        { Columns = await connection.SelectAsync<GXViewColumn>(GXSelectArgs.SelectAll<GXViewColumn>(l => l.View == view), cancellationToken) };
                        if (view.Columns != null)
                        {
                            var comparer4 = new UniqueComparer<GXViewColumn, Guid>();
                            List<GXViewColumn>? addedColumns;
                            if (iView.Columns == null)
                            {
                                //If new link is added.
                                await AddColumnsToViewAsync(connection, view, view.Columns, cancellationToken);
                            }
                            else
                            {
                                List<GXViewColumn> updatedColumns = [.. view.Columns.Intersect(iView.Columns, comparer4)];
                                List<GXViewColumn> removedColumns = [.. iView.Columns.Except(view.Columns, comparer4)];
                                addedColumns = [.. view.Columns.Except(iView.Columns, comparer4)];
                                var removableColumns = new List<GXViewColumn>();
                                foreach (var link in removedColumns)
                                {
                                    link.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXViewColumn), link.Id, cancellationToken);
                                    if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, link.Policies, cancellationToken))
                                    {
                                        removableColumns.Add(link);
                                    }
                                }
                                removedColumns = removableColumns;
                                if (removedColumns != null && removedColumns.Count != 0)
                                {
                                    RemoveColumnsFromView(connection, removedColumns);
                                }
                                if (addedColumns.Count != 0)
                                {
                                    await AddColumnsToViewAsync(connection, view, addedColumns, cancellationToken);
                                }
                                if (updatedColumns.Count != 0)
                                {
                                    foreach (var it in updatedColumns)
                                    {
                                        GXUpdateArgs u = GXUpdateArgs.Update(it);
                                        u.Exclude<GXViewColumn>(e => new
                                        {
                                            e.CreationTime,
                                            e.View,
                                        });
                                        using var policyTransaction = connection.BeginTransaction();
                                        await connection.UpdateAsync(policyTransaction, u, cancellationToken);
                                        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXViewColumn), it.Id, it.Policies,
                                            cancellationToken, policyTransaction);
                                        policyTransaction.Commit();
                                    }
                                }

                            }
                        }
                    }
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXViewPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
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
                        return await UpsertAsync(views, columns, linkedCts.Token);
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

        private async Task<GXView?> DeleteDefaultOverrideAsync(GXDbConnection connection, GXView view,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.Edit);
            var stored = await connection.SingleOrDefaultAsync<GXView>(
                GXSelectArgs.SelectAll<GXView>(v => v.Id == view.Id), cancellationToken);
            if (stored == null)
            {
                return null;
            }
            if (!string.Equals(stored.ConcurrencyStamp, view.ConcurrencyStamp, StringComparison.Ordinal))
            {
                throw new ArgumentException(Properties.Resources.ContentEdited);
            }
            var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXView), stored.Id, cancellationToken);
            if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, policies, cancellationToken))
            {
                throw new UnauthorizedAccessException();
            }
            var columns = await connection.SelectAsync<GXViewColumn>(
                GXSelectArgs.Select<GXViewColumn>(c => c.Id, c => c.View == stored), cancellationToken);
            foreach (var column in columns)
            {
                policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXViewColumn), column.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var current = await connection.SingleOrDefaultAsync<GXView>(transaction,
                GXSelectArgs.Select<GXView>(v => new { v.Id, v.ConcurrencyStamp }, v => v.Id == stored.Id), cancellationToken);
            if (current == null || !string.Equals(current.ConcurrencyStamp, view.ConcurrencyStamp, StringComparison.Ordinal))
            {
                throw new ArgumentException(Properties.Resources.ContentEdited);
            }
            await DeleteViewContentsAsync(connection, transaction, stored, cancellationToken);
            await connection.DeleteAsync(transaction,
                GXDeleteArgs.Delete<GXViewGroupView>(m => m.ViewId == stored.Id), cancellationToken);
            var deleted = await connection.DeleteAsync(transaction,
                GXDeleteArgs.Delete<GXView>(v => v.Id == stored.Id && v.ConcurrencyStamp == stored.ConcurrencyStamp), cancellationToken);
            if (deleted != 1)
            {
                throw new ArgumentException(Properties.Resources.ContentEdited);
            }
            await transaction.CommitAsync(cancellationToken);
            return stored;
        }

        /// <summary>
        /// Map view group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="viewId">View ID.</param>
        /// <param name="groups">Group IDs of the view groups where the view is added.</param>
        private static void AddViewToViewGroups(GXDbConnection connection,
            Guid viewId, IEnumerable<GXViewGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXViewGroupView> list = [];
            foreach (GXViewGroup it in groups)
            {
                list.Add(new GXViewGroupView()
                {
                    ViewId = viewId,
                    ViewGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between view group and view.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="viewId">View ID.</param>
        /// <param name="groups">Group IDs of the view groups where the view is removed.</param>
        private static void RemoveViewsFromViewGroup(GXDbConnection connection,
            Guid viewId, IEnumerable<GXViewGroup> groups)
        {
            foreach (GXViewGroup it in groups)
            {
                connection.Delete(GXDeleteArgs.Delete<GXViewGroupView>(w => w.ViewId == viewId && w.ViewGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add link to view.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="view">View where links are joined.</param>
        /// <param name="columns">View links.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        private static async Task AddColumnsToViewAsync(GXDbConnection connection,
            GXView? view, IEnumerable<GXViewColumn>? columns, CancellationToken cancellationToken)
        {
            if (columns != null)
            {
                DateTime now = DateTime.Now;
                foreach (GXViewColumn it in columns)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    it.CreationTime = now;
                    it.View = view;
                    it.Visibility ??= (byte)Visibility.All;
                }
                using var policyTransaction = connection.BeginTransaction();
                connection.Insert(policyTransaction, GXInsertArgs.InsertRange(columns));
                foreach (var link in columns)
                {
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXViewColumn), link.Id, link.Policies,
                    cancellationToken, policyTransaction);
                }

                policyTransaction.Commit();
            }
        }

        /// <summary>
        /// Remove map between view group and view.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="links">View links.</param>
        private static void RemoveColumnsFromView(GXDbConnection connection,
            IEnumerable<GXViewColumn> links)
        {
            foreach (GXViewColumn it in links)
            {
                connection.Delete(GXDeleteArgs.Delete<GXViewColumn>(w => w.Id == it.Id));
            }
        }

        /// <summary>
        /// Table defaults are defined by their components; only overrides are persisted.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Preserves the existing instance API used by repository consumers.")]
        internal Task AddDefaultViews(GXDbConnection connection, IDbTransaction? transaction, GXViewGroup adminGroup)
        {
            _ = connection;
            _ = transaction;
            _ = adminGroup;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Component-defined table defaults require no persisted override rows.
            return _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.Edit);
        }

        /// <inheritdoc />
        public async Task ResetToDefaultsAsync(IEnumerable<Guid> views, CancellationToken cancellationToken = default)
        {
            views = views.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXView, Guid>(_dbFactory, User, _serviceProvider, views,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXViewPolicies.Edit);
            ArgumentNullException.ThrowIfNull(views);
            var ids = views.Distinct().ToArray();
            if (ids.Length == 0)
            {
                throw new ArgumentException("Select at least one built-in view.", nameof(views));
            }

            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var changed = await ResetViewsToDefaultsAsync(connection, ids, new GXUser { Id = ServerHelpers.GetUserId(User) }, [], cancellationToken);
            foreach (var id in changed)
            {
                _notificationCache.Add(await GetNotifiedUsers(id, NotificationAction.Edit, connection),
                    new GXTarget(GXViewPolicies.Edit, id));
            }
        }, cancellationToken);
        }

        // Validate the complete selection before mutation and commit the reset as one operation.
        internal static async Task<Guid[]> ResetViewsToDefaultsAsync(GXDbConnection connection, Guid[] ids,
            GXUser creator, Guid[] userGroups, CancellationToken cancellationToken = default)
        {
            _ = userGroups;
            _ = creator;
            ids = [.. ids.Distinct()];
            if (ids.Length == 0)
            {
                throw new ArgumentException("Select at least one built-in view.", nameof(ids));
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var select = GXSelectArgs.SelectAll<GXView>();
            if (ids.Length != 0)
            {
                select.Where.And<GXView>(v => ids.Contains(v.Id));
            }
            var targets = await connection.SelectAsync<GXView>(transaction, select, cancellationToken);
            if (ids.Length != 0 && (targets.Count != ids.Length || targets.Any(v => !GXViews.IsBuiltIn(v.Name))))
            {
                throw new ArgumentException("Every selected view must exist and have a built-in default definition.", nameof(ids));
            }
            foreach (var target in targets)
            {
                await DeleteViewContentsAsync(connection, transaction, target, cancellationToken);
                var defaults = new GXView(target.Name)
                {
                    Id = target.Id,
                    Updated = DateTimeOffset.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    Preload = false
                };
                var update = GXUpdateArgs.Update(defaults);
                update.Exclude<GXView>(v => new { v.Creator, v.CreationTime, v.ViewGroups, v.Columns });
                await connection.UpdateAsync(transaction, update, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return [.. targets.Select(view => view.Id)];
        }
        private static async Task DeleteViewContentsAsync(GXDbConnection connection, IDbTransaction transaction,
            GXView view, CancellationToken cancellationToken)
        {
            var columns = await connection.SelectAsync<GXViewColumn>(transaction,
                GXSelectArgs.Select<GXViewColumn>(c => c.Id, c => c.View == view), cancellationToken);
            foreach (var column in columns)
            {
                await connection.DeleteAsync(transaction,
                    GXDeleteArgs.Delete<GXViewColumnPolicy>(p => p.ViewColumnId == column.Id), cancellationToken);
            }
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXViewColumn>(c => c.View == view), cancellationToken);
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXViewPolicy>(p => p.ViewId == view.Id), cancellationToken);
        }

    }
}
