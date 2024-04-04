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
using Gurux.DLMS.AMI.Client.Pages.Admin;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class TriggerGroupRepository : ITriggerGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid triggerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            arg.Where.And<GXTriggerGroup>(where => where.Removed == null && where.Id == triggerGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXTrigger>> GetJoinedTriggers(Guid triggerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTrigger>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXTrigger, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerId);
            arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            arg.Where.And<GXTriggerGroup>(where => where.Removed == null && where.Id == triggerGroupId);
            return (await _host.Connection.SelectAsync<GXTrigger>(arg));
        }


        /// <inheritdoc />
        public async Task<List<GXTriggerGroup>> GetJoinedTriggerGroups(ClaimsPrincipal User, Guid triggerId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTriggerGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerGroupId);
            arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(a => a.TriggerId, b => b.Id);
            arg.Where.And<GXTrigger>(where => where.Removed == null && where.Id == triggerId);
            return (await _host.Connection.SelectAsync<GXTriggerGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByTriggerGroup(ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByTriggerGroups(ServerHelpers.GetUserId(User), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || !User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.TriggerGroupManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXTriggerGroup>(a => a.Id, q => userGrouprs.Contains(q.Id));
            List<GXTriggerGroup> list = _host.Connection.Select<GXTriggerGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXTriggerGroup, List<string>> updates = new Dictionary<GXTriggerGroup, List<string>>();
            foreach (GXTriggerGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXTriggerGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXTriggerGroup tmp = new GXTriggerGroup() { Id = it.Key.Id };
                await _eventsNotifier.TriggerGroupDelete(it.Value, new GXTriggerGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXTriggerGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListTriggerGroups? request,
            ListTriggerGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the trigger groups.
                arg = GXSelectArgs.SelectAll<GXTriggerGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetTriggerGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If trigger groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupTriggerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXTriggerGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXTriggerGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTriggerGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXTriggerGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXTriggerGroup>(q => q.CreationTime);
            }
            GXTriggerGroup[] groups = (await _host.Connection.SelectAsync<GXTriggerGroup>(arg)).ToArray();
            if (response != null)
            {
                response.TriggerGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc cref="ITriggerGroupRepository.ReadAsync"/>
        public async Task<GXTriggerGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXTriggerGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetTriggerGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXTriggerGroup>(e => e.Triggers);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXTriggerGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get triggers that belongs for this trigger group.
            arg = GXSelectArgs.SelectAll<GXTrigger>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(j => j.TriggerId, j => j.Id);
            arg.Where.And<GXTriggerGroupTrigger>(q => q.Removed == null && q.TriggerGroupId == id);
            group.Triggers = await _host.Connection.SelectAsync<GXTrigger>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this trigger group.
            arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupTriggerGroup>(q => q.Removed == null && q.TriggerGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXTriggerGroup> TriggerGroups,
            Expression<Func<GXTriggerGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXTriggerGroup, List<string>> updates = new Dictionary<GXTriggerGroup, List<string>>();
            foreach (GXTriggerGroup it in TriggerGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(User,
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
                    args.Exclude<GXTriggerGroup>(e => e.UserGroups);
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddTriggerGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXTriggerGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXTriggerGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXTriggerGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to Trigger group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddTriggerGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveTriggerGroupFromUserGroups(it.Id, removed);
                    }
                    //Map triggers to Trigger group.
                    if (it.Triggers != null && it.Triggers.Any())
                    {
                        List<GXTrigger> list3 = await GetJoinedTriggers(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Triggers.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddTriggersToTriggerGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveTriggersFromTriggerGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(User, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.TriggerGroupUpdate(it.Value, new GXTriggerGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map trigger group to user groups.
        /// </summary>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        public void AddTriggerGroupToUserGroups(Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupTriggerGroup> list = new List<GXUserGroupTriggerGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupTriggerGroup()
                {
                    TriggerGroupId = triggerGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between trigger group and user groups.
        /// </summary>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        public void RemoveTriggerGroupFromUserGroups(Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupTriggerGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupTriggerGroup>(w => w.UserGroupId == ug &&
                    w.TriggerGroupId == triggerGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map triggers to trigger group.
        /// </summary>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        public void AddTriggersToTriggerGroup(Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXTriggerGroupTrigger> list = new List<GXTriggerGroupTrigger>();
            foreach (var ug in groups)
            {
                list.Add(new GXTriggerGroupTrigger()
                {
                    TriggerGroupId = triggerGroupId,
                    TriggerId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between triggers and trigger group.
        /// </summary>
        /// <param name="triggerGroupId">Trigger group ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        public void RemoveTriggersFromTriggerGroup(Guid triggerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXTriggerGroupTrigger>();
            foreach (var it in groups)
            {
                args.Where.Or<GXTriggerGroupTrigger>(w => w.TriggerId == it &&
                    w.TriggerGroupId == triggerGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}