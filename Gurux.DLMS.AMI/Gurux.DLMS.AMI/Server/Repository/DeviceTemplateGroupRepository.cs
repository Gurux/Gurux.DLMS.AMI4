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
// This file is a part of Gurux DeviceTemplate Framework.
//
// Gurux DeviceTemplate Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux DeviceTemplate Framework is distributed in the hope that it will be useful,
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
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc cref="IDeviceTemplateGroupRepository"/>
    public class DeviceTemplateGroupRepository : IDeviceTemplateGroupRepository
    {
        private readonly IGXHost _host;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository)
        {
            _host = host;
            _userRepository = userRepository;
            _userManager = userManager;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid deviceTemplateId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == deviceTemplateId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }


        /// <inheritdoc />
        public async Task<List<GXDeviceTemplateGroup>> GetJoinedDeviceTemplateGroups(ClaimsPrincipal user, Guid deviceTemplateId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            arg.Where.And<GXDeviceTemplate>(where => where.Removed == null && where.Id == deviceTemplateId);
            return (await _host.Connection.SelectAsync<GXDeviceTemplateGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplateGroup(ServerHelpers.GetUserId(user), groupId);
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
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplateGroups(ServerHelpers.GetUserId(user), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <summary>
        /// Get all users that can access this device template group. 
        /// </summary>
        /// <returns></returns>
        List<string> GetUsers(string userId, Guid groupId, bool isAdmin)
        {
            GXSelectArgs args;
            //Check that user can access this group.
            if (!isAdmin)
            {
                args = GXSelectArgs.Select<GXUserGroupUser>(a => a.UserId);
                args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
                args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
                args.Where.And<GXUserGroup>(q => q.Removed == null);
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null && q.Id == groupId);
                if (_host.Connection.SingleOrDefault<bool>(GXSelectArgs.Select<GXUserGroupUser>(q => GXSql.IsEmpty(q), where => where.UserId == userId)))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            //Get users who can access this group.
            args = GXSelectArgs.Select<GXUserGroupUser>(a => a.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null && q.Id == groupId);
            List<GXUserGroupUser> users = _host.Connection.Select<GXUserGroupUser>(args);
            List<string> list = users.Select(s => s.UserId).ToList();
            if (isAdmin)
            {
                list.Add(userId);
            }
            return list;
        }

        /// <summary>
        /// Get all device template groups that this user can access. 
        /// </summary>
        /// <returns></returns>
        List<Guid> GetGroups(string userId, bool isAdmin)
        {
            GXSelectArgs args;
            List<GXDeviceTemplateGroup> groups;
            //Check that user can access this group.
            if (isAdmin)
            {
                args = GXSelectArgs.Select<GXDeviceTemplateGroup>(a => a.Id, q => q.Removed == null);
                groups = _host.Connection.Select<GXDeviceTemplateGroup>(args);
            }
            else
            {
                args = GXSelectArgs.Select<GXDeviceTemplateGroup>(a => a.Id, q => q.Removed == null);
                args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.DeviceTemplateGroupId);
                args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(a => a.Id, b => b.UserGroupId);
                args.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(a => a.UserId, b => b.Id);
                args.Where.And<GXUserGroup>(q => q.Removed == null);
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
                args.Where.And<GXUser>(q => q.Removed == null && q.Id == userId);
                groups = _host.Connection.Select<GXDeviceTemplateGroup>(args);
            }
            List<Guid> list = groups.Select(s => s.Id).ToList();
            return list;
        }

        /// <inheritdoc cref="IDeviceTemplateGroupRepository.DeleteAsync"/>
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> deviceTemplateGrouprs)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.DeviceTemplateGroupManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXDeviceTemplateGroup>(a => new { a.Id, a.Name }, q => deviceTemplateGrouprs.Contains(q.Id));
            List<GXDeviceTemplateGroup> list = _host.Connection.Select<GXDeviceTemplateGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = new Dictionary<GXDeviceTemplateGroup, List<string>>();
            string userId = _userManager.GetUserId(User);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            foreach (GXDeviceTemplateGroup it in list)
            {
                it.Removed = now;
                List<string> users = GetUsers(userId, it.Id, isAdmin);
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXDeviceTemplateGroup tmp = new GXDeviceTemplateGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.DeviceTemplateGroupDelete(it.Value, new GXDeviceTemplateGroup[] { tmp });
            }
        }
        /// <inheritdoc />
        public async Task<GXDeviceTemplateGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListDeviceTemplateGroups? request,
            ListDeviceTemplateGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the device template groups.
                arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceTemplateGroupsByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTemplateGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Descending = true;
            arg.OrderBy.Add<GXDeviceTemplateGroup>(q => q.CreationTime);
            GXDeviceTemplateGroup[] groups = (await _host.Connection.SelectAsync<GXDeviceTemplateGroup>(arg)).ToArray();
            if (response != null)
            {
                response.DeviceTemplateGroups = groups;
            }
            return groups;
        }

        /// <inheritdoc cref="IDeviceTemplateGroupRepository.ReadAsync"/>
        public async Task<GXDeviceTemplateGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceTemplateGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXDeviceTemplateGroup>(e => e.DeviceTemplates);
            arg.Distinct = true;
            GXDeviceTemplate DeviceTemplate = await _host.Connection.SingleOrDefaultAsync<GXDeviceTemplate>(arg);
            if (DeviceTemplate == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            var ret = (await _host.Connection.SingleOrDefaultAsync<GXDeviceTemplateGroup>(arg));
            if (ret == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get device templates that belongs for this device template group.
            arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
            arg.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
            ret.DeviceTemplates = await _host.Connection.SelectAsync<GXDeviceTemplate>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this device template group.
            arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupDeviceTemplateGroup>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
            ret.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return ret;
        }

        /// <inheritdoc cref="IDeviceTemplateGroupRepository.UpdateAsync"/>
        public async Task<Guid[]> UpdateAsync(ClaimsPrincipal User, IEnumerable<GXDeviceTemplateGroup> DeviceTemplateGroups)
        {
            DateTime now = DateTime.Now;
            bool isAdmin = true;
            string userId;
            if (User != null)
            {
                userId = _userManager.GetUserId(User);
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            List<Guid> list = new List<Guid>();
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = new Dictionary<GXDeviceTemplateGroup, List<string>>();
            foreach (GXDeviceTemplateGroup it in DeviceTemplateGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default script groups if not admin.
                    if (User != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(User,
                            ServerHelpers.GetUserId(User));
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
                    args.Exclude<GXDeviceTemplateGroup>(e => e.UserGroups);
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    //Add user group to DeviceTemplate group.
                    AddDeviceTemplateGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplateGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it);
                    args.Exclude<GXDeviceTemplateGroup>(e => e.UserGroups);
                    args.Exclude<GXDeviceTemplateGroup>(q => new { q.CreationTime, q.UserGroups, q.DeviceTemplates });
                    _host.Connection.Update(args);

                    //Map user group to DeviceTemplate group.
                    if (it.UserGroups != null && it.UserGroups.Count != 0)
                    {
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                        List<Guid> groups = list2.Select(s => s.Id).ToList();
                        Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                        Guid[] removed = groups.Except(tmp).ToArray();
                        Guid[] added = tmp.Except(groups).ToArray();
                        if (added.Length != 0)
                        {
                            AddDeviceTemplateGroupToUserGroups(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveDeviceTemplateGroupFromUserGroups(it.Id, removed);
                        }
                    }
                    //Map device template group to device template.
                    if (it.DeviceTemplates != null && it.DeviceTemplates.Count != 0)
                    {
                        List<GXDeviceTemplateGroup> list2 = await GetJoinedDeviceTemplateGroups(User, it.Id);
                        List<Guid> groups = list2.Select(s => s.Id).ToList();
                        Guid[] tmp = it.DeviceTemplates.Select(s => s.Id).ToArray();
                        Guid[] removed = groups.Except(tmp).ToArray();
                        Guid[] added = tmp.Except(groups).ToArray();
                        if (added.Length != 0)
                        {
                            AddDeviceTemplateGroupsToDeviceTemplate(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveDeviceTemplateGroupsFromDeviceTemplate(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(User, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceTemplateGroupUpdate(it.Value, new GXDeviceTemplateGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="DeviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupToUserGroups(Guid DeviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupDeviceTemplateGroup> list = new List<GXUserGroupDeviceTemplateGroup>();
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupDeviceTemplateGroup()
                {
                    DeviceTemplateGroupId = DeviceTemplateGroupId,
                    UserGroupId = it,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupFromUserGroups(Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupDeviceTemplateGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupDeviceTemplateGroup>(w => w.UserGroupId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            _host.Connection.Delete(args);
        }
        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="group">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupsToDeviceTemplate(Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceTemplateGroupDeviceTemplate> list = new List<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (var it in groups)
            {
                list.Add(new GXDeviceTemplateGroupDeviceTemplate()
                {
                    DeviceTemplateGroupId = deviceTemplateGroupId,
                    DeviceTemplateId = it,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="DeviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="group">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupsFromDeviceTemplate(Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (var it in groups)
            {
                args.Where.Or<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}