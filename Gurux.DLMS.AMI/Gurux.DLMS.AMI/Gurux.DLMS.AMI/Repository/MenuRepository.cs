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

using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.UI.Components.Enums;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public partial class MenuRepository : IMenuRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMenuGroupRepository _menuGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IMenuGroupRepository menuGroupRepository,
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
            _menuGroupRepository = menuGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXMenu), id, action, GXMenuPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? menuId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByMenu(s => s.Id!,
                ServerHelpers.GetUserId(User), menuId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (menuId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXMenu), menuId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? menuIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByMenus(s => s.Id!, ServerHelpers.GetUserId(User), menuIds);
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
            IEnumerable<Guid> menus,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            menus = menus.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXMenu, Guid>(_dbFactory, User, _serviceProvider, menus,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(a => a.Id, q => menus.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXMenu> list = connection.Select<GXMenu>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXMenu, List<string>> updates = [];
                foreach (GXMenu it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXMenu>(it.Id), cancellationToken);
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
                    GXMenu tmp = new() { Id = it.Key.Id };
                    _notificationCache.Add(users, new GXTarget(GXMenuPolicies.Delete, tmp.Id));
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
                        await DeleteAsync(menus, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXMenu>> ListAsync(
            ListMenus? request,
            ListMenusResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.View);

            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the menus.
                arg = GXSelectArgs.SelectAll<GXMenu>();
                if (request?.Filter?.Removed == null)
                {
                    arg.Where.And<GXMenu>(menu => menu.Removed == null);
                }

                if (request?.Exclude?.Any() == true)
                {
                    arg.Where.And<GXMenu>(w => !request.Exclude.Contains(w.Id));
                }

                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXMenu>(w => request.Included.Contains(w.Id));
                }
            }
            else if (User.Identity?.IsAuthenticated == true)
            {
                arg = GXSelectArgs.SelectAll<GXMenu>();
                if (request?.Filter?.Removed == null)
                {
                    arg.Where.And<GXMenu>(m => m.Removed == null);
                }
                if (request?.Exclude?.Any() == true)
                {
                    arg.Where.And<GXMenu>(m => !request.Exclude.Contains(m.Id));
                }
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXMenu>(m => request.Included.Contains(m.Id));
                }
            }
            else
            {
                return [];
                //Anonymous user can see only menus in public menu groups.
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
                arg.OrderBy.Add<GXMenu>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXMenu>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXMenu>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (!User.IsInRole(GXRoles.Admin))
            {
                var allowed = new List<Guid>();
                foreach (var candidate in await connection.SelectAsync<GXMenu>(GXSelectArgs.Select<GXMenu>(m => m.Id), cancellationToken))
                {
                    candidate.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenu), candidate.Id, cancellationToken);
                    if (await CanViewAsync(connection, candidate, cancellationToken))
                    {
                        allowed.Add(candidate.Id);
                    }
                }
                if (allowed.Count == 0)
                {
                    if (response != null)
                    {
                        response.Count = 0;
                        response.Menus = [];
                    }
                    return [];
                }
                arg.Where.And<GXMenu>(m => allowed.Contains(m.Id));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXMenu>(q => GXSql.Count(1));
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
            GXMenu[] menus = [.. (await connection.SelectAsync<GXMenu>(arg))];
            foreach (var resource in menus)
            {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenu), resource.Id, cancellationToken);
            }

            if (request?.Filter?.Enabled.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                menus = [.. menus.Where(w => w.Enabled == true)];
            }
            if (response != null)
            {
                response.Menus = menus;
                if (response.Count == 0)
                {
                    response.Count = menus.Length;
                }
            }
            return menus;
        }

        internal static void ValidateAudience(GXMenu menu)
        {
            menu.MenuGroups ??= [];
            if (menu.MenuGroups.Count == 0 && menu.Policies?.Any(p => !string.IsNullOrWhiteSpace(p)) != true)
            {
                throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
            }
        }

        // Group assignments restrict access; an ungrouped menu needs an explicit policy.
        internal async Task<bool> CanViewAsync(GXDbConnection connection, GXMenu menu, CancellationToken cancellationToken = default)
        {
            if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, menu.Policies, cancellationToken))
            {
                return false;
            }
            if (User.IsInRole(GXRoles.Admin))
            {
                return true;
            }
            var groups = GXSelectArgs.Select<GXMenuGroup>(g => g.Id, g => g.Removed == null);
            groups.Joins.AddInnerJoin<GXMenuGroup, GXMenuGroupMenu>(g => g.Id, m => m.MenuGroupId);
            groups.Where.And<GXMenuGroupMenu>(m => m.MenuId == menu.Id && m.Removed == null);
            if (!(await connection.SelectAsync<GXMenuGroup>(groups, cancellationToken)).Any())
            {
                return menu.Policies?.Any(p => !string.IsNullOrWhiteSpace(p)) == true;
            }
            var userId = ServerHelpers.GetUserId(User, false);
            if (userId == null || User.Identity?.IsAuthenticated != true)
            {
                return false;
            }
            groups.Joins.AddInnerJoin<GXMenuGroup, GXUserGroupMenuGroup>(g => g.Id, m => m.MenuGroupId);
            groups.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXUserGroup>(m => m.UserGroupId, g => g.Id);
            groups.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(g => g.Id, m => m.UserGroupId);
            groups.Where.And<GXUserGroupMenuGroup>(m => m.Removed == null);
            groups.Where.And<GXUserGroup>(g => g.Removed == null);
            groups.Where.And<GXUserGroupUser>(m => m.UserId == userId && m.Removed == null);
            return (await connection.SelectAsync<GXMenuGroup>(groups, cancellationToken)).Any();
        }

        /// <inheritdoc />
        public async Task<GXMenu> ReadAsync(string name, Expression<Func<GXMenu, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == name && w.Removed == null);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXMenu? menu = await connection.SingleOrDefaultAsync<GXMenu>(arg);
                if (menu == null)
                {
                    //Menu is null if admin is not created.
                    return new GXMenu();
                }
                return await ReadAsync(menu.Id, columns, cancellationToken);

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
        public async Task<GXMenu> ReadAsync(Guid id, Expression<Func<GXMenu, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var arg = GXSelectArgs.SelectAll<GXMenu>(m => m.Id == id && m.Removed == null);
            var menu = await connection.SingleOrDefaultAsync<GXMenu>(arg, cancellationToken);
            if (menu == null)
            {
                if (User?.Identity?.IsAuthenticated == true && !User.IsInRole(GXRoles.Admin))
                {
                    // A menu outside the user's groups is unavailable, just like a
                    // menu rejected by its resource policies below.
                    return new GXMenu();
                }
                throw new ArgumentException(Properties.Resources.MenuNotFound);
            }
            menu.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenu), menu.Id, cancellationToken);

            if (!await CanViewAsync(connection, menu, cancellationToken))
            {
                return new GXMenu();
            }

            var links = GXSelectArgs.SelectAll<GXMenuLink>(l => l.Menu == menu && l.Removed == null);
            links.Columns.Exclude<GXMenuLink>(l => l.Menu!);
            links.OrderBy.Add<GXMenuLink>(l => l.Order);
            links.Columns.Add<GXConfiguration>(c => new { c.Id, c.Name, c.Enabled });
            links.Joins.AddLeftJoin<GXMenuLink, GXConfiguration>(l => l.Configuration!, c => c.Id);
            var visible = new List<GXMenuLink>();
            foreach (var link in await connection.SelectAsync<GXMenuLink>(links, cancellationToken))
            {
                if (link.Configuration != null)
                {
                    if (!link.Configuration.Enabled)
                    {
                        continue;
                    }
                    link.Configuration = new GXConfiguration { Id = link.Configuration.Id, Name = link.Configuration.Name };
                }
                link.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenuLink), link.Id, cancellationToken);
                if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, link.Policies, cancellationToken))
                {
                    visible.Add(link);
                }
            }
            menu.Links = visible;
            var groups = GXSelectArgs.SelectAll<GXMenuGroup>();
            groups.Joins.AddInnerJoin<GXMenuGroup, GXMenuGroupMenu>(g => g.Id, m => m.MenuGroupId);
            groups.Where.And<GXMenuGroupMenu>(m => m.MenuId == id);
            groups.Where.And<GXMenuGroup>(g => g.Removed == null);
            groups.Columns.Exclude<GXMenuGroup>(g => g.Menus!);
            menu.MenuGroups = await connection.SelectAsync<GXMenuGroup>(groups, cancellationToken);
            var creator = GXSelectArgs.Select<GXUser>(u => new { u.Id, u.UserName });
            creator.Joins.AddInnerJoin<GXMenu, GXUser>(m => m.Creator!, u => u.Id!);
            creator.Where.And<GXMenu>(m => m.Id == id);
            menu.Creator = await connection.SingleOrDefaultAsync<GXUser>(creator, cancellationToken);
            return menu;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXMenu> menus,
            Expression<Func<GXMenu, object>>? columns, CancellationToken cancellationToken = default)
        {
            menus = menus.ToArray();
            return await GXHistoryMutation.RunAsync<GXMenu, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, menus,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.Add, GXMenuPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                List<Guid> list = [];
                Dictionary<GXMenu, List<string>> updates = [];
                bool translationsChanged = false;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXMenu? menu in menus)
                {
                    if (!Enum.IsDefined(menu.Kind) || menu.Kind == MenuKind.All)
                    {
                        throw new ArgumentException("Invalid menu kind.");
                    }
                    if (menu.Location is { } location && !Enum.IsDefined(location))
                    {
                        throw new ArgumentException("Invalid menu location.");
                    }
                    await GXResourcePolicyAccess.ValidateAsync(connection, menu.Policies, cancellationToken);
                    foreach (var link in menu.Links ?? [])
                    {
                        if (link.Configuration != null)
                        {
                            var configurationId = link.Configuration.Id;
                            var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(
                                GXSelectArgs.Select<GXConfiguration>(c => c.Id, c => c.Id == configurationId), cancellationToken) ?? throw new ArgumentException("Unknown configuration reference.");
                        }
                        await GXResourcePolicyAccess.ValidateAsync(connection, link.Policies, cancellationToken);
                    }

                    if (string.IsNullOrEmpty(menu.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (string.IsNullOrEmpty(menu.Title))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidTitle);
                    }
                    ValidateAudience(menu);
                    if (menu.Id == Guid.Empty)
                    {
                        menu.Creator = creator;
                        menu.CreationTime = now;
                        GXInsertArgs args = GXInsertArgs.Insert(menu);
                        args.Exclude<GXMenu>(q => new
                        {
                            q.MenuGroups,
                            q.Links
                        });
                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Insert(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenu), menu.Id, menu.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        list.Add(menu.Id);
                        AddMenuToMenuGroups(connection, menu.Id, menu.MenuGroups);
                        await AddLinksToMenuAsync(connection, menu, menu.Links, cancellationToken);
                        updates[menu] = await GetNotifiedUsers(menu.Id, NotificationAction.Edit, connection);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXMenu>(q => q.ConcurrencyStamp!, where => where.Id == menu.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != menu.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        menu.Updated = now;
                        menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(menu, columns);
                        args.Exclude<GXMenu>(q => new
                        {
                            q.CreationTime,
                            q.MenuGroups,
                            q.Links
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            menu.Creator == null ||
                            string.IsNullOrEmpty(menu.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXMenu>(q => q.Creator!);
                        }
                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Update(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenu), menu.Id, menu.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        //Map menu groups to menu.
                        List<GXMenuGroup> menuGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IMenuGroupRepository menuGroupRepository = scope.ServiceProvider.GetRequiredService<IMenuGroupRepository>();
                            menuGroups = await menuGroupRepository.GetJoinedMenuGroups(menu.Id);
                            var comparer = new UniqueComparer<GXMenuGroup, Guid>();
                            var requestedMenuGroups = menu.MenuGroups ?? [];
                            List<GXMenuGroup> removedMenuGroups = [.. menuGroups.Except(requestedMenuGroups, comparer)];
                            List<GXMenuGroup> addedMenuGroups = [.. requestedMenuGroups.Except(menuGroups, comparer)];
                            if (removedMenuGroups.Count != 0)
                            {
                                RemoveMenusFromMenuGroup(connection, menu.Id, removedMenuGroups);
                            }
                            if (addedMenuGroups.Count != 0)
                            {
                                AddMenuToMenuGroups(connection, menu.Id, addedMenuGroups);
                            }
                        }

                        updates[menu] = await GetNotifiedUsers(menu.Id, NotificationAction.Edit, connection);

                        //Update links.
                        GXMenu iMenu = new()
                        { Links = await connection.SelectAsync<GXMenuLink>(GXSelectArgs.SelectAll<GXMenuLink>(l => l.Menu == menu), cancellationToken) };
                        if (menu.Links != null)
                        {
                            var comparer4 = new UniqueComparer<GXMenuLink, Guid>();
                            List<GXMenuLink>? addedLinks;
                            if (iMenu.Links == null)
                            {
                                //If new link is added.
                                await AddLinksToMenuAsync(connection, menu, menu.Links, cancellationToken);
                            }
                            else
                            {
                                List<GXMenuLink> updatedLinks = [.. menu.Links.Intersect(iMenu.Links, comparer4)];
                                List<GXMenuLink> removedLinks = [.. iMenu.Links.Except(menu.Links, comparer4)];
                                addedLinks = [.. menu.Links.Except(iMenu.Links, comparer4)];
                                var removableLinks = new List<GXMenuLink>();
                                foreach (var link in removedLinks)
                                {
                                    link.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenuLink), link.Id, cancellationToken);
                                    if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, link.Policies, cancellationToken))
                                    {
                                        removableLinks.Add(link);
                                    }
                                }
                                removedLinks = removableLinks;
                                if (removedLinks != null && removedLinks.Count != 0)
                                {
                                    RemoveLinksFromMenu(connection, removedLinks);
                                }
                                if (addedLinks.Count != 0)
                                {
                                    await AddLinksToMenuAsync(connection, menu, addedLinks, cancellationToken);
                                }
                                if (updatedLinks.Count != 0)
                                {
                                    foreach (var it in updatedLinks)
                                    {
                                        var previous = iMenu.Links.Single(l => l.Id == it.Id);
                                        GXUpdateArgs u = GXUpdateArgs.Update(it);
                                        u.Exclude<GXMenuLink>(e => new
                                        {
                                            e.CreationTime,
                                            e.Menu,
                                        });
                                        using var policyTransaction = connection.BeginTransaction();
                                        await connection.UpdateAsync(policyTransaction, u, cancellationToken);
                                        if (menu.Kind == MenuKind.Configuration)
                                        {
                                            translationsChanged |= await GXConfigurationLocalization.CopyAsync(connection,
                                                policyTransaction, previous.Name, it.Name, creator, cancellationToken);
                                            translationsChanged |= await GXConfigurationLocalization.CopyAsync(connection,
                                                policyTransaction, previous.Description, it.Description, creator, cancellationToken);
                                        }
                                        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), it.Id, it.Policies,
                                            cancellationToken, policyTransaction);
                                        policyTransaction.Commit();
                                    }
                                }

                            }
                        }
                    }
                }
                if (translationsChanged)
                {
                    await new GXResourceVersions(_dbFactory).TouchTextsAsync(cancellationToken);
                    _notificationCache.Add(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]),
                        new GXTarget(GXLocalizedResourcePolicies.Edit));
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXMenuPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
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
                        return await UpsertAsync(menus, columns, linkedCts.Token);
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
        /// Map menu group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="menuId">Menu ID.</param>
        /// <param name="groups">Group IDs of the menu groups where the menu is added.</param>
        private static void AddMenuToMenuGroups(GXDbConnection connection,
            Guid menuId, IEnumerable<GXMenuGroup>? groups)
        {
            if (groups == null)
            {
                return;
            }
            DateTime now = DateTime.Now;
            List<GXMenuGroupMenu> list = [];
            foreach (GXMenuGroup it in groups)
            {
                list.Add(new GXMenuGroupMenu()
                {
                    MenuId = menuId,
                    MenuGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between menu group and menu.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="menuId">Menu ID.</param>
        /// <param name="groups">Group IDs of the menu groups where the menu is removed.</param>
        private static void RemoveMenusFromMenuGroup(GXDbConnection connection,
            Guid menuId, IEnumerable<GXMenuGroup> groups)
        {
            foreach (GXMenuGroup it in groups)
            {
                connection.Delete(GXDeleteArgs.Delete<GXMenuGroupMenu>(w => w.MenuId == menuId && w.MenuGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add link to menu.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="menu">Menu where links are joined.</param>
        /// <param name="links">Menu links.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        private static async Task AddLinksToMenuAsync(GXDbConnection connection,
            GXMenu menu, IEnumerable<GXMenuLink>? links, CancellationToken cancellationToken)
        {
            if (links != null)
            {
                DateTime now = DateTime.Now;
                foreach (GXMenuLink it in links)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (string.IsNullOrEmpty(it.Url))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidLink);
                    }
                    it.CreationTime = now;
                    it.Menu = menu;
                    it.SystemDefined ??= false;
                    it.Visibility ??= (byte)Visibility.All;
                }
                using var policyTransaction = connection.BeginTransaction();
                connection.Insert(policyTransaction, GXInsertArgs.InsertRange(links));
                foreach (var link in links)
                {
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), link.Id, link.Policies,
                    cancellationToken, policyTransaction);
                }

                policyTransaction.Commit();
            }
        }

        /// <summary>
        /// Remove map between menu group and menu.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="links">Menu links.</param>
        private static void RemoveLinksFromMenu(GXDbConnection connection,
            IEnumerable<GXMenuLink> links)
        {
            foreach (GXMenuLink it in links)
            {
                connection.Delete(GXDeleteArgs.Delete<GXMenuLink>(w => w.Id == it.Id));
            }
        }

        /// <summary>
        /// Adds a new menu link to the specified menu or returns the existing link with the given name.
        /// </summary>
        /// <remarks>If a link with the specified name already exists in the menu, the existing link is
        /// returned and no changes are made. The assigned policy controls access to the new menu link. The
        /// method updates the menu's link collection accordingly.</remarks>
        /// <param name="connection">DB connection.</param>
        /// <param name="menu">The menu to which the link will be added. Cannot be null.</param>
        /// <param name="visibility">The visibility level to assign to the menu link.</param>
        /// <param name="link">The URL or target address for the menu link. Cannot be null or empty.</param>
        /// <param name="name">The unique name of the menu link. Used to identify existing links. Cannot be null or empty.</param>
        /// <param name="description">An optional description for the menu link. May be null.</param>
        /// <param name="icon">An optional icon identifier or path for the menu link. May be null.</param>
        /// <param name="padges">An optional array of badge strings to associate with the menu link. May be null.</param>
        /// <param name="padgeTitle">An optional title for the badges associated with the menu link. May be null.</param>
        /// <param name="policy">The policy required to access the menu link. Null leaves access unrestricted.</param>
        /// <param name="type">Blazor target type.</param>
        /// <param name="configuration">Configuration opened by the link.</param>
        /// <param name="componentTypeName">Component type name when the type is resolved at runtime.</param>
        /// <returns>The menu link that was added to the menu, or the existing link if one with the specified name already
        /// exists.</returns>
        private static async Task<GXMenuLink> AddLink(GXDbConnection? connection,
            GXMenu menu,
            Visibility visibility,
            string link, string name,
            string? description = null,
            string? icon = null,
            string[]? padges = null,
            string? padgeTitle = null,
            string? policy = null,
            Type? type = null,
            GXConfiguration? configuration = null,
            string? componentTypeName = null)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenuLink>(s => s.Id,
                w => w.Menu == menu && w.Removed == null && (w.Name == name || w.Url == link));
            GXMenuLink? item = connection == null
                ? menu.Links?.FirstOrDefault(l => l.Name == name || l.Url == link)
                : connection.SingleOrDefault<GXMenuLink>(arg);
            if (item == null)
            {
                MenuLinkType targetType = type == null ? MenuLinkType.Link : MenuLinkType.Content;
                item = new GXMenuLink()
                {
                    MenuLinkType = targetType,
                    Visibility = (byte)visibility,
                    Menu = menu,
                    Order = menu.Links?.Count > 0 ? menu.Links.Max(l => l.Order) + 1 : 1,
                    Icon = ClientHelpers.GetKebabCase(icon),
                    CreationTime = menu.CreationTime,
                    Name = name,
                    Description = description,
                    Url = link,
                    Padges = padges != null ? string.Join(';', padges) : null,
                    PadgeTitle = padgeTitle,
                    TargetType = type != null ? type.FullName : componentTypeName,
                    Configuration = configuration == null ? null : new GXConfiguration
                    {
                        Id = configuration.Id,
                        Name = connection == null ? configuration.Name : null
                    },
                    Policies = string.IsNullOrWhiteSpace(policy) ? [] : [policy],
                };
                if (item.Icon == "")
                {
                    item.Icon = null;
                }

                if (connection != null)
                {
                    using var policyTransaction = connection.BeginTransaction();
                    connection.Insert(policyTransaction, GXInsertArgs.Insert(item));
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), item.Id, item.Policies, transaction: policyTransaction);
                    policyTransaction.Commit();
                }

                menu.Links?.Add(item);
            }
            return item;
        }

        internal static async Task AddConfigurationMenu(GXDbConnection? connection, GXMenuGroup group)
        {
            var proxyConfiguration = connection == null ? null : await ConfigurationRepository.EnsureHttpProxyConfigurationAsync(connection);
            var menu = connection == null ? null : await connection.SingleOrDefaultAsync<GXMenu>(
                GXSelectArgs.SelectAll<GXMenu>(m => m.Name == GXMenus.ConfigurationMenu && m.Removed == null));
            if (menu != null)
            {
                var obsolete = await connection!.SelectAsync<GXMenuLink>(
                    GXSelectArgs.SelectAll<GXMenuLink>(l => l.Menu == menu && l.Url == "config/performance" && l.Removed == null));
                foreach (var link in obsolete)
                {
                    link.Removed = DateTimeOffset.UtcNow;
                    await connection.UpdateAsync(GXUpdateArgs.Update(link, l => l.Removed!));
                }
                if (obsolete.Any())
                {
                    menu.Updated = DateTimeOffset.UtcNow;
                    menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(GXUpdateArgs.Update(menu, m => new { m.Updated, m.ConcurrencyStamp }));
                }
                if (proxyConfiguration?.Enabled == true)
                {
                    await AddLink(connection, menu, Visibility.All, "config/http-proxy", "HTTP proxy",
                        description: "Allowed HTTP servers, caching and request limits.", icon: "http",
                        policy: GXConfigurationPolicies.View, configuration: proxyConfiguration,
                        componentTypeName: typeof(Gurux.DLMS.AMI.Client.Pages.Config.HttpProxy).FullName);
                }
                await NormalizeConfigurationMenuAsync(connection!, menu);
                return; // Existing navigation belongs to its editor.
            }
            menu = new GXMenu(GXMenus.ConfigurationMenu)
            {
                Title = "Configuration",
                Kind = MenuKind.Configuration,
                Creator = group.Creator,
                CreationTime = group.CreationTime,
                SystemDefined = true,
                Preload = true,
                Policies = [GXConfigurationPolicies.View]
            };
            group.Menus?.Add(menu);
            if (connection != null)
            {
                using var transaction = connection.BeginTransaction();
                connection.Insert(transaction, GXInsertArgs.Insert(menu));
                connection.Insert(transaction, GXInsertArgs.Insert(new GXMenuGroupMenu { MenuId = menu.Id, MenuGroupId = group.Id, CreationTime = group.CreationTime }));
                await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenu), menu.Id, menu.Policies, transaction: transaction);
                transaction.Commit();
            }
            var defaults = ConfigurationRepository.CreateDefaultConfigurationSettings();
            var configurations = connection == null ? [.. defaults.Select(l => l.Configuration!)] : await connection.SelectAsync<GXConfiguration>(
                GXSelectArgs.SelectAll<GXConfiguration>(c => c.Enabled));
            foreach (var definition in defaults.OrderBy(l => l.Order))
            {
                var configuration = configurations.FirstOrDefault(c =>
                    string.Equals(c.Name, definition.Configuration?.Name, StringComparison.OrdinalIgnoreCase));
                if (configuration == null || string.IsNullOrWhiteSpace(definition.Url))
                {
                    continue;
                }
                await AddLink(connection, menu, Visibility.All, definition.Url, definition.Name!,
                    description: definition.Description, icon: definition.Icon,
                    policy: GXConfigurationPolicies.View, configuration: configuration,
                    componentTypeName: definition.TargetType);
            }
        }

        private static async Task NormalizeConfigurationMenuAsync(GXDbConnection connection, GXMenu menu)
        {
            var definitions = ConfigurationRepository.CreateDefaultConfigurationSettings();
            var configurations = await connection.SelectAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>());
            var links = await connection.SelectAsync<GXMenuLink>(
                GXSelectArgs.SelectAll<GXMenuLink>(link => link.Menu == menu && link.Removed == null));
            bool changed = false;
            foreach (var link in links)
            {
                var configuration = configurations.FirstOrDefault(item => item.Id == link.Configuration?.Id);
                var definition = definitions.FirstOrDefault(item => configuration != null &&
                    string.Equals(item.Configuration?.Name, configuration.Name, StringComparison.OrdinalIgnoreCase));
                if (definition != null && NormalizeConfigurationLink(link, definition))
                {
                    link.Updated = DateTimeOffset.UtcNow;
                    link.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(GXUpdateArgs.Update(link,
                        item => new { item.Name, item.Url, item.Updated, item.ConcurrencyStamp }));
                    changed = true;
                }
            }
            if (changed)
            {
                menu.Updated = DateTimeOffset.UtcNow;
                menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.UpdateAsync(GXUpdateArgs.Update(menu, item => new { item.Updated, item.ConcurrencyStamp }));
            }
        }

        // Change only recognized built-in routes and former default labels.
        internal static bool NormalizeConfigurationLink(GXMenuLink link, GXMenuLink definition)
        {
            if (link.Url == null || !string.Equals(
                GXConfigurationRoutes.Canonicalize(link.Url.Trim('/')),
                definition.Url, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var oldName = definition.Url switch
            {
                "config/agents" => ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agent)),
                "config/triggers" => ServerHelpers.GetInvariantString(nameof(Properties.Resources.Trigger)),
                "config/workflows" => ServerHelpers.GetInvariantString(nameof(Properties.Resources.Workflow)),
                "config/manufacturers" => ServerHelpers.GetInvariantString(nameof(Properties.Resources.Manufacturer)),
                _ => definition.Name
            };
            bool changed = link.Url != definition.Url;
            link.Url = definition.Url;
            if (link.Name == oldName && link.Name != definition.Name)
            {
                link.Name = definition.Name;
                changed = true;
            }
            return changed;
        }

        internal static async Task EnsureConfigurationMenuAsync(GXDbConnection connection, CancellationToken cancellationToken = default)
        {
            var schema = new GXSchemaManager(connection);
            if (!schema.TableExist<GXMenuGroup>() || !schema.TableExist<GXMenuLink>() || !schema.TableExist<GXConfiguration>() || !schema.TableExist<GXPolicy>() || !schema.GetColumns<GXMenuLink>().Contains("Configuration", StringComparer.OrdinalIgnoreCase))
            {
                return;
            }
            var tabsName = GXMenus.ConfigurationTab;
            var tabs = await connection.SingleOrDefaultAsync<GXMenu>(
                GXSelectArgs.Select<GXMenu>(m => m.Id, m => m.Name == tabsName && m.Removed == null), cancellationToken);
            if (tabs != null)
            {
                var removedTabs = await connection.SelectAsync<GXMenuLink>(GXSelectArgs.SelectAll<GXMenuLink>(
                    l => l.Menu == tabs && l.Url == "config/LogTypes" && l.Removed == null), cancellationToken);
                foreach (var link in removedTabs)
                {
                    link.Removed = DateTimeOffset.UtcNow;
                    await connection.UpdateAsync(GXUpdateArgs.Update(link, l => l.Removed!), cancellationToken);
                }
                if (removedTabs.Any())
                {
                    tabs.Updated = DateTimeOffset.UtcNow;
                    tabs.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(GXUpdateArgs.Update(tabs, m => new { m.Updated, m.ConcurrencyStamp }), cancellationToken);
                }
            }
            var policyName = GXConfigurationPolicies.View;
            var policy = await connection.SingleOrDefaultAsync<GXPolicy>(
                GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.Name == policyName), cancellationToken);
            if (policy == null)
            {
                return;
            }
            var groupName = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Users));
            var group = await connection.SingleOrDefaultAsync<GXMenuGroup>(
                GXSelectArgs.SelectAll<GXMenuGroup>(g => g.Name == groupName && g.Removed == null), cancellationToken);
            if (group != null)
            {
                await AddConfigurationMenu(connection, group);
            }
        }

        private static async Task AddNavMenu(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.NavMenu);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.NavMenu)
                {
                    Preload = true,
                    Title = "Navication Menu",
                    Description = "The Navigation menu contains links intended for site visitors.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                //NavMenu is shown only for login users.
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "/", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Home)), null, "Home");
            await AddLink(connection, menu, Visibility.All, "/device", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Devices)), null, "Device", [TargetType.DeviceLog, TargetType.DeviceTrace], "New Logs", GXDevicePolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Schedule", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Schedules)), null, "Schedule", [TargetType.ScheduleLog], "New Logs", GXSchedulePolicies.View);
            await AddLink(connection, menu, Visibility.All, "/DeviceTemplate", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTemplates)), null, "DeviceTemplate", null, "New Logs", GXDeviceTemplatePolicies.View);
            await AddLink(connection, menu, Visibility.All, "/User", ServerHelpers.GetInvariantString(nameof(Properties.Resources.UserManagement)), null, "User", [TargetType.UserError, TargetType.UserAction], "New Logs", GXUserPolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Agent", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agents)), null, "Agents", [TargetType.AgentLog], "New Logs", GXAgentPolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Gateway", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Gateway)), null, "Gateway", [TargetType.GatewayLog], "New Logs", GXGatewayPolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Logs", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logs)), null, "Log", null, null, GXLogPolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Extension", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Extensions)), null, "Extension", null, null, null);
            await AddLink(connection, menu, Visibility.All, "/Config", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Configuration)), null, "Configuration", null, null, GXConfigurationPolicies.View);
            await AddLink(connection, menu, Visibility.All, "/Favorites", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Favorites)), null, "Favorite", null, null, GXFavoritePolicies.View);
        }

        private static async Task AddHamburgerMenu(GXDbConnection? connection, GXMenuGroup def, GXUser? creator, DateTime now)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.HamburgerMenu);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.HamburgerMenu)
                {
                    Title = "Hamburger Menu",
                    Description = "The hamburger menu provides access to navigation links.",
                    CreationTime = now,
                    Creator = creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = now,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "/Login", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Login)), null, "Login");
            await AddLink(connection, menu, Visibility.All, "/Logout", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logout)), null, "Logout");
        }

        private static async Task AddDeviceTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.DeviceTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.DeviceTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Device tab links.",
                    Description = "The Device tab contains links to device-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "Device", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Devices)), null, TargetType.Device, [TargetType.Device], "New devices", GXDevicePolicies.View, typeof(Client.Pages.Device.Devices));
            await AddLink(connection, menu, Visibility.All, "DeviceGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceGroup)), null, TargetType.DeviceGroup, [TargetType.DeviceGroup], "New device group", GXDeviceGroupPolicies.View, typeof(Client.Pages.Device.DeviceGroups));
            await AddLink(connection, menu, Visibility.All, "DeviceLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceLogs)), null, TargetType.DeviceLog, [TargetType.DeviceLog], "New device logs", GXDeviceLogPolicies.View, typeof(Client.Pages.Device.DeviceLogs));
            await AddLink(connection, menu, Visibility.All, "DeviceAction", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceActions)), null, TargetType.DeviceAction, [TargetType.DeviceAction], "New device actions", GXDeviceActionPolicies.View, typeof(Client.Pages.Device.DeviceActions));
            await AddLink(connection, menu, Visibility.All, "DeviceTrace", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTraces)), null, TargetType.DeviceTrace, [TargetType.DeviceTrace], "New device traces", GXDeviceTracePolicies.View, typeof(Client.Pages.Device.DeviceTraces));
        }

        private static async Task AddDeviceTemplateTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.DeviceTemplateTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.DeviceTemplateTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Device template tab links.",
                    Description = "The Device template tab contains links to device template-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "DeviceTemplate", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTemplates)), null, TargetType.DeviceTemplate, [TargetType.DeviceTemplate], "New device templates", GXDeviceTemplatePolicies.View, typeof(Client.Pages.DeviceTemplate.DeviceTemplates));
            await AddLink(connection, menu, Visibility.All, "DeviceTemplateGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTemplateGroup)), null, TargetType.DeviceTemplateGroup, [TargetType.DeviceTemplateGroup], "New device template group", GXDeviceTemplateGroupPolicies.View, typeof(Client.Pages.DeviceTemplate.DeviceTemplateGroups));
        }

        private static async Task AddUserDetailsTabs(GXDbConnection? connection, GXMenuGroup mg)
        {
            var menu = connection == null ? null : await connection.SingleOrDefaultAsync<GXMenu>(GXSelectArgs.SelectAll<GXMenu>(m => m.Name == GXMenus.UserDetailsTab));
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.UserDetailsTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "User editor tabs.",
                    Creator = mg.Creator,
                    CreationTime = mg.CreationTime,
                    SystemDefined = true,
                    Preload = true
                };
                mg.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu menugroupMenu = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = mg.Id,
                    CreationTime = mg.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(menugroupMenu));
            }
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.User.UserGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "User", policy: GXUserPolicies.View, type: typeof(Client.Pages.User.UserGeneral));
            await AddLink(connection, menu, Visibility.All, "ProfilePicture", labels.GetString("ProfilePicture", System.Globalization.CultureInfo.InvariantCulture)!, icon: "User", policy: GXUserPolicies.View, type: typeof(Client.Helpers.ProfilePictureEditor));
            await AddLink(connection, menu, Visibility.All, "Roles", labels.GetString("Roles", System.Globalization.CultureInfo.InvariantCulture) ?? "Roles", icon: "User", policy: GXUserPolicies.View, type: typeof(Client.Pages.User.UserRoles));
            await AddLink(connection, menu, Visibility.All, "Permissions", labels.GetString("Permissions", System.Globalization.CultureInfo.InvariantCulture) ?? "Permissions", icon: "User", policy: GXUserPolicies.View, type: typeof(Client.Pages.User.UserPermissions));
            await AddLink(connection, menu, Visibility.All, "Groups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "UserGroups", policy: GXUserGroupPolicies.View, type: typeof(Client.Pages.User.UserGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "Notifications", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Notifications)), icon: "Notifications", policy: GXNotificationPolicies.View, type: typeof(Client.Pages.Notification.Notifications));
            await AddLink(connection, menu, Visibility.All, "PersonalAccessTokens", labels.GetString("PersonalAccessTokens", System.Globalization.CultureInfo.InvariantCulture)!, icon: "personal-access-token", policy: GXTokenPolicies.View, type: typeof(Client.Pages.User.UserPersonalAccessTokens));
        }

        private static async Task<GXMenu> AddManagementTabMenu(GXDbConnection? connection, GXMenuGroup group, string name)
        {
            var menu = connection == null ? null : await connection.SingleOrDefaultAsync<GXMenu>(
                GXSelectArgs.Select<GXMenu>(m => m.Id, m => m.Name == name));
            if (menu != null)
            {
                return menu;
            }
            menu = new GXMenu(name)
            {
                Kind = MenuKind.Tabs,
                Title = name + " links.",
                CreationTime = group.CreationTime,
                Creator = group.Creator,
                SystemDefined = true,
                Preload = true
            };
            group.Menus?.Add(menu);
            connection?.Insert(GXInsertArgs.Insert(menu));
            connection?.Insert(GXInsertArgs.Insert(new GXMenuGroupMenu
            {
                MenuId = menu.Id,
                MenuGroupId = group.Id,
                CreationTime = group.CreationTime
            }));
            return menu;
        }

        private static async Task AddContentTabMenu(GXDbConnection? connection, GXMenuGroup group, string name,
            params (string Path, string Label, string? Icon, string? Policy)[] tabs)
        {
            var menu = await AddManagementTabMenu(connection, group, name);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Device.DeviceGeneral).Assembly);
            foreach (var tab in tabs)
            {
                var label = labels.GetString(tab.Label, System.Globalization.CultureInfo.InvariantCulture)
                    ?? throw new InvalidOperationException("Missing tab label: " + tab.Label);
                await AddLink(connection, menu, Visibility.All, tab.Path, label, icon: tab.Icon, policy: tab.Policy);
            }
        }

        // These tabs render inside their owning editor to preserve its form state and component references.
        // Their labels, order, icons and policies use the same stored defaults as component-backed tabs.
        private static async Task AddContentTabMenus(GXDbConnection? connection, GXMenuGroup group)
        {
            await AddContentTabMenu(connection, group, GXMenus.AgentSettingsTab,
                ("Reader", "Reader", "reader", null),
                ("Pushevents", "PushEvents", "notification", null),
                ("RawNotifications", "RawNotifications", "notification", null),
                ("Autoconnect", "AutoConnect", "auto-connect", null),
                ("Gateway", "Gateway", "gateway", null));
            await AddContentTabMenu(connection, group, GXMenus.ThemeDetailsTab,
                ("Theme", "Theme", "theme", null),
                ("Color", "Color", "color", GXAppearancePolicies.View),
                ("Text", "Text", "text", GXAppearancePolicies.View));
            await AddContentTabMenu(connection, group, GXMenus.DeviceSelectorTab,
                ("Devices", "Devices", null, null), ("Objects", "Objects", null, null), ("Attributes", "Attributes", null, null));
            await AddContentTabMenu(connection, group, GXMenus.DeviceGroupSelectorTab,
                ("DeviceGroups", "DeviceGroups", null, null), ("Objects", "Objects", null, null), ("Attributes", "Attributes", null, null));
            await AddContentTabMenu(connection, group, GXMenus.DeviceTemplateSettingsTab,
                ("Profiles", "Profiles", "profiles", null), ("Ciphering", "Ciphering", "ciphering", null), ("Advanced", "Advanced", "advanced", null));
            await AddContentTabMenu(connection, group, GXMenus.ObjectTemplateDetailsTab,
                ("Defaultvalues", "DefaultValues", "object-view", null), ("Generic", "Generic", "generic", null));
            await AddContentTabMenu(connection, group, GXMenus.ObjectDetailsTab, ("General", "GeneralInformation", null, null));
            await AddContentTabMenu(connection, group, GXMenus.ChargeDetailsTab,
                ("General", "General", null, null), ("Unitchargeactive", "UnitChargeActive", null, null), ("Unitchargepassive", "UnitChargePassive", null, null));
            await AddContentTabMenu(connection, group, GXMenus.ConfigurationLanguageTab,
                ("Language", "Language", "language", null), ("Localize", "Localize", "localized-resource", null));
            await AddContentTabMenu(connection, group, GXMenus.AccessControlTab,
                ("Roles", "Roles", null, GXRolePolicies.View), ("Policies", "Policies", null, GXPolicyPolicies.View));
            await AddContentTabMenu(connection, group, GXMenus.LogTypeDetailsTab,
                ("GeneralInformation", "GeneralInformation", null, null), ("Policies", "Policies", null, null));
            await AddContentTabMenu(connection, group, GXMenus.NotificationTab,
                ("Types", "Notifications", null, null), ("ServerNotifications", "ServerNotifications", null, GXConfigurationPolicies.View));
            await AddContentTabMenu(connection, group, GXMenus.NotificationDetailsTab,
                ("GeneralInformation", "GeneralInformation", "notification", null), ("Notifications", "Notifications", "notification", null));
            await AddContentTabMenu(connection, group, GXMenus.PolicyTab,
                ("GeneralInformation", "GeneralInformation", null, null), ("Policies", "Policies", null, null));
        }

        private static async Task AddThemeEditorTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ThemeEditorTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Appearance.ThemeGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "theme", policy: GXAppearancePolicies.View, type: typeof(Client.Pages.Appearance.ThemeGeneral));
        }

        private static async Task AddTriggerDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.TriggerDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Trigger.TriggerGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "trigger", policy: GXTriggerPolicies.View, type: typeof(Client.Pages.Trigger.TriggerGeneral));
        }

        private static async Task AddAgentDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.AgentDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "agent", policy: GXAgentPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGeneral));
            await AddLink(connection, menu, Visibility.All, "AgentGroups", labels.GetString("AgentGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "agent-groups", policy: GXAgentGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGroupsSelector));
        }

        private static async Task AddBlockDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.BlockDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Block.BlockGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "block", policy: GXBlockPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Block.BlockGeneral));
            await AddLink(connection, menu, Visibility.All, "BlockGroups", labels.GetString("BlockGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "block-groups", policy: GXBlockGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Block.BlockGroupsSelector));
        }

        private static async Task AddComponentViewDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ComponentViewDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "component-view", policy: GXComponentViewPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewGeneral));
            await AddLink(connection, menu, Visibility.All, "ComponentViewGroups", labels.GetString("ComponentViewGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "component-view-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewGroupsSelector));
        }

        private static async Task AddDeviceDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.DeviceDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device", policy: GXDevicePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGeneral));
            await AddLink(connection, menu, Visibility.All, "ProfilePicture", labels.GetString("ProfilePicture", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device", policy: GXDevicePolicies.View, type: typeof(Client.Helpers.ProfilePictureEditor));
            await AddLink(connection, menu, Visibility.All, "DeviceGroups", labels.GetString("DeviceGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "Objects", labels.GetString("Objects", System.Globalization.CultureInfo.InvariantCulture)!, icon: "objects", policy: GXDevicePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Objects.Objects));
        }

        private static async Task AddDeviceTemplateDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.DeviceTemplateDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplateGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-template", policy: GXDeviceTemplatePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplateGeneral));
            await AddLink(connection, menu, Visibility.All, "ProfilePicture", labels.GetString("ProfilePicture", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-template", policy: GXDeviceTemplatePolicies.View, type: typeof(Client.Helpers.ProfilePictureEditor));
            await AddLink(connection, menu, Visibility.All, "DeviceTemplateGroups", labels.GetString("DeviceTemplateGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-template-groups-selector", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplateGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "ObjectTemplates", labels.GetString("ObjectTemplates", System.Globalization.CultureInfo.InvariantCulture)!, icon: "object-templates", policy: GXDeviceTemplatePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.ObjectTemplates));
        }

        private static async Task AddGatewayDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.GatewayDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewayGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "gateway", policy: GXGatewayPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewayGeneral));
            await AddLink(connection, menu, Visibility.All, "GatewayGroups", labels.GetString("GatewayGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "gateway-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewayGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "DeviceGroups", labels.GetString("DeviceGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-groups", policy: GXDeviceGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "Devices", labels.GetString("Devices", System.Globalization.CultureInfo.InvariantCulture)!, icon: "devices", policy: GXDevicePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DevicesSelector));
        }

        private static async Task AddKeyManagementDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.KeyManagementDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-management", policy: GXKeyManagementPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementGeneral));
            await AddLink(connection, menu, Visibility.All, "KeyManagementGroups", labels.GetString("KeyManagementGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-management-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementGroupsSelector));
        }

        private static async Task AddManufacturerDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ManufacturerDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Manufacturer.ManufacturerGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "manufacturer", policy: GXManufacturerPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Manufacturer.ManufacturerGeneral));
            await AddLink(connection, menu, Visibility.All, "ProfilePicture", labels.GetString("ProfilePicture", System.Globalization.CultureInfo.InvariantCulture)!, icon: "manufacturer", policy: GXManufacturerPolicies.View, type: typeof(Client.Helpers.ProfilePictureEditor));
            await AddLink(connection, menu, Visibility.All, "ModelProfilePicture", labels.GetString("DeviceModelProfilePicture", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device", policy: GXManufacturerPolicies.View, type: typeof(Client.Helpers.ProfilePictureEditor));
        }

        private static async Task AddMenuDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.MenuDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenuGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menu", policy: GXMenuPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenuGeneral));
            await AddLink(connection, menu, Visibility.All, "MenuGroups", labels.GetString("MenuGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menu-groups", policy: GXMenuGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenuGroupsSelector));
        }

        private static async Task AddModuleDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ModuleDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModuleGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module", policy: GXModulePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModuleGeneral));
            await AddLink(connection, menu, Visibility.All, "ModuleGroups", labels.GetString("ModuleGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module-groups", policy: GXModuleGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModuleGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "Policies", labels.GetString("Policies", System.Globalization.CultureInfo.InvariantCulture)!, icon: "policy", policy: GXModulePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModulePolicies));
        }

        private static async Task AddScheduleDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ScheduleDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "schedule", policy: GXSchedulePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGeneral));
            await AddLink(connection, menu, Visibility.All, "ScheduleGroups", labels.GetString("ScheduleGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "schedule-groups", policy: GXSchedulePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "Device", labels.GetString("DevicesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device", policy: GXDevicePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceSelector));
            await AddLink(connection, menu, Visibility.All, "DeviceGroup", labels.GetString("DeviceGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-groups", policy: GXDeviceGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupSelector));
            await AddLink(connection, menu, Visibility.All, "Modules", labels.GetString("ModulesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModulesSelector));
        }

        private static async Task AddScriptDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ScriptDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-view", policy: GXScriptPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptGeneral));
            await AddLink(connection, menu, Visibility.All, "ScriptGroups", labels.GetString("ScriptGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptGroupsSelector));
        }

        private static async Task AddViewDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ViewDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.View.ViewGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "view", policy: GXViewPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.View.ViewGeneral));
            await AddLink(connection, menu, Visibility.All, "ViewGroups", "View groups", icon: "view-groups", policy: GXViewGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.View.ViewGroupsSelector));
        }

        private static async Task AddWorkflowDetailsTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.WorkflowDetailsTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowGeneral).Assembly);
            var general = await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflow", policy: GXWorkflowPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowGeneral));
            if (connection != null)
            {
                // Repair the former built-in requirement as well as newly seeded menus.
                // Leave customized policy sets and non-system overrides untouched.
                var storedMenu = await connection.SingleOrDefaultAsync<GXMenu>(
                    GXSelectArgs.Select<GXMenu>(m => m.SystemDefined!, m => m.Id == menu.Id));
                var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXMenuLink), general.Id);
                if (storedMenu?.SystemDefined == true && policies.SequenceEqual([GXWorkflowGroupPolicies.View]))
                {
                    using var transaction = connection.BeginTransaction();
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), general.Id,
                        [GXWorkflowPolicies.View], transaction: transaction);
                    general.Updated = menu.Updated = DateTimeOffset.UtcNow;
                    general.ConcurrencyStamp = Guid.NewGuid().ToString();
                    menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(general, l => new { l.Updated, l.ConcurrencyStamp }));
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(menu, m => new { m.Updated, m.ConcurrencyStamp }));
                    transaction.Commit();
                }
            }
            await AddLink(connection, menu, Visibility.All, "WorkflowGroups", labels.GetString("WorkflowGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflow-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "ScriptMethods", labels.GetString("ScriptMethods", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-methods", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptMethodsSelector));
        }

        private static async Task AddWorkflowGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.WorkflowGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflow-group", policy: GXWorkflowGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Workflows", labels.GetString("Workflows", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflows", policy: GXWorkflowPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Workflow.WorkflowsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddGatewayGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.GatewayGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewayGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "gateway-group", policy: GXGatewayGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewayGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Gateways", labels.GetString("Gateways", System.Globalization.CultureInfo.InvariantCulture)!, icon: "gateways", policy: GXGatewayPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Gateway.GatewaysSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddModuleGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ModuleGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModuleGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module-group", policy: GXModuleGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModuleGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Modules", labels.GetString("ModulesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "modules", policy: GXModulePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Module.ModulesSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddMenuGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.MenuGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenuGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menu-group", policy: GXMenuGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenuGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Menus", labels.GetString("Menus", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menus", policy: GXUserPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Menu.MenusSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddDeviceTemplateGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.DeviceTemplateGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplateGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-template-group", policy: GXDeviceTemplateGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplateGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "DeviceTemplates", labels.GetString("DeviceTemplates", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-templates", policy: GXDeviceTemplatePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.DeviceTemplate.DeviceTemplatesSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddViewGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ViewGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.View.ViewGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "view-group", policy: GXViewGroupPolicies.View, type: typeof(Client.Pages.View.ViewGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Views", "Views", icon: "views", policy: GXViewPolicies.View, type: typeof(Client.Pages.View.ViewsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddUserGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.UserGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-group", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Users", labels.GetString("UsersTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "users", policy: GXUserPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UsersSelector));
            await AddLink(connection, menu, Visibility.All, "DeviceGroups", labels.GetString("DeviceGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "ScheduleGroups", labels.GetString("ScheduleGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "schedule-groups", policy: GXScheduleGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGroupsSelector));
        }

        private static async Task AddDeviceGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.DeviceGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-group", policy: GXDeviceGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Devices", labels.GetString("DevicesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "devices", policy: GXDevicePolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DevicesSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "AgentGroups", labels.GetString("AgentGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "agent-groups", policy: GXAgentGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGroupsSelector));
        }

        private static async Task AddTriggerGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.TriggerGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Trigger.TriggerGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "trigger-group", policy: GXTriggerGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Trigger.TriggerGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Triggers", labels.GetString("Triggers", System.Globalization.CultureInfo.InvariantCulture)!, icon: "triggers", policy: GXUserPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Trigger.TriggersSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddScriptGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ScriptGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-group", policy: GXScriptGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Scripts", labels.GetString("Scripts", System.Globalization.CultureInfo.InvariantCulture)!, icon: "scripts", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddKeyManagementGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.KeyManagementGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-management-group", policy: GXKeyManagementGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "KeyManagements", labels.GetString("KeyManagements", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-managements", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.KeyManagement.KeyManagementsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddScheduleGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ScheduleGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "schedule-group", policy: GXScheduleGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.ScheduleGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Schedules", labels.GetString("SchedulesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "schedules", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Schedule.SchedulesSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddComponentViewGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ComponentViewGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "component-view-group", policy: GXComponentViewGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "ComponentViews", labels.GetString("ComponentViews", System.Globalization.CultureInfo.InvariantCulture)!, icon: "component-views", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.ComponentView.ComponentViewsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddAgentGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.AgentGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "agent-group", policy: GXAgentGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Agents", labels.GetString("Agents", System.Globalization.CultureInfo.InvariantCulture)!, icon: "agents", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Agent.AgentsSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.User.UserGroupsSelector));
            await AddLink(connection, menu, Visibility.All, "DeviceGroups", labels.GetString("DeviceGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "device-groups", policy: GXUserGroupPolicies.View, type: typeof(Gurux.DLMS.AMI.Client.Pages.Device.DeviceGroupsSelector));
        }

        private static async Task AddBlockGroupTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.BlockGroupTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Block.BlockGroupGeneral).Assembly);
            await AddLink(connection, menu, Visibility.All, "General", labels.GetString("GeneralInformation", System.Globalization.CultureInfo.InvariantCulture)!, icon: "block", policy: GXBlockGroupPolicies.View, type: typeof(Client.Pages.Block.BlockGroupGeneral));
            await AddLink(connection, menu, Visibility.All, "Blocks", labels.GetString("Blocks", System.Globalization.CultureInfo.InvariantCulture)!, icon: "blocks", policy: GXUserGroupPolicies.View, type: typeof(Client.Pages.Block.BlocksSelector));
            await AddLink(connection, menu, Visibility.All, "UserGroups", labels.GetString("UserGroupsTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "user-groups", policy: GXUserGroupPolicies.View, type: typeof(Client.Pages.User.UserGroupsSelector));
        }

        private static async Task AddBlockTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.BlockTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Block.Blocks).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Block", labels.GetString("Blocks", System.Globalization.CultureInfo.InvariantCulture)!, icon: "blocks", policy: GXBlockPolicies.View, type: typeof(Client.Pages.Block.Blocks));
            await AddLink(connection, menu, Visibility.All, "config/BlockGroup", labels.GetString("BlockGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "block-groups", policy: GXBlockGroupPolicies.View, type: typeof(Client.Pages.Block.BlockGroups));
        }

        private static async Task AddMenuTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.MenuTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Menu.Menus).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Menu", labels.GetString("Menus", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menus", policy: GXMenuPolicies.View, type: typeof(Client.Pages.Menu.Menus));
            await AddLink(connection, menu, Visibility.All, "config/MenuGroup", labels.GetString("MenuGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "menu-groups", policy: GXMenuGroupPolicies.View, type: typeof(Client.Pages.Menu.MenuGroups));
        }

        private static async Task AddViewTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ViewTab);
            await AddLink(connection, menu, Visibility.All, "config/View", "Views", icon: "views", policy: GXViewPolicies.View, type: typeof(Client.Pages.View.Views));
            await AddLink(connection, menu, Visibility.All, "config/ViewGroup", "View groups", icon: "view-groups", policy: GXViewGroupPolicies.View, type: typeof(Client.Pages.View.ViewGroups));
        }

        private static async Task AddWorkflowTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.WorkflowTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Workflow.Workflows).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Workflow", labels.GetString("Workflows", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflows", policy: GXWorkflowPolicies.View, type: typeof(Client.Pages.Workflow.Workflows));
            await AddLink(connection, menu, Visibility.All, "config/WorkflowGroup", labels.GetString("WorkflowGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflow-groups", policy: GXWorkflowGroupPolicies.View, type: typeof(Client.Pages.Workflow.WorkflowGroups));
            await AddLink(connection, menu, Visibility.All, "config/WorkflowLog", labels.GetString("WorkflowLogs", System.Globalization.CultureInfo.InvariantCulture)!, icon: "workflow-logs", policy: GXWorkflowLogPolicies.View, type: typeof(Client.Pages.Workflow.WorkflowLogs));
        }

        private static async Task AddScriptTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ScriptTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Script.Scripts).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Script", labels.GetString("Scripts", System.Globalization.CultureInfo.InvariantCulture)!, icon: "scripts", policy: GXScriptPolicies.View, type: typeof(Client.Pages.Script.Scripts));
            await AddLink(connection, menu, Visibility.All, "config/ScriptGroup", labels.GetString("ScriptGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-groups", policy: GXScriptGroupPolicies.View, type: typeof(Client.Pages.Script.ScriptGroups));
            await AddLink(connection, menu, Visibility.All, "config/ScriptLog", labels.GetString("ScriptLogs", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script-logs", policy: GXScriptLogPolicies.View, type: typeof(Client.Pages.Script.ScriptLogs));
            await AddLink(connection, menu, Visibility.All, "Templates", labels.GetString("Templates", System.Globalization.CultureInfo.InvariantCulture)!, icon: "script", policy: GXUpdatePolicies.Admin, type: typeof(Gurux.DLMS.AMI.Client.Pages.Script.ScriptTemplates));
        }

        private static async Task AddTriggerTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.TriggerTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Trigger.Triggers).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Trigger", labels.GetString("Triggers", System.Globalization.CultureInfo.InvariantCulture)!, icon: "triggers", policy: GXTriggerPolicies.View, type: typeof(Client.Pages.Trigger.Triggers));
        }

        private static async Task AddComponentViewTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ComponentViewTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.ComponentView.ComponentViews).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Component", labels.GetString("ComponentViews", System.Globalization.CultureInfo.InvariantCulture)!, icon: "component-views", policy: GXComponentViewPolicies.View, type: typeof(Client.Pages.ComponentView.ComponentViews));
        }

        private static async Task AddKeyManagementTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.KeyManagementTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.KeyManagement.KeyManagements).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/KeyManagement", labels.GetString("KeyManagements", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-managements", policy: GXKeyManagementPolicies.View, type: typeof(Client.Pages.KeyManagement.KeyManagements));
            await AddLink(connection, menu, Visibility.All, "config/KeyManagementGroup", labels.GetString("KeyManagementGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-management-groups", policy: GXKeyManagementPolicies.View, type: typeof(Client.Pages.KeyManagement.KeyManagementGroups));
            await AddLink(connection, menu, Visibility.All, "config/KeyManagementLog", labels.GetString("KeyManagementLogs", System.Globalization.CultureInfo.InvariantCulture)!, icon: "key-management-logs", policy: GXKeyManagementLogPolicies.View, type: typeof(Client.Pages.KeyManagement.KeyManagementLogs));
        }

        private static async Task AddAppearanceTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.AppearanceTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Appearance.Images).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/appearance/Image", labels.GetString("Icons", System.Globalization.CultureInfo.InvariantCulture)!, icon: "images", policy: GXAppearancePolicies.View, type: typeof(Client.Pages.Appearance.Images));
            await AddLink(connection, menu, Visibility.All, "config/appearance/theme", labels.GetString("Theme", System.Globalization.CultureInfo.InvariantCulture)!, icon: "theme", policy: GXAppearancePolicies.View, type: typeof(Client.Pages.Appearance.Theme));
            await AddLink(connection, menu, Visibility.All, "config/appearance/iconpack", labels.GetString("IconPack", System.Globalization.CultureInfo.InvariantCulture)!, icon: "icon-pack", policy: GXAppearancePolicies.View, type: typeof(Client.Pages.Appearance.IconPack));
        }

        private static async Task AddManufacturerTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ManufacturerTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Manufacturer.Manufacturers).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Manufacturer", labels.GetString("Manufacturers", System.Globalization.CultureInfo.InvariantCulture)!, icon: "manufacturers", policy: GXManufacturerPolicies.View, type: typeof(Client.Pages.Manufacturer.Manufacturers));
        }

        private static async Task AddModuleTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ModuleTab);
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Module.Modules).Assembly);
            await AddLink(connection, menu, Visibility.All, "config/Module", labels.GetString("ModulesTitle", System.Globalization.CultureInfo.InvariantCulture)!, icon: "modules", policy: GXModulePolicies.View, type: typeof(Client.Pages.Module.Modules));
            await AddLink(connection, menu, Visibility.All, "config/ModuleGroup", labels.GetString("ModuleGroups", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module-groups", policy: GXModuleGroupPolicies.View, type: typeof(Client.Pages.Module.ModuleGroups));
            await AddLink(connection, menu, Visibility.All, "config/ModuleLog", labels.GetString("ModuleLogs", System.Globalization.CultureInfo.InvariantCulture)!, icon: "module-logs", policy: GXModulePolicies.Edit, type: typeof(Client.Pages.Module.ModuleLogs));
        }

        private static async Task AddConfigurationTabs(GXDbConnection? connection, GXMenuGroup group)
        {
            var menu = await AddManagementTabMenu(connection, group, GXMenus.ConfigurationTab);
            await AddLink(connection, menu, Visibility.All, "config", "Settings", icon: "configuration",
                policy: GXConfigurationPolicies.View, type: typeof(Client.Pages.Config.ConfigurationList));
            await AddLink(connection, menu, Visibility.All, "config/settings", "Configuration layout", icon: "edit",
                policy: GXMenuPolicies.Edit, type: typeof(Client.Pages.Config.ConfigurationMenuSettings));
        }

        private static async Task AddUpdateTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            var menu = connection == null ? null : await connection.SingleOrDefaultAsync<GXMenu>(GXSelectArgs.Select<GXMenu>(m => m.Id, m => m.Name == GXMenus.UpdateTab));
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.UpdateTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Update tab links.",
                    Description = "Available updates and update notification settings.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                connection?.Insert(GXInsertArgs.Insert(new GXMenuGroupMenu
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime
                }));
            }
            var labels = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Admin.Updates).Assembly);
            await AddLink(connection, menu, Visibility.All, "updates", labels.GetString("AvailableUpdates", System.Globalization.CultureInfo.InvariantCulture)!, icon: "updates", policy: GXUpdatePolicies.Admin, type: typeof(Client.Pages.Admin.Updates));
            await AddLink(connection, menu, Visibility.All, "updates/settings", labels.GetString("UpdateSettings", System.Globalization.CultureInfo.InvariantCulture)!, icon: "settings", policy: GXUpdatePolicies.Admin, type: typeof(Client.Pages.Admin.UpdateSettingsPage));
        }

        private static async Task AddUserTabs(GXDbConnection? connection, GXMenuGroup mg)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.UserTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.UserTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "User tab links.",
                    Description = "The User tab contains links to user-related views and actions.",
                    CreationTime = mg.CreationTime,
                    Creator = mg.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                mg.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu menugroupMenu = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = mg.Id,
                    CreationTime = mg.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(menugroupMenu));
            }
            await AddLink(connection, menu, Visibility.All, "User", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Users)), null, TargetType.User, [TargetType.User], "New users", GXUserPolicies.View, typeof(Client.Pages.User.Users));
            await AddLink(connection, menu, Visibility.All, "UserGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.UserGroups)), null, TargetType.UserGroup, [TargetType.UserGroup], "New user group", GXUserGroupPolicies.View, typeof(Client.Pages.User.UserGroups));
            await AddLink(connection, menu, Visibility.All, "UserError", ServerHelpers.GetInvariantString(nameof(Properties.Resources.UserErrors)), null, TargetType.UserError, [TargetType.UserError], "New user errors", GXUserErrorPolicies.View, typeof(Client.Pages.User.UserErrors));
            await AddLink(connection, menu, Visibility.All, "UserAction", ServerHelpers.GetInvariantString(nameof(Properties.Resources.UserActions)), null, TargetType.UserAction, [TargetType.UserAction], "New user actions", GXUserActionPolicies.View, typeof(Client.Pages.User.UserActions));
        }


        private static async Task AddScheduleTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.ScheduleTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.ScheduleTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Schedule tab links.",
                    Description = "The Schedule tab contains links to schedule-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "Schedule", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Schedules)), null, TargetType.Schedule, [TargetType.Schedule], "New schedules", GXSchedulePolicies.View, typeof(Client.Pages.Schedule.Schedules));
            await AddLink(connection, menu, Visibility.All, "ScheduleGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScheduleGroups)), null, TargetType.ScheduleGroup, [TargetType.ScheduleGroup], "New schedule group", GXScheduleGroupPolicies.View, typeof(Client.Pages.Schedule.ScheduleGroups));
            await AddLink(connection, menu, Visibility.All, "ScheduleLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScheduleLogs)), null, TargetType.ScheduleLog, [TargetType.ScheduleLog], "New schedule logs", GXScheduleLogPolicies.View, typeof(Client.Pages.Schedule.ScheduleLogs));
        }

        private static async Task AddGatewayTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.GatewayTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.GatewayTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Gateway tab links.",
                    Description = "The Gateway tab contains links to gateway-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "Gateway", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Gateway)), null, TargetType.Gateway, [TargetType.Gateway], "New gateways", GXGatewayPolicies.View, typeof(Client.Pages.Gateway.Gateways));
            await AddLink(connection, menu, Visibility.All, "GatewayGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.GatewayGroup)), null, TargetType.GatewayGroup, [TargetType.GatewayGroup], "New gateway group", GXGatewayGroupPolicies.View, typeof(Client.Pages.Gateway.GatewayGroups));
            await AddLink(connection, menu, Visibility.All, "GatewayLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.GatewayLogs)), null, TargetType.GatewayLog, [TargetType.GatewayLog], "New gateway logs", GXGatewayLogPolicies.View, typeof(Client.Pages.Gateway.GatewayLogs));
        }

        private static async Task AddAgentTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.AgentTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.AgentTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Agent tab links.",
                    Description = "The Agent tab contains links to agent-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLink(connection, menu, Visibility.All, "Agent", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agents)), null, TargetType.Agent, [TargetType.Agent], "New agents", GXAgentPolicies.View, typeof(Client.Pages.Agent.Agents));
            await AddLink(connection, menu, Visibility.All, "AgentGroup", ServerHelpers.GetInvariantString(nameof(Properties.Resources.AgentGroups)), null, TargetType.AgentGroup, [TargetType.AgentGroup], "New agent group", GXAgentGroupPolicies.View, typeof(Client.Pages.Agent.AgentGroups));
            await AddLink(connection, menu, Visibility.All, "AgentLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.AgentLogs)), null, TargetType.AgentLog, [TargetType.AgentLog], "New agent logs", GXAgentLogPolicies.View, typeof(Client.Pages.Agent.AgentLogs));
        }

        internal static async Task AddLogLinksAsync(GXDbConnection? connection, GXMenu menu, CancellationToken cancellationToken = default)
        {
            menu.Links = connection == null ? [] : await connection.SelectAsync<GXMenuLink>(
                GXSelectArgs.SelectAll<GXMenuLink>(link => link.Menu == menu && link.Removed == null), cancellationToken);
            int before = menu.Links.Count;
            var obsoletePerformances = menu.Links.Where(l => string.Equals(l.Url, "Logs/Performances", StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var link in obsoletePerformances)
            {
                if (connection != null)
                {
                    link.Removed = DateTimeOffset.UtcNow;
                    await connection.UpdateAsync(GXUpdateArgs.Update(link, l => l.Removed!));
                }
                menu.Links.Remove(link);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await AddLink(connection, menu, Visibility.All, "Logs/SystemLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemLogs)), null, TargetType.SystemLog, [TargetType.SystemLog], "New logs", GXSystemLogPolicies.View, typeof(Client.Pages.Admin.SystemLog));
            await AddLink(connection, menu, Visibility.All, "Logs/Task", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Tasks)), null, TargetType.Task, [TargetType.Task], "New Tasks", GXTaskPolicies.View, typeof(Client.Pages.Admin.Tasks));
            await AddLink(connection, menu, Visibility.All, "Logs/ScheduleLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScheduleLogs)), null, TargetType.ScheduleLog, [TargetType.ScheduleLog], "New logs", GXScheduleLogPolicies.View, typeof(Client.Pages.Schedule.ScheduleLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/ModuleLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ModuleLogs)), null, TargetType.ModuleLog, [TargetType.ModuleLog], "New logs", GXModuleLogPolicies.View, typeof(Client.Pages.Module.ModuleLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/ScriptLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScriptLog)), null, TargetType.ScriptLog, [TargetType.ScriptLog], "New logs", GXScriptLogPolicies.View, typeof(Client.Pages.Script.ScriptLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/WorkflowLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.WorkflowLog)), null, TargetType.WorkflowLog, [TargetType.WorkflowLog], "New logs", GXWorkflowLogPolicies.View, typeof(Client.Pages.Workflow.WorkflowLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/general", ServerHelpers.GetInvariantString(nameof(Properties.Resources.General)), null, TargetType.Log, [TargetType.Log], "New logs", GXLogPolicies.View, typeof(Client.Pages.Log.Logs));

            await AddLink(connection, menu, Visibility.All, "Logs/KeyManagementLog", "Key management logs", null, TargetType.KeyManagementLog, [TargetType.KeyManagementLog], "New logs", GXKeyManagementLogPolicies.View, typeof(Client.Pages.KeyManagement.KeyManagementLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/GatewayLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.GatewayLogs)), null, TargetType.GatewayLog, [TargetType.GatewayLog], "New logs", GXGatewayLogPolicies.View, typeof(Client.Pages.Gateway.GatewayLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/AgentLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.AgentLogs)), null, TargetType.AgentLog, [TargetType.AgentLog], "New logs", GXAgentLogPolicies.View, typeof(Client.Pages.Agent.AgentLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/DeviceLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceLogs)), null, TargetType.DeviceLog, [TargetType.DeviceLog], "New logs", GXDeviceLogPolicies.View, typeof(Client.Pages.Device.DeviceLogs));
            await AddLink(connection, menu, Visibility.All, "Logs/DeviceAction", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceActions)), null, TargetType.DeviceAction, [TargetType.DeviceAction], "New logs", GXDeviceActionPolicies.View, typeof(Client.Pages.Device.DeviceActions));
            await AddLink(connection, menu, Visibility.All, "Logs/DeviceTrace", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTraces)), null, TargetType.DeviceTrace, [TargetType.DeviceTrace], "New logs", GXDeviceTracePolicies.View, typeof(Client.Pages.Device.DeviceTraces));
            await AddLink(connection, menu, Visibility.All, "Logs/changehistory", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ChangeHistory)), null, TargetType.ChangeHistory, [TargetType.ChangeHistory], "New change history", GXChangeHistoryPolicies.View, typeof(Client.Pages.ChangeHistory));
            await AddLink(connection, menu, Visibility.All, "updates", "Available updates", icon: "updates", policy: GXUpdatePolicies.Admin, type: typeof(Client.Pages.Admin.UpdatePages));
            if (connection != null && menu.Links.Count != before)
            {
                menu.Updated = DateTimeOffset.UtcNow;
                menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.UpdateAsync(GXUpdateArgs.Update(menu, m => new { m.Updated, m.ConcurrencyStamp }), cancellationToken);
            }
        }
        private static async Task AddLogTabs(GXDbConnection? connection, GXMenuGroup def)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.LogTab);
            GXMenu? menu = connection?.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.LogTab)
                {
                    Kind = MenuKind.Tabs,
                    Title = "Log tab links.",
                    Description = "The Log tab contains links to log-related views and actions.",
                    CreationTime = def.CreationTime,
                    Creator = def.Creator,
                    SystemDefined = true,
                    Preload = true
                };
                //LogTab is shown only for admin or device users.
                def.Menus?.Add(menu);
                connection?.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = def.CreationTime,
                };
                connection?.Insert(GXInsertArgs.Insert(mg));
            }
            await AddLogLinksAsync(connection, menu);
        }

        /// <summary>
        /// Adds the default set of menus to the specified host.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="adminGroup">The admin group.</param>
        internal static async Task AddDefaultMenus(GXDbConnection? connection,
            GXMenuGroup adminGroup)
        {
            DateTime now = DateTime.Now;
            var adminUser = connection == null ? null : await ServerSettings.GetDefaultAdminUserAsync(connection);
            var creator = connection == null ? adminGroup.Creator : new GXUser() { Id = ClientHelpers.GetUserId(adminUser) };
            string name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Default));
            GXSelectArgs arg = GXSelectArgs.Select<GXMenuGroup>(s => s.Id, w => w.Name == name);
            //Add log tab links first because nav menu is using it.
            await AddUserTabs(connection, adminGroup);
            await AddUserDetailsTabs(connection, adminGroup);
            await AddContentTabMenus(connection, adminGroup);
            await AddThemeEditorTabs(connection, adminGroup);
            await AddAgentDetailsTabs(connection, adminGroup);
            await AddTriggerDetailsTabs(connection, adminGroup);
            await AddBlockDetailsTabs(connection, adminGroup);
            await AddComponentViewDetailsTabs(connection, adminGroup);
            await AddDeviceDetailsTabs(connection, adminGroup);
            await AddDeviceTemplateDetailsTabs(connection, adminGroup);
            await AddGatewayDetailsTabs(connection, adminGroup);
            await AddKeyManagementDetailsTabs(connection, adminGroup);
            await AddManufacturerDetailsTabs(connection, adminGroup);
            await AddMenuDetailsTabs(connection, adminGroup);
            await AddModuleDetailsTabs(connection, adminGroup);
            await AddScheduleDetailsTabs(connection, adminGroup);
            await AddScriptDetailsTabs(connection, adminGroup);
            await AddViewDetailsTabs(connection, adminGroup);
            await AddWorkflowDetailsTabs(connection, adminGroup);
            await AddUpdateTabs(connection, adminGroup);
            await AddConfigurationTabs(connection, adminGroup);
            await AddBlockTabs(connection, adminGroup);
            await AddBlockGroupTabs(connection, adminGroup);
            await AddWorkflowGroupTabs(connection, adminGroup);
            await AddGatewayGroupTabs(connection, adminGroup);
            await AddModuleGroupTabs(connection, adminGroup);
            await AddMenuGroupTabs(connection, adminGroup);
            await AddViewGroupTabs(connection, adminGroup);
            await AddDeviceTemplateGroupTabs(connection, adminGroup);
            await AddUserGroupTabs(connection, adminGroup);
            await AddDeviceGroupTabs(connection, adminGroup);
            await AddTriggerGroupTabs(connection, adminGroup);
            await AddScriptGroupTabs(connection, adminGroup);
            await AddKeyManagementGroupTabs(connection, adminGroup);
            await AddScheduleGroupTabs(connection, adminGroup);
            await AddComponentViewGroupTabs(connection, adminGroup);
            await AddAgentGroupTabs(connection, adminGroup);
            await AddMenuTabs(connection, adminGroup);
            await AddViewTabs(connection, adminGroup);
            await AddWorkflowTabs(connection, adminGroup);
            await AddScriptTabs(connection, adminGroup);
            await AddTriggerTabs(connection, adminGroup);
            await AddComponentViewTabs(connection, adminGroup);
            await AddKeyManagementTabs(connection, adminGroup);
            await AddAppearanceTabs(connection, adminGroup);
            await AddManufacturerTabs(connection, adminGroup);
            await AddModuleTabs(connection, adminGroup);
            await AddDeviceTemplateTabs(connection, adminGroup);
            await AddDeviceTabs(connection, adminGroup);
            await AddScheduleTabs(connection, adminGroup);
            await AddAgentTabs(connection, adminGroup);
            await AddGatewayTabs(connection, adminGroup);
            await AddLogTabs(connection, adminGroup);
            await AddNavMenu(connection, adminGroup);
            await AddConfigurationMenu(connection, adminGroup);
            await AddHamburgerMenu(connection, adminGroup, creator, now);
        }

        /// <inheritdoc />
        public async Task EnsureDefaultsAsync(MenuKind kind, CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAllAsync<GXMenu>(_dbFactory, User, _serviceProvider,
        async () =>
        {
            await ApplyDefaultsAsync(kind, [], false, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ResetToDefaultsAsync(MenuKind kind, IEnumerable<Guid> menus, CancellationToken cancellationToken = default)
        {
            menus = menus.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXMenu, Guid>(_dbFactory, User, _serviceProvider, menus,
        async () =>
        {
            var ids = menus?.Distinct().ToArray() ?? [];
            if (ids.Length == 0)
            {
                throw new ArgumentException("Select at least one menu to reset.", nameof(menus));
            }

            await ApplyDefaultsAsync(kind, ids, true, cancellationToken);
        }, cancellationToken);
        }

        private async Task ApplyDefaultsAsync(MenuKind kind, Guid[] ids, bool reset, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXMenuPolicies.Edit);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var changed = await ApplyDefaultMenusAsync(connection, kind, ids, reset,
                new GXUser { Id = ServerHelpers.GetUserId(User) }, cancellationToken);
            foreach (var id in changed)
            {
                _notificationCache.Add(await GetNotifiedUsers(id, NotificationAction.Edit, connection), new GXTarget(GXMenuPolicies.Edit, id));
            }
        }

        /// <summary>Resets menus from the same default definitions used at initialization.</summary>
        internal static async Task<Guid[]> ApplyDefaultMenusAsync(GXDbConnection connection, MenuKind kind, Guid[] ids, bool reset,
            GXUser creator, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (reset && ids.Length == 0)
            {
                throw new ArgumentException("Select at least one menu to reset.", nameof(ids));
            }

            var group = new GXMenuGroup(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Users)))
            {
                Creator = creator,
                CreationTime = DateTimeOffset.UtcNow
            };
            await AddDefaultMenus(null, group);
            var defaults = group.Menus!.ToDictionary(m => m.Name!, StringComparer.Ordinal);
            var selected = ids.Length == 0 ? [] : await connection.SelectAsync<GXMenu>(
                GXSelectArgs.SelectAll<GXMenu>(m => ids.Contains(m.Id)), cancellationToken);
            if (ids.Length != 0 && (selected.Count != ids.Length || selected.Any(m => !defaults.ContainsKey(m.Name!) || (kind != MenuKind.All && m.Kind != kind))))
            {
                throw new ArgumentException("Every selected menu must exist, match the requested kind and have a built-in default.");
            }
            var configurations = await connection.SelectAsync<GXConfiguration>(
                GXSelectArgs.SelectAll<GXConfiguration>(), cancellationToken);
            var existingMenus = await connection.SelectAsync<GXMenu>(GXSelectArgs.SelectAll<GXMenu>(), cancellationToken);
            var targets = !reset ? defaults.Values.Where(m => (kind == MenuKind.All || m.Kind == kind)
                && !existingMenus.Any(existing => existing.Name == m.Name)).ToList() : [.. selected.Select(m =>
            {
                var value = defaults[m.Name!];
                value.Id = m.Id;
                value.Creator = m.Creator;
                value.CreationTime = m.CreationTime;
                return value;
            })];
            foreach (var menu in targets)
            {
                if (menu.Kind != MenuKind.Configuration)
                {
                    continue;
                }
                menu.Links = [.. menu.Links!.Where(l =>
                {
                    var configuration = configurations.FirstOrDefault(c => c.Enabled && c.Name == l.Configuration?.Name);
                    l.Configuration = configuration == null ? null : new GXConfiguration { Id = configuration.Id };
                    return configuration != null;
                })];
            }
            var existingGroup = ids.Length == 0 ? await connection.SingleOrDefaultAsync<GXMenuGroup>(
                GXSelectArgs.SelectAll<GXMenuGroup>(g => g.Name == group.Name && g.Removed == null), cancellationToken) : null;
            if (existingGroup != null)
            {
                group.Id = existingGroup.Id;
            }
            using var transaction = connection.BeginTransaction();
            if (ids.Length == 0)
            {
                if (existingGroup == null && targets.Count != 0)
                {
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(group), cancellationToken);
                }
            }
            foreach (var menu in targets)
            {
                if (ids.Length == 0)
                {
                    var insert = GXInsertArgs.Insert(menu);
                    insert.Exclude<GXMenu>(m => new { m.Links, m.MenuGroups });
                    await connection.InsertAsync(transaction, insert, cancellationToken);
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXMenuGroupMenu { MenuId = menu.Id, MenuGroupId = group.Id, CreationTime = group.CreationTime }), cancellationToken);
                }
                else
                {
                    var oldLinks = await connection.SelectAsync<GXMenuLink>(transaction, GXSelectArgs.Select<GXMenuLink>(l => l.Id, l => l.Menu == menu), cancellationToken);
                    var oldLinkIds = oldLinks.Select(l => l.Id).ToArray();
                    if (oldLinkIds.Length != 0)
                    {
                        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXMenuLinkPolicy>(p => oldLinkIds.Contains(p.MenuLinkId)), cancellationToken);
                    }

                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXMenuLink>(l => l.Menu == menu), cancellationToken);
                    menu.Updated = DateTimeOffset.UtcNow;
                    menu.ConcurrencyStamp = Guid.NewGuid().ToString();
                    var update = GXUpdateArgs.Update(menu);
                    update.Exclude<GXMenu>(m => new { m.Links, m.MenuGroups, m.CreationTime, m.Creator });
                    await connection.UpdateAsync(transaction, update, cancellationToken);
                }
                await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenu), menu.Id, menu.Policies, cancellationToken, transaction);
                foreach (var link in menu.Links ?? [])
                {
                    link.Id = Guid.Empty;
                    link.Menu = new GXMenu { Id = menu.Id };
                    link.CreationTime = DateTimeOffset.UtcNow;
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(link), cancellationToken);
                    await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), link.Id, link.Policies, cancellationToken, transaction);
                }
            }
            // Built-in menus belong to the Users group. Older installations created
            // this group without memberships, leaving ordinary users with no navigation.
            // Preserve configured memberships, including explicitly removed mappings.
            if (ids.Length == 0 && (existingGroup != null || targets.Count != 0))
            {
                var memberships = await connection.SelectAsync<GXUserGroupMenuGroup>(transaction,
                    GXSelectArgs.SelectAll<GXUserGroupMenuGroup>(m => m.MenuGroupId == group.Id), cancellationToken);
                if (memberships.Count == 0)
                {
                    var userGroups = await connection.SelectAsync<GXUserGroup>(transaction,
                        GXSelectArgs.Select<GXUserGroup>(g => g.Id, g => g.Default == true && g.Removed == null), cancellationToken);
                    foreach (var userGroup in userGroups)
                    {
                        await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXUserGroupMenuGroup
                        {
                            MenuGroupId = group.Id,
                            UserGroupId = userGroup.Id,
                            CreationTime = DateTimeOffset.UtcNow
                        }), cancellationToken);
                    }
                }
            }
            transaction.Commit();
            return [.. targets.Select(m => m.Id)];
        }

        // Lifecycle operations share this repository's persistence rules and notification queue.
        internal static IModuleMenuRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleMenuRepository
        {
            private string LogPath => $"module/{scope.ModuleId}/Logs/{scope.ModuleId}";

            public async Task EnsureLogLinkAsync(AmiModuleCapture definition, GXPolicy policy, CancellationToken token)
            {
                if (string.IsNullOrWhiteSpace(definition.TargetType))
                {
                    throw new ArgumentException("A module log view is required.");
                }
                var ownedId = await scope.Settings.ReadLogMenuLinkIdAsync(token);
                var link = ownedId.HasValue ? await FindLogLinkAsync(ownedId.Value, token) : null;
                if (link != null)
                {
                    ValidateOwnership(link);
                    return;
                }
                var menu = await scope.Connection.SingleOrDefaultAsync<GXMenu>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXMenu>(m => m.Name == "LogTab" && m.Removed == null), token)
                    ?? throw new InvalidOperationException("The LogTab menu must exist before installing capture defaults.");
                link = new GXMenuLink(scope.ModuleId)
                {
                    Id = Guid.NewGuid(),
                    Menu = menu,
                    Url = LogPath,
                    MenuLinkType = MenuLinkType.Content,
                    TargetType = definition.TargetType,
                    Icon = definition.Icon,
                    Order = 100,
                    CreationTime = DateTimeOffset.UtcNow,
                    SystemDefined = false
                };
                Validator.ValidateObject(link, new ValidationContext(link), true);
                await scope.Connection.InsertAsync(scope.Transaction, GXInsertArgs.Insert(link), token);
                await GXResourcePolicyStorage.SaveAsync(scope.Connection, typeof(GXMenuLink), link.Id, [policy.Name!], token, scope.Transaction);
                await scope.Settings.SetLogMenuLinkIdAsync(link.Id, token);
                await GXModuleRepositoryNotifications.QueueAsync(scope, GXMenuPolicies.Edit, menu.Id.ToString(), token);
            }

            public async Task RemoveLogLinkAsync(CancellationToken token)
            {
                var ownedId = await scope.Settings.ReadLogMenuLinkIdAsync(token);
                if (!ownedId.HasValue)
                {
                    return;
                }
                var link = await FindLogLinkAsync(ownedId.Value, token);
                if (link == null)
                {
                    await scope.Settings.SetLogMenuLinkIdAsync(null, token);
                    return;
                }
                ValidateOwnership(link);
                var id = link.Id;
                await scope.Connection.DeleteAsync(scope.Transaction, GXDeleteArgs.Delete<GXMenuLinkPolicy>(p => p.MenuLinkId == id), token);
                await scope.Connection.DeleteAsync(scope.Transaction, GXDeleteArgs.Delete<GXMenuLink>(l => l.Id == id), token);
                await scope.Settings.SetLogMenuLinkIdAsync(null, token);
                if (link.Menu != null)
                {
                    await GXModuleRepositoryNotifications.QueueAsync(scope, GXMenuPolicies.Edit, link.Menu.Id.ToString(), token);
                }
            }

            private Task<GXMenuLink?> FindLogLinkAsync(Guid id, CancellationToken token)
            {
                var query = GXSelectArgs.SelectAll<GXMenuLink>(l => l.Id == id);
                query.Joins.AddLeftJoin<GXMenuLink, GXMenu>(l => l.Menu!, m => m.Id);
                query.Columns.Add<GXMenu>(m => new { m.Id, m.Name });
                return scope.Connection.SingleOrDefaultAsync<GXMenuLink>(scope.Transaction, query, token);
            }

            private void ValidateOwnership(GXMenuLink link)
            {
                if (link.Url != LogPath || link.Menu?.Name != "LogTab" || link.SystemDefined == true)
                {
                    throw new InvalidOperationException("The menu link does not match this module's ownership record.");
                }
            }
        }
    }
}
