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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ComponentViewGroupRepository : IComponentViewGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentViewGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            IUserGroupRepository userGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<GXComponentViewGroup>> GetJoinedComponentViewGroups(ClaimsPrincipal user, Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXComponentViewGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXComponentViewGroup, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewGroupId);
            arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(a => a.ComponentViewId, b => b.Id);
            arg.Where.And<GXComponentView>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXComponentViewGroup>(arg));
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid componentViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            arg.Where.And<GXComponentViewGroup>(where => where.Removed == null && where.Id == componentViewGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXComponentView>> GetJoinedComponentViews(ClaimsPrincipal user, Guid componentViewGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXComponentView>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXComponentView, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewId);
            arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXComponentViewGroup>(where => where.Removed == null && where.Id == componentViewGroupId);
            return (await _host.Connection.SelectAsync<GXComponentView>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByComponentViewGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }
        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByComponentViewGroups(ServerHelpers.GetUserId(user), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs, bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ComponentViewGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXComponentViewGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXComponentViewGroup> list = _host.Connection.Select<GXComponentViewGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXComponentViewGroup, List<string>> updates = new Dictionary<GXComponentViewGroup, List<string>>();
            foreach (GXComponentViewGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXComponentViewGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXComponentViewGroup tmp = new GXComponentViewGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ComponentViewGroupDelete(it.Value, new GXComponentViewGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXComponentViewGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListComponentViewGroups? request,
            ListComponentViewGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agent groups.
                arg = GXSelectArgs.SelectAll<GXComponentViewGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetComponentViewGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If component view groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupComponentViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXComponentViewGroup>(w => request.Exclude.Contains(w.Id) == false);
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXComponentViewGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXComponentViewGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXComponentViewGroup>(q => q.CreationTime);
            }
            GXComponentViewGroup[] groups = (await _host.Connection.SelectAsync<GXComponentViewGroup>(arg)).ToArray();
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
        public async Task<GXComponentViewGroup> ReadAsync(
         ClaimsPrincipal user,
         Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXComponentViewGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetComponentViewGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXComponentViewGroup>(e => e.ComponentViews);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXComponentViewGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get component views that belongs for this component view group.
            arg = GXSelectArgs.Select<GXComponentView>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(j => j.ComponentViewId, j => j.Id);
            arg.Where.And<GXComponentViewGroupComponentView>(q => q.Removed == null && q.ComponentViewGroupId == id);
            group.ComponentViews = await _host.Connection.SelectAsync<GXComponentView>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this component view group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupComponentViewGroup>(q => q.Removed == null && q.ComponentViewGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXComponentViewGroup> ComponentViewGroups,
            Expression<Func<GXComponentViewGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXComponentViewGroup, List<string>> updates = new Dictionary<GXComponentViewGroup, List<string>>();
            foreach (GXComponentViewGroup it in ComponentViewGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    if (user != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user, ServerHelpers.GetUserId(user));
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXComponentViewGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddComponentViewGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXComponentViewGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }

                    List<string> users = await GetUsersAsync(user, it.Id);
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXComponentViewGroup>(q => new { q.UserGroups, q.CreationTime, q.ComponentViews });
                    _host.Connection.Update(args);
                    //Map user group to ComponentView group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddComponentViewGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveComponentViewGroupFromUserGroups(it.Id, removed);
                    }
                    //Map componentViews to ComponentView group.
                    if (it.ComponentViews != null && it.ComponentViews.Any())
                    {
                        List<GXComponentView> list3 = await GetJoinedComponentViews(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.ComponentViews.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddComponentViewsToComponentViewGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveComponentViewsFromComponentViewGroup(it.Id, removed);
                        }
                    }
                    updates[it] = users;
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ComponentViewGroupUpdate(it.Value, new GXComponentViewGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map componentView group to user groups.
        /// </summary>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is added.</param>
        public void AddComponentViewGroupToUserGroups(Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupComponentViewGroup> list = new List<GXUserGroupComponentViewGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupComponentViewGroup()
                {
                    ComponentViewGroupId = componentViewGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between componentView group and user groups.
        /// </summary>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is removed.</param>
        public void RemoveComponentViewGroupFromUserGroups(Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupComponentViewGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupComponentViewGroup>(w => w.UserGroupId == ug &&
                    w.ComponentViewGroupId == componentViewGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map componentViews to componentView group.
        /// </summary>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is added.</param>
        public void AddComponentViewsToComponentViewGroup(Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXComponentViewGroupComponentView> list = new List<GXComponentViewGroupComponentView>();
            foreach (var ug in groups)
            {
                list.Add(new GXComponentViewGroupComponentView()
                {
                    ComponentViewGroupId = componentViewGroupId,
                    ComponentViewId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between componentViews and componentView group.
        /// </summary>
        /// <param name="componentViewGroupId">ComponentView group ID.</param>
        /// <param name="groups">Group IDs of the componentView groups where the componentView is removed.</param>
        public void RemoveComponentViewsFromComponentViewGroup(Guid componentViewGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXComponentViewGroupComponentView>();
            foreach (var it in groups)
            {
                args.Where.Or<GXComponentViewGroupComponentView>(w => w.ComponentViewId == it &&
                    w.ComponentViewGroupId == componentViewGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}