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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class MenuGroupRepository : IMenuGroupRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuGroupRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<GXMenuGroup>> GetJoinedMenuGroups(Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXMenuGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXMenuGroup, GXMenuGroupMenu>(a => a.Id, b => b.MenuGroupId);
            arg.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenu>(a => a.MenuId, b => b.Id);
            arg.Where.And<GXMenu>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXMenuGroup>(arg));
        }


        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid MenuGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupMenuGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXMenuGroup>(a => a.MenuGroupId, b => b.Id);
            arg.Where.And<GXMenuGroup>(where => where.Removed == null && where.Id == MenuGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXMenu>> GetJoinedMenus(Guid MenuGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXMenu>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXMenu, GXMenuGroupMenu>(a => a.Id, b => b.MenuId);
            arg.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenuGroup>(a => a.MenuGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXMenuGroup>(where => where.Removed == null && where.Id == MenuGroupId);
            return (await _host.Connection.SelectAsync<GXMenu>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByMenuGroup(s => s.Id,
                ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByMenuGroups(s => s.Id,
                ServerHelpers.GetUserId(User), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenuGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXMenuGroup> list = _host.Connection.Select<GXMenuGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXMenuGroup, List<string>> updates = new Dictionary<GXMenuGroup, List<string>>();
            foreach (GXMenuGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXMenuGroup>(it.Id));
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
                    it.Value, TargetType.MenuGroup, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXMenuGroup tmp = new GXMenuGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.MenuGroupDelete(users, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXMenuGroup[]> ListAsync(
            ListMenuGroups? request,
            ListMenuGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXMenuGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetMenuGroupsByUser(s => "*", userId);
            }
            if (request != null)
            {
                //If menu groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupMenuGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user2);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
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
                arg.OrderBy.Add<GXMenuGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXMenuGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXMenuGroup[] groups = (await _host.Connection.SelectAsync<GXMenuGroup>(arg)).ToArray();
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
        public async Task<GXMenuGroup> ReadAsync(Guid id)
        {
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
            arg.Columns.Exclude<GXMenuGroup>(e => e.Menus);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXMenuGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get blocks that belong for this block group.
            arg = GXSelectArgs.Select<GXMenu>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXMenuGroupMenu, GXMenu>(j => j.MenuId, j => j.Id);
            arg.Where.And<GXMenuGroupMenu>(q => q.Removed == null && q.MenuGroupId == id);
            group.Menus = await _host.Connection.SelectAsync<GXMenu>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this block group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupMenuGroup>(q => q.Removed == null && q.MenuGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXMenuGroup, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXMenuGroup>(w => w.Id == id);
            group.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXMenuGroup> MenuGroups,
            Expression<Func<GXMenuGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXMenuGroup, List<string>> updates = new Dictionary<GXMenuGroup, List<string>>();
            foreach (GXMenuGroup it in MenuGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(
                        ServerHelpers.GetUserId(User));
                    if (it.UserGroups == null || !it.UserGroups.Any())
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
                    args.Exclude<GXMenuGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddMenuGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXMenuGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
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
                        args.Exclude<GXMenuGroup>(q => q.Creator);
                    }

                    _host.Connection.Update(args);
                    //Map user group to Menu group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddMenuGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveMenuGroupFromUserGroups(it.Id, removed);
                    }
                    //Map blocks to Menu group.
                    //Only adming can add blocks that are visible to all users.
                    if (it.Menus != null && (User.IsInRole(GXRoles.Admin) || it.Menus.Any()))
                    {
                        List<GXMenu> list3 = await GetJoinedMenus(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Menus.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddMenusToMenuGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveMenusFromMenuGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.MenuGroupUpdate(it.Value, [it.Key]);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map block group to user groups.
        /// </summary>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddMenuGroupToUserGroups(Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupMenuGroup> list = new List<GXUserGroupMenuGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupMenuGroup()
                {
                    MenuGroupId = MenuGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and user groups.
        /// </summary>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveMenuGroupFromUserGroups(Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupMenuGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupMenuGroup>(w => w.UserGroupId == ug &&
                    w.MenuGroupId == MenuGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddMenusToMenuGroup(Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXMenuGroupMenu> list = new List<GXMenuGroupMenu>();
            foreach (var ug in groups)
            {
                list.Add(new GXMenuGroupMenu()
                {
                    MenuGroupId = MenuGroupId,
                    MenuId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="MenuGroupId">Menu group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveMenusFromMenuGroup(Guid MenuGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXMenuGroupMenu>();
            foreach (var it in groups)
            {
                args.Where.Or<GXMenuGroupMenu>(w => w.MenuId == it &&
                    w.MenuGroupId == MenuGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}