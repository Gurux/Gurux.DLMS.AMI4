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
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Client.Pages.Subtotal;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class SubtotalGroupRepository : ISubtotalGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;



        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalGroupRepository(
            IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task<List<GXSubtotalGroup>> GetJoinedSubtotalGroups(ClaimsPrincipal user, Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSubtotalGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXSubtotalGroup, GXSubtotalGroupSubtotal>(a => a.Id, b => b.SubtotalGroupId);
            arg.Joins.AddInnerJoin<GXSubtotalGroupSubtotal, GXSubtotal>(a => a.SubtotalId, b => b.Id);
            arg.Where.And<GXSubtotal>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXSubtotalGroup>(arg));
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid subtotalGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupSubtotalGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);
            arg.Where.And<GXSubtotalGroup>(where => where.Removed == null && where.Id == subtotalGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXSubtotal>> GetJoinedSubtotals(ClaimsPrincipal user, Guid subtotalGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSubtotal>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalGroupSubtotal>(a => a.Id, b => b.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalGroupSubtotal, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXSubtotalGroup>(where => where.Removed == null && where.Id == subtotalGroupId);
            return (await _host.Connection.SelectAsync<GXSubtotal>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersBySubtotalGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersBySubtotalGroups(ServerHelpers.GetUserId(user), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SubtotalGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXSubtotalGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXSubtotalGroup> list = _host.Connection.Select<GXSubtotalGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotalGroup, List<string>> updates = new Dictionary<GXSubtotalGroup, List<string>>();
            foreach (GXSubtotalGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXSubtotalGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXSubtotalGroup tmp = new GXSubtotalGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.SubtotalGroupDelete(it.Value, new GXSubtotalGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXSubtotalGroup[]> ListAsync(
            ClaimsPrincipal user,
            ListSubtotalGroups? request,
            ListSubtotalGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXSubtotalGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSubtotalGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If subtotal groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupSubtotalGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXSubtotalGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXSubtotalGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSubtotalGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSubtotalGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSubtotalGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXSubtotalGroup[] groups = (await _host.Connection.SelectAsync<GXSubtotalGroup>(arg)).ToArray();
            if (response != null)
            {
                response.SubtotalGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXSubtotalGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXSubtotalGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSubtotalGroupsByUser(userId, id);
            }
            arg.Columns.Add<GXSubtotalGroup>();
            arg.Columns.Exclude<GXSubtotalGroup>(e => e.Subtotals);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXSubtotalGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get subtotals that belong for this subtotal group.
            arg = GXSelectArgs.Select<GXSubtotal>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no subtotals in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXSubtotalGroupSubtotal, GXSubtotal>(j => j.SubtotalId, j => j.Id);
            arg.Where.And<GXSubtotalGroupSubtotal>(q => q.Removed == null && q.SubtotalGroupId == id);
            group.Subtotals = await _host.Connection.SelectAsync<GXSubtotal>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this subtotal group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no subtotals in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupSubtotalGroup>(q => q.Removed == null && q.SubtotalGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXSubtotalGroup> SubtotalGroups,
            Expression<Func<GXSubtotalGroup, object?>>? columns)
        {
            bool isAdmin = user.IsInRole(GXRoles.Admin);
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXSubtotalGroup, List<string>> updates = new Dictionary<GXSubtotalGroup, List<string>>();
            foreach (GXSubtotalGroup it in SubtotalGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user,
                                               ServerHelpers.GetUserId(user));
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
                    args.Exclude<GXSubtotalGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddSubtotalGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXSubtotalGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXSubtotalGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.Subtotals
                    });
                    if (!user.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXSubtotalGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to Subtotal group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddSubtotalGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveSubtotalGroupFromUserGroups(it.Id, removed);
                    }
                    //Map subtotals to Subtotal group.
                    //Only adming can add subtotals that are visible to all users.
                    if (it.Subtotals != null && (isAdmin || it.Subtotals.Any()))
                    {
                        List<GXSubtotal> list3 = await GetJoinedSubtotals(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Subtotals.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddSubtotalsToSubtotalGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveSubtotalsFromSubtotalGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.SubtotalGroupUpdate(it.Value, new GXSubtotalGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map subtotal group to user groups.
        /// </summary>
        /// <param name="subtotalGroupId">Subtotal group ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is added.</param>
        public void AddSubtotalGroupToUserGroups(Guid subtotalGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupSubtotalGroup> list = new List<GXUserGroupSubtotalGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupSubtotalGroup()
                {
                    SubtotalGroupId = subtotalGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between subtotal group and user groups.
        /// </summary>
        /// <param name="subtotalGroupId">Subtotal group ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is removed.</param>
        public void RemoveSubtotalGroupFromUserGroups(Guid subtotalGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupSubtotalGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupSubtotalGroup>(w => w.UserGroupId == ug &&
                    w.SubtotalGroupId == subtotalGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map subtotals to subtotal group.
        /// </summary>
        /// <param name="subtotalGroupId">Subtotal group ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is added.</param>
        public void AddSubtotalsToSubtotalGroup(Guid subtotalGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXSubtotalGroupSubtotal> list = new List<GXSubtotalGroupSubtotal>();
            foreach (var ug in groups)
            {
                list.Add(new GXSubtotalGroupSubtotal()
                {
                    SubtotalGroupId = subtotalGroupId,
                    SubtotalId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between subtotals and subtotal group.
        /// </summary>
        /// <param name="subtotalGroupId">Subtotal group ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is removed.</param>
        public void RemoveSubtotalsFromSubtotalGroup(Guid subtotalGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXSubtotalGroupSubtotal>();
            foreach (var it in groups)
            {
                args.Where.Or<GXSubtotalGroupSubtotal>(w => w.SubtotalId == it &&
                    w.SubtotalGroupId == subtotalGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}