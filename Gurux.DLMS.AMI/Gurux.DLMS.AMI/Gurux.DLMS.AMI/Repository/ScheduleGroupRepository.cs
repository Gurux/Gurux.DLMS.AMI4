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
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ScheduleGroupRepository : IScheduleGroupRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleGroupRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor?.User;
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _performanceSettings = performanceSettings;
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(IDbTransaction transaction, Guid scheduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return await _host.Connection.SelectAsync<GXUserGroup>(transaction, arg);
        }

        private async Task<List<GXSchedule>> GetJoinedSchedules(IDbTransaction transaction, Guid scheduleGroupId)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSchedule>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXSchedule, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            if (!User.IsInRole(GXRoles.Admin))
            {
                var id = ServerHelpers.GetUserId(User);
                arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            }
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return await _host.Connection.SelectAsync<GXSchedule>(transaction, arg);
        }

        /// <inheritdoc />
        public async Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(Guid scheduleId)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXSchedule>(where => where.Removed == null && where.Id == scheduleId);
            return (await _host.Connection.SelectAsync<GXScheduleGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroup(s => s.Id,
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroups(s => s.Id,
                ServerHelpers.GetUserId(User), groupIds);
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
            IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXScheduleGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXScheduleGroup> list = _host.Connection.Select<GXScheduleGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleGroup, List<string>> updates = new Dictionary<GXScheduleGroup, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in list)
                {
                    List<string> users = await GetUsersAsync(it.Id);
                    if (!delete)
                    {
                        it.Removed = now;
                        _host.Connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    await _host.Connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(list));
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.ScheduleGroup, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                await _eventsNotifier.ScheduleGroupDelete(users, [it.Key]);
            }
        }

        /// <inheritdoc />
        public async Task<GXScheduleGroup[]> ListAsync(
            ListScheduleGroups? request,
            ListScheduleGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the schedule groups.
                arg = GXSelectArgs.SelectAll<GXScheduleGroup>();
                arg.Joins.AddLeftJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXScheduleGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXScheduleGroup>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScheduleGroupsByUser(s => "*", userId, null,
                    request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If schedule groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupScheduleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
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
        public async Task<GXScheduleGroup> ReadAsync(Guid id)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroup) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXScheduleGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScheduleGroupsByUser(s => s.Id, userId, id);
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

            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXScheduleGroup>(w => w.Id == id);
            group.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXScheduleGroup> scheduleGroups,
            Expression<Func<GXScheduleGroup, object?>>? columns)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ScheduleGroupManager) &&
                !User.HasScope(GXScheduleGroupPolicies.Add)))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXScheduleGroup, List<string>> updates = new Dictionary<GXScheduleGroup, List<string>>();
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = scheduleGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = scheduleGroups.Where(w => w.Id != Guid.Empty).ToList();
            //Get notified users.
            if (newGroups.Any())
            {
                var first = newGroups.First();
                var users = await GetUsersAsync(first.Id);
                foreach (var it in newGroups)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedGroups)
            {
                updates[it] = await GetUsersAsync(it.Id);
            }

            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in scheduleGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        //Get default user groups.
                        if (defaultGroups == null)
                        {
                            defaultGroups = await _userGroupRepository.GetDefaultUserGroups(
                                creator.Id);
                        }
                        it.UserGroups = defaultGroups;
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
                            if (schedule.Id == Guid.Empty)
                            {
                                schedule.CreationTime = now;
                                schedule.Creator = creator;
                            }
                        }
                    }
                }
                if (newGroups.Any())
                {
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        if (it.Schedules != null)
                        {
                            //Update creator and creation time for all schedules.
                            foreach (var schedule in it.Schedules)
                            {
                                schedule.CreationTime = now;
                                schedule.Creator = creator;
                            }
                        }
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXScheduleGroup>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        //User groups must hanlde separetly because users are identified with name and not Guid.
                        e.UserGroups,
                        e.Schedules
                    });
                    await _host.Connection.InsertAsync(transaction, args);
                    foreach (GXScheduleGroup it in newGroups)
                    {
                        list.Add(it.Id);
                        await MapUserGroups(transaction, it, true);
                        await MapSchedules(transaction, it, true);
                    }
                }
                foreach (var it in updatedGroups)
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
                    args.Exclude<GXScheduleGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.Schedules
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXScheduleGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(transaction, args);
                    await MapUserGroups(transaction, it, false);
                    await MapSchedules(transaction, it, false);
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                //Only ID and name are notified.
                await _eventsNotifier.ScheduleGroupUpdate(it.Value,
                    new GXScheduleGroup[] { new GXScheduleGroup() { Id = it.Key.Id, Name = it.Key.Name } });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map user group to Schedule group.
        /// </summary>
        private async Task MapUserGroups(IDbTransaction transaction, GXScheduleGroup it, bool add)
        {
            if (it.UserGroups != null)
            {
                if (add)
                {
                    AddScheduleGroupToUserGroups(transaction, it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(transaction, it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddScheduleGroupToUserGroups(transaction, it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveScheduleGroupFromUserGroups(transaction, it.Id, removed);
                    }
                }
            }
        }

        private async Task MapSchedules(IDbTransaction transaction, GXScheduleGroup it, bool add)
        {
            //Map schedules to Schedule group.
            if (it.Schedules != null)
            {
                if (add)
                {
                    AddSchedulesToScheduleGroup(transaction, it.Id, it.Schedules);
                }
                else
                {
                    List<GXSchedule> list = await GetJoinedSchedules(transaction, it.Id);
                    var comparer = new UniqueComparer<GXSchedule, Guid>();
                    List<GXSchedule> removed = list.Except(it.Schedules, comparer).ToList();
                    List<GXSchedule> added = it.Schedules.Except(list, comparer).ToList();
                    if (added.Count != 0)
                    {
                        AddSchedulesToScheduleGroup(transaction, it.Id, added);
                    }
                    if (removed.Count != 0)
                    {
                        RemoveSchedulesFromScheduleGroup(transaction, it.Id, removed);
                    }
                }
            }
        }

        /// <summary>
        /// Map schedule group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is added.</param>
        public void AddScheduleGroupToUserGroups(IDbTransaction transaction, Guid scheduleGroupId, IEnumerable<Guid> groups)
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
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        public void RemoveScheduleGroupFromUserGroups(IDbTransaction transaction, Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScheduleGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupScheduleGroup>(w => w.UserGroupId == ug &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map schedules to schedule group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="schedules">Added schedules.</param>
        public void AddSchedulesToScheduleGroup(
            IDbTransaction transaction,
            Guid scheduleGroupId,
            IEnumerable<GXSchedule> schedules)
        {
            DateTime now = DateTime.Now;
            List<GXSchedule> added = new List<GXSchedule>();
            List<GXScheduleGroupSchedule> list = new List<GXScheduleGroupSchedule>();
            foreach (var schedule in schedules)
            {
                if (schedule.Id == Guid.Empty)
                {
                    added.Add(schedule);
                }
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(added));
            foreach (var schedule in schedules)
            {
                list.Add(new GXScheduleGroupSchedule()
                {
                    ScheduleGroupId = scheduleGroupId,
                    ScheduleId = schedule.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedules and schedule group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        public void RemoveSchedulesFromScheduleGroup(
            IDbTransaction transaction,
            Guid scheduleGroupId,
            IEnumerable<GXSchedule> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXScheduleGroupSchedule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScheduleGroupSchedule>(w => w.ScheduleId == it.Id &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }
    }
}