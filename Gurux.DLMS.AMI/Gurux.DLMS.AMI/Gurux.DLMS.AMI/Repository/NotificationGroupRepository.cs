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
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class NotificationGroupRepository : INotificationGroupRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationGroupRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.User) &&
                !user.IsInRole(GXRoles.NotificationGroup) &&
                !user.IsInRole(GXRoles.NotificationGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<GXNotificationGroup>> GetJoinedNotificationGroups(Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXNotificationGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXNotificationGroup, GXNotificationGroupNotification>(a => a.Id, b => b.NotificationGroupId);
            arg.Joins.AddInnerJoin<GXNotificationGroupNotification, GXNotification>(a => a.NotificationId, b => b.Id);
            arg.Where.And<GXNotification>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXNotificationGroup>(arg));
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid notificationGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupNotificationGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupNotificationGroup, GXNotificationGroup>(a => a.NotificationGroupId, b => b.Id);
            arg.Where.And<GXNotificationGroup>(where => where.Removed == null && where.Id == notificationGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXNotification>> GetJoinedNotifications(Guid notificationGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXNotification>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationGroupNotification>(a => a.Id, b => b.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationGroupNotification, GXNotificationGroup>(a => a.NotificationGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupNotificationGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXNotificationGroup>(where => where.Removed == null && where.Id == notificationGroupId);
            return (await _host.Connection.SelectAsync<GXNotification>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByNotificationGroup(s => s.Id,
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
            GXSelectArgs args = GXQuery.GetUsersByNotificationGroups(s => s.Id,
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
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.NotificationGroupManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXNotificationGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXNotificationGroup> list = _host.Connection.Select<GXNotificationGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXNotificationGroup, List<string>> updates = new Dictionary<GXNotificationGroup, List<string>>();
            foreach (GXNotificationGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXNotificationGroup>(it.Id));
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
                    it.Value, TargetType.NotificationGroup, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXNotificationGroup tmp = new GXNotificationGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.NotificationGroupDelete(users, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXNotificationGroup[]> ListAsync(

            ListNotificationGroups? request,
            ListNotificationGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXNotificationGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetNotificationGroupsByUser(s => "*", userId, null);
            }
            if (request != null)
            {
                //If notification groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupNotificationGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXNotificationGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXNotificationGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXNotificationGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXNotificationGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXNotificationGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXNotificationGroup[] groups = (await _host.Connection.SelectAsync<GXNotificationGroup>(arg)).ToArray();
            if (response != null)
            {
                response.NotificationGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXNotificationGroup> ReadAsync(Guid id)
        {
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXNotificationGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetNotificationGroupsByUser(s => "*", userId, id);
            }
            arg.Columns.Add<GXNotificationGroup>();
            arg.Columns.Exclude<GXNotificationGroup>(e => e.Notifications);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXNotificationGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get notifications that belong for this notification group.
            arg = GXSelectArgs.Select<GXNotification>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no notifications in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXNotificationGroupNotification, GXNotification>(j => j.NotificationId, j => j.Id);
            arg.Where.And<GXNotificationGroupNotification>(q => q.Removed == null && q.NotificationGroupId == id);
            group.Notifications = await _host.Connection.SelectAsync<GXNotification>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this notification group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no notifications in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupNotificationGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupNotificationGroup>(q => q.Removed == null && q.NotificationGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXNotificationGroup, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXNotificationGroup>(w => w.Id == id);
            group.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXNotificationGroup> NotificationGroups,
            Expression<Func<GXNotificationGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXNotificationGroup, List<string>> updates = new Dictionary<GXNotificationGroup, List<string>>();
            foreach (GXNotificationGroup it in NotificationGroups)
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
                    args.Exclude<GXNotificationGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddNotificationGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXNotificationGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXNotificationGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.Notifications
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXNotificationGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to Notification group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddNotificationGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveNotificationGroupFromUserGroups(it.Id, removed);
                    }
                    //Map notifications to Notification group.
                    //Only adming can add notifications that are visible to all users.
                    if (it.Notifications != null && (User.IsInRole(GXRoles.Admin) || it.Notifications.Any()))
                    {
                        List<GXNotification> list3 = await GetJoinedNotifications(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Notifications.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddNotificationsToNotificationGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveNotificationsFromNotificationGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.NotificationGroupUpdate(it.Value, [it.Key]);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map notification group to user groups.
        /// </summary>
        /// <param name="notificationGroupId">Notification group ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is added.</param>
        public void AddNotificationGroupToUserGroups(Guid notificationGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupNotificationGroup> list = new List<GXUserGroupNotificationGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupNotificationGroup()
                {
                    NotificationGroupId = notificationGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between notification group and user groups.
        /// </summary>
        /// <param name="notificationGroupId">Notification group ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is removed.</param>
        public void RemoveNotificationGroupFromUserGroups(Guid notificationGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupNotificationGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupNotificationGroup>(w => w.UserGroupId == ug &&
                    w.NotificationGroupId == notificationGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map notifications to notification group.
        /// </summary>
        /// <param name="notificationGroupId">Notification group ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is added.</param>
        public void AddNotificationsToNotificationGroup(Guid notificationGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXNotificationGroupNotification> list = new List<GXNotificationGroupNotification>();
            foreach (var ug in groups)
            {
                list.Add(new GXNotificationGroupNotification()
                {
                    NotificationGroupId = notificationGroupId,
                    NotificationId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between notifications and notification group.
        /// </summary>
        /// <param name="notificationGroupId">Notification group ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is removed.</param>
        public void RemoveNotificationsFromNotificationGroup(Guid notificationGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXNotificationGroupNotification>();
            foreach (var it in groups)
            {
                args.Where.Or<GXNotificationGroupNotification>(w => w.NotificationId == it &&
                    w.NotificationGroupId == notificationGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}