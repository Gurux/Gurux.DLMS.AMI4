//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class MenuRepository : IMenuRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMenuGroupRepository _menuGroupRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IMenuGroupRepository menuGroupRepository,
            IGXEventsNotifier eventsNotifier,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _menuGroupRepository = menuGroupRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? menuId)
        {
            GXSelectArgs args = GXQuery.GetUsersByMenu(s => s.Id,
                ServerHelpers.GetUserId(User), menuId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? menuIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByMenus(s => s.Id,
                ServerHelpers.GetUserId(User), menuIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> menus,
            bool delete)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.MenuManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(a => a.Id, q => menus.Contains(q.Id));
            List<GXMenu> list = _host.Connection.Select<GXMenu>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXMenu, List<string>> updates = new();
            foreach (GXMenu it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXMenu>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.Menu, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXMenu tmp = new GXMenu() { Id = it.Key.Id };
                await _eventsNotifier.MenuDelete(users, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXMenu[]> ListAsync(
            ListMenus? request,
            ListMenusResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the menus.
                arg = GXSelectArgs.SelectAll<GXMenu>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXMenu>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXMenu>(w => request.Included.Contains(w.Id));
                }
            }
            else if (User.Identity.IsAuthenticated)
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetMenusByUser(s => "*", userId,
                    null, request?.Exclude, request?.Included);
            }
            else
            {
                return null;
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
                arg.OrderBy.Add<GXMenu>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXMenu>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXMenu[] menus = (await _host.Connection.SelectAsync<GXMenu>(arg)).ToArray();
            if (request?.Filter?.Active.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                menus = menus.Where(w => w.Active == true).ToArray();
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

        /// <inheritdoc />
        public async Task<GXMenu> ReadAsync(string name)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == name);
            GXMenu menu = await _host.Connection.SingleOrDefaultAsync<GXMenu>(arg);
            if (menu == null)
            {
                //Menu is null if admin is not created.
                return null;
            }
            return await ReadAsync(menu.Id);
        }

        /// <inheritdoc />
        public async Task<GXMenu> ReadAsync(Guid id)
        {
            GXSelectArgs arg;
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the menus.
                arg = GXSelectArgs.SelectAll<GXMenu>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXMenu, GXMenuGroupMenu>(x => x.Id, y => y.MenuId);
                arg.Joins.AddLeftJoin<GXMenuGroupMenu, GXMenuGroup>(j => j.MenuGroupId, j => j.Id);
            }
            else if (User?.Identity?.IsAuthenticated == true)
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetMenusByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenuGroup>(j => j.MenuGroupId, j => j.Id);
            }
            else
            {
                //Anonymous user can see only menus in public menu groups.
                arg = GXSelectArgs.SelectAll<GXMenu>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXMenu, GXMenuGroupMenu>(x => x.Id, y => y.MenuId);
                arg.Joins.AddLeftJoin<GXMenuGroupMenu, GXMenuGroup>(j => j.MenuGroupId, j => j.Id);
                arg.Joins.AddLeftJoin<GXMenu, GXMenuRole>(x => x.Id, y => y.MenuId);
                arg.Where.And<GXMenuRole>(w => w.RoleId == null);
            }
            arg.Columns.Add<GXMenuGroup>();
            arg.Columns.Exclude<GXMenuGroup>(e => e.Menus);
            arg.Distinct = true;
            GXMenu menu = await _host.Connection.SingleOrDefaultAsync<GXMenu>(arg);
            if (menu == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get links.
            arg = GXSelectArgs.SelectAll<GXMenuLink>(w => w.Menu == menu);
            arg.OrderBy.Add<GXMenuLink>(o => o.Order);
            arg.Columns.Exclude<GXMenuLink>(e => e.Menu);
            if (User?.Identity?.IsAuthenticated != true)
            {
                //Anonymous user can see only menus in public menu groups.
                arg.Joins.AddLeftJoin<GXMenuLink, GXMenuLinkRole>(x => x.Id, y => y.MenuLinkId);
                arg.Where.And<GXMenuLinkRole>(w => w.MenuLinkId == null);
            }
            menu.Links = (await _host.Connection.SelectAsync<GXMenuLink>(arg)).ToList();
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXMenu, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXMenu>(w => w.Id == id);
            menu.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return menu;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXMenu> menus,
            Expression<Func<GXMenu, object?>>? columns)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.MenuManager)))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new();
            Dictionary<GXMenu, List<string>> updates = new();
            foreach (GXMenu menu in menus)
            {
                if (string.IsNullOrEmpty(menu.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(menu.Title))
                {
                    throw new ArgumentException(Properties.Resources.InvalidTitle);
                }
                if (menu.MenuGroups == null || !menu.MenuGroups.Any())
                {
                    ListMenuGroups request = new ListMenuGroups()
                    {
                        Filter = new GXMenuGroup() { Default = true }
                    };
                    menu.MenuGroups = new List<GXMenuGroup>();
                    menu.MenuGroups.AddRange(await _menuGroupRepository.ListAsync(request, null, CancellationToken.None));
                    if (!menu.MenuGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
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
                    _host.Connection.Insert(args);
                    list.Add(menu.Id);
                    AddMenuToMenuGroups(menu.Id, menu.MenuGroups);
                    AddLinksToMenu(menu, menu.Links);
                    updates[menu] = await GetUsersAsync(menu.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXMenu>(q => q.ConcurrencyStamp, where => where.Id == menu.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
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
                        args.Exclude<GXMenu>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map menu groups to menu.
                    List<GXMenuGroup> menuGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IMenuGroupRepository menuGroupRepository = scope.ServiceProvider.GetRequiredService<IMenuGroupRepository>();
                        menuGroups = await menuGroupRepository.GetJoinedMenuGroups(menu.Id);
                    }
                    var comparer = new UniqueComparer<GXMenuGroup, Guid>();
                    List<GXMenuGroup> removedMenuGroups = menuGroups.Except(menu.MenuGroups, comparer).ToList();
                    List<GXMenuGroup> addedMenuGroups = menu.MenuGroups.Except(menuGroups, comparer).ToList();
                    if (removedMenuGroups.Any())
                    {
                        RemoveMenusFromMenuGroup(menu.Id, removedMenuGroups);
                    }
                    if (addedMenuGroups.Any())
                    {
                        AddMenuToMenuGroups(menu.Id, addedMenuGroups);
                    }
                    updates[menu] = await GetUsersAsync(menu.Id);

                    //Update links.
                    GXMenu iMenu = await ReadAsync(menu.Id);
                    if (menu.Links != null)
                    {
                        var comparer4 = new UniqueComparer<GXMenuLink, Guid>();
                        List<GXMenuLink>? addedLinks;
                        if (iMenu.Links == null)
                        {
                            //If new link is added.
                            AddLinksToMenu(menu, menu.Links);
                        }
                        else
                        {
                            List<GXMenuLink> updatedLinks = menu.Links.Union(iMenu.Links, comparer4).ToList();
                            List<GXMenuLink> removedLinks = iMenu.Links.Except(menu.Links, comparer4).ToList();
                            addedLinks = menu.Links.Except(iMenu.Links, comparer4).ToList();
                            if (removedLinks != null && removedLinks.Any())
                            {
                                RemoveLinksFromMenu(removedLinks);
                            }
                            if (addedLinks.Any())
                            {
                                AddLinksToMenu(menu, addedLinks);
                            }
                            if (updatedLinks.Any())
                            {
                                foreach (var it in updatedLinks)
                                {
                                    GXUpdateArgs u = GXUpdateArgs.Update(it);
                                    u.Exclude<GXMenuLink>(e => new
                                    {
                                        e.CreationTime,
                                        e.Menu
                                    });
                                    await _host.Connection.UpdateAsync(u);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.MenuUpdate(it.Value, new GXMenu[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map menu group to user groups.
        /// </summary>
        /// <param name="menuId">Menu ID.</param>
        /// <param name="groups">Group IDs of the menu groups where the menu is added.</param>
        public void AddMenuToMenuGroups(Guid menuId, IEnumerable<GXMenuGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXMenuGroupMenu> list = new();
            foreach (GXMenuGroup it in groups)
            {
                list.Add(new GXMenuGroupMenu()
                {
                    MenuId = menuId,
                    MenuGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between menu group and menu.
        /// </summary>
        /// <param name="menuId">Menu ID.</param>
        /// <param name="groups">Group IDs of the menu groups where the menu is removed.</param>
        public void RemoveMenusFromMenuGroup(Guid menuId, IEnumerable<GXMenuGroup> groups)
        {
            foreach (GXMenuGroup it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXMenuGroupMenu>(w => w.MenuId == menuId && w.MenuGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add link to menu.
        /// </summary>
        /// <param name="menu">Menu where links are joined.</param>
        /// <param name="links">Menu links.</param>
        public void AddLinksToMenu(GXMenu menu, IEnumerable<GXMenuLink>? links)
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
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(links));
            }
        }

        /// <summary>
        /// Remove map between menu group and menu.
        /// </summary>
        /// <param name="links">Menu links.</param>
        public void RemoveLinksFromMenu(IEnumerable<GXMenuLink> links)
        {
            foreach (GXMenuLink it in links)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXMenuLink>(w => w.Id == it.Id));
            }
        }
    }
}
