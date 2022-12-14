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
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserGroupRepository : IUserGroupRepository
    {
        private readonly IGXHost _host;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGXEventsNotifier _eventsNotifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserGroupRepository(
            IGXHost host,
            UserManager<ApplicationUser> userManager,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userManager = userManager;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByUserGroup(ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByUserGroups(ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            return ret;
        }

        /// <inheritdoc cref="IUserGroupRepository.DeleteAsync"/>
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.UserGroupManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXUserGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXUserGroup> list = _host.Connection.Select<GXUserGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXUserGroup, List<string>> updates = new Dictionary<GXUserGroup, List<string>>();
            foreach (GXUserGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXUserGroup tmp = new GXUserGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.UserGroupDelete(it.Value, new GXUserGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(string userId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(a => a.UserId, b => b.Id);
            arg.Where.And<GXUser>(where => where.Removed == null && where.Id == userId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <summary>
        /// Returns user group with users in that group.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public async Task<List<GXUserGroup>> GetGroupWithUsers(Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null && where.Id == groupId);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(a => a.UserId, b => b.Id);
            arg.Where.And<GXUser>(where => where.Removed == null);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<GXUserGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListUserGroups? request,
            ListUserGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the user groups.
                arg = GXSelectArgs.SelectAll<GXUserGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetUserGroupsByUser(userId);
                arg.Columns.Clear();
                arg.Columns.Add<GXUserGroup>();
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXUserGroup>(q => q.CreationTime);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Descending = true;
            GXUserGroup[] groups = (await _host.Connection.SelectAsync<GXUserGroup>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.UserGroups = groups;
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXUserGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetUserGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXUserGroup>(e => e.Users);
            arg.Distinct = true;
            GXUser User = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            if (User == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            var ret = (await _host.Connection.SingleOrDefaultAsync<GXUserGroup>(arg));
            if (ret == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get users that belongs for this user group.
            arg = GXSelectArgs.SelectAll<GXUser>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId);
            arg.Where.And<GXUserGroupUser>(q => q.Removed == null && q.UserGroupId == id);
            ret.Users = await _host.Connection.SelectAsync<GXUser>(arg);

            ////////////////////////////////////////////////////
            //Get schedule groups in this user group.
            arg = GXSelectArgs.SelectAll<GXScheduleGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Where.And<GXUserGroupScheduleGroup>(q => q.Removed == null && q.UserGroupId == id);
            ret.ScheduleGroups = await _host.Connection.SelectAsync<GXScheduleGroup>(arg);
            ////////////////////////////////////////////////////
            //Get device groups.
            arg = GXSelectArgs.SelectAll<GXDeviceGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
            arg.Where.And<GXUserGroupDeviceGroup>(q => q.Removed == null && q.UserGroupId == id);
            ret.DeviceGroups = await _host.Connection.SelectAsync<GXDeviceGroup>(arg);
            return ret;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXUserGroup> userGrouprs)
        {
            DateTime now = DateTime.Now;
            string userId = null;
            //User is null if framework creates the user group.
            if (user != null)
            {
                userId = _userManager.GetUserId(user);
            }
            List<Guid> list = new List<Guid>();
            Dictionary<GXUserGroup, List<string>> updates = new Dictionary<GXUserGroup, List<string>>();
            foreach (GXUserGroup userGroup in userGrouprs)
            {
                if (string.IsNullOrEmpty(userGroup.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (userGroup.Id == Guid.Empty)
                {
                    userGroup.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(userGroup);
                    //Users must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXUserGroup>(e => e.Users);
                    _host.Connection.Insert(args);
                    list.Add(userGroup.Id);
                    if (userGroup.Users != null && userGroup.Users.Any())
                    {
                        //Add user(s) to user group.
                        AddUsersToGroup(userGroup.Users.Select(s => s.Id).ToArray(), userGroup.Id);
                    }
                    else if (userId != null)
                    {
                        //Add creator to user group.
                        AddUserToGroup(userId, userGroup.Id);
                    }
                    updates[userGroup] = await GetUsersAsync(user, userGroup.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXUserGroup>(q => q.ConcurrencyStamp, where => where.Id == userGroup.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != userGroup.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }

                    List<string> users = await GetUsersAsync(user, userGroup.Id);
                    userGroup.Updated = now;
                    userGroup.ConcurrencyStamp = Guid.NewGuid().ToString();
                    List<string> usersIds = userGroup.Users.Select(s => s.Id).ToList();
                    m = GXSelectArgs.Select<GXUserGroupUser>(c => c.UserId, where => where.Removed == null && where.UserGroupId == userGroup.Id);
                    m.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                    m.Where.And<GXUser>(w => w.Removed == null);
                    List<string> userTableIds = _host.Connection.Select<string>(m);
                    GXUpdateArgs args = GXUpdateArgs.Update(userGroup);
                    args.Exclude<GXUserGroup>(q => new { q.CreationTime, q.Users, q.ScheduleGroups });
                    _host.Connection.Update(args);
                    //Map users to user group.
                    if (userGroup.Users != null && userGroup.Users.Count != 0)
                    {
                        string[] removed = userTableIds.Except(users).ToArray();
                        string[] added = users.Except(userTableIds).ToArray();
                        foreach (var ug in added)
                        {
                            GXUserGroupUser ugu = new GXUserGroupUser()
                            {
                                UserGroupId = userGroup.Id,
                                UserId = ug,
                                CreationTime = now
                            };
                            _host.Connection.Insert(GXInsertArgs.Insert(ugu));
                        }
                        foreach (var ug in removed)
                        {
                            _host.Connection.Delete(GXDeleteArgs.DeleteById<GXUserGroupScheduleGroup>(ug));
                        }
                    }
                    //Map schedule groups to user group.
                    if (userGroup.ScheduleGroups != null && userGroup.ScheduleGroups.Count != 0)
                    {
                        List<GXScheduleGroup> scheduleGroups = await GetJoinedScheduleGroups(userGroup.Id);
                        var comparer = new UniqueComparer<GXScheduleGroup, Guid>();
                        List<GXScheduleGroup> removedScheduleGroups = scheduleGroups.Except(userGroup.ScheduleGroups, comparer).ToList();
                        List<GXScheduleGroup> addedScheduleGroups = userGroup.ScheduleGroups.Except(scheduleGroups, comparer).ToList();
                        if (removedScheduleGroups.Any())
                        {
                            RemoveScheduleGroupsFromUserGroup(userGroup.Id, removedScheduleGroups);
                        }
                        if (addedScheduleGroups.Any())
                        {
                            AddScheduleGroupToUserGroup(userGroup.Id, addedScheduleGroups);
                        }
                    }
                    updates[userGroup] = users;
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.UserGroupUpdate(it.Value, new GXUserGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add user to user group.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="group">Group ID of the group where the user is added.</param>
        public void AddUserToGroup(string userId, Guid group)
        {
            GXUserGroupUser ugu = new GXUserGroupUser()
            {
                UserGroupId = group,
                UserId = userId,
                CreationTime = DateTime.Now
            };
            _host.Connection.Insert(GXInsertArgs.Insert(ugu));
        }

        /// <summary>
        /// Add users to the user group.
        /// </summary>
        /// <param name="users">Users ID.</param>
        /// <param name="groupId">Group ID of the group where the user is added.</param>
        public void AddUsersToGroup(IEnumerable<string> users, Guid groupId)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupUser> list = new List<GXUserGroupUser>();
            foreach (string it in users)
            {
                list.Add(new GXUserGroupUser()
                {
                    UserGroupId = groupId,
                    UserId = it,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Add user to user groups.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="group">Group IDs of the user groups where the user is added.</param>
        public void AddUserToGroups(string userId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupUser> list = new List<GXUserGroupUser>();
            foreach (Guid it in groups)
            {
                list.Add(new GXUserGroupUser()
                {
                    UserGroupId = it,
                    UserId = userId,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }


        /// <summary>
        /// Map schedule group to user group.
        /// </summary>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Added schedule groups.</param>
        public void AddScheduleGroupToUserGroup(Guid userGroupId, IEnumerable<GXScheduleGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupScheduleGroup> list = new List<GXUserGroupScheduleGroup>();
            foreach (GXScheduleGroup it in groups)
            {
                list.Add(new GXUserGroupScheduleGroup()
                {
                    UserGroupId = userGroupId,
                    ScheduleGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and user group.
        /// </summary>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Removed schedule groups.</param>
        public void RemoveScheduleGroupsFromUserGroup(Guid userGroupId, IEnumerable<GXScheduleGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScheduleGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupScheduleGroup>(w => w.UserGroupId == userGroupId &&
                    w.ScheduleGroupId == it.Id);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Returns schedule groups that belong for this user group.
        /// </summary>
        /// <param name="userGroupId">User group ID</param>
        /// <returns>List of schedule groups.</returns>

        public async Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(Guid userGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            arg.Where.And<GXUserGroup>(where => where.Removed == null && where.Id == userGroupId);
            return (await _host.Connection.SelectAsync<GXScheduleGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetDefaultUserGroups(ClaimsPrincipal user, string userId)
        {
            GXSelectArgs args = GXQuery.GetUserGroupsByUser(userId);
            args.Where.And<GXUserGroup>(w => w.Default == true);
            return await _host.Connection.SelectAsync<GXUserGroup>(args);
        }
    }
}
