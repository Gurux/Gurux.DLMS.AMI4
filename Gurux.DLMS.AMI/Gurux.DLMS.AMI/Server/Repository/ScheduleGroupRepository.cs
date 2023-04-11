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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Pages.Config;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ScheduleGroupRepository : IScheduleGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleGroupRepository(
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

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid scheduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXSchedule>> GetJoinedSchedules(ClaimsPrincipal user, Guid scheduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSchedule>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXSchedule, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return (await _host.Connection.SelectAsync<GXSchedule>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(ClaimsPrincipal user, Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXSchedule>(where => where.Removed == null && where.Id == scheduleId);
            return (await _host.Connection.SelectAsync<GXScheduleGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroup(ServerHelpers.GetUserId(user), groupId);
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
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroups(ServerHelpers.GetUserId(user), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXScheduleGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXScheduleGroup> list = _host.Connection.Select<GXScheduleGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleGroup, List<string>> updates = new Dictionary<GXScheduleGroup, List<string>>();
            foreach (GXScheduleGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXScheduleGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXScheduleGroup tmp = new GXScheduleGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ScheduleGroupDelete(it.Value, new GXScheduleGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXScheduleGroup[]> ListAsync(
        ClaimsPrincipal User,
            ListScheduleGroups? request,
            ListScheduleGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the schedule groups.
                arg = GXSelectArgs.SelectAll<GXScheduleGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScheduleGroupsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScheduleGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXScheduleGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXScheduleGroup>(q => q.CreationTime);
            }
            GXScheduleGroup[] groups = (await _host.Connection.SelectAsync<GXScheduleGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ScheduleGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXScheduleGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXScheduleGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScheduleGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXScheduleGroup>(e => e.Schedules);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXScheduleGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get schedules that belongs for this schedule group.
            arg = GXSelectArgs.Select<GXSchedule>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(j => j.ScheduleId, j => j.Id);
            arg.Where.And<GXScheduleGroupSchedule>(q => q.Removed == null && q.ScheduleGroupId == id);
            group.Schedules = await _host.Connection.SelectAsync<GXSchedule>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this schedule group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupScheduleGroup>(q => q.Removed == null && q.ScheduleGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXScheduleGroup> ScheduleGroups,
            Expression<Func<GXScheduleGroup, object?>>? columns)
        {
            string userId = ServerHelpers.GetUserId(user);
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXScheduleGroup, List<string>> updates = new Dictionary<GXScheduleGroup, List<string>>();
            foreach (GXScheduleGroup it in ScheduleGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default script groups if not admin.
                    if (user != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user, userId);
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Schedules != null)
                {
                    //Update creator and creation time for all schedules.
                    foreach (var schedule in it.Schedules)
                    {
                        schedule.CreationTime = now;
                        schedule.Creator = new GXUser() { Id = userId };
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXScheduleGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddScheduleGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXScheduleGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXScheduleGroup>(q => new { q.UserGroups, q.CreationTime, q.Schedules });
                    _host.Connection.Update(args);
                    //Map user group to Schedule group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddScheduleGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveScheduleGroupFromUserGroups(it.Id, removed);
                    }
                    //Map schedules to Schedule group.
                    if (it.Schedules != null && it.Schedules.Count != 0)
                    {
                        List<GXSchedule> list3 = await GetJoinedSchedules(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Schedules.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddSchedulesToScheduleGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveSchedulesFromScheduleGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ScheduleGroupUpdate(it.Value, new GXScheduleGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map schedule group to user groups.
        /// </summary>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is added.</param>
        public void AddScheduleGroupToUserGroups(Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupScheduleGroup> list = new List<GXUserGroupScheduleGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupScheduleGroup()
                {
                    ScheduleGroupId = scheduleGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and user groups.
        /// </summary>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        public void RemoveScheduleGroupFromUserGroups(Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScheduleGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupScheduleGroup>(w => w.UserGroupId == ug &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map schedules to schedule group.
        /// </summary>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is added.</param>
        public void AddSchedulesToScheduleGroup(Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleGroupSchedule> list = new List<GXScheduleGroupSchedule>();
            foreach (var ug in groups)
            {
                list.Add(new GXScheduleGroupSchedule()
                {
                    ScheduleGroupId = scheduleGroupId,
                    ScheduleId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedules and schedule group.
        /// </summary>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        public void RemoveSchedulesFromScheduleGroup(Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXScheduleGroupSchedule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScheduleGroupSchedule>(w => w.ScheduleId == it &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}