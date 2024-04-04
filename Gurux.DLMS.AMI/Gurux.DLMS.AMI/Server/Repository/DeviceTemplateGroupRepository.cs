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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared;
using System.Data;
using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DeviceTemplateGroupRepository : IDeviceTemplateGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateGroupRepository(
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

        /// <summary>
        /// Returns user groups where device template belongs.
        /// </summary>
        /// <param name="groupId">Device template group ID.</param>
        /// <returns>User groups where device template belongs.</returns>
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == groupId);
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

        /// <summary>
        /// Returns user groups where device template belongs.
        /// </summary>
        /// <param name="groupId">Device template group ID.</param>
        /// <returns>User groups where device template belongs.</returns>
        private async Task<List<GXDeviceTemplate>> GetJoinedDeviceTemplate(ClaimsPrincipal user, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == groupId);
            return (await _host.Connection.SelectAsync<GXDeviceTemplate>(arg));
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

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> deviceTemplateGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.DeviceTemplateGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXDeviceTemplateGroup>(a => new { a.Id, a.Name }, q => deviceTemplateGrouprs.Contains(q.Id));
            List<GXDeviceTemplateGroup> list = _host.Connection.Select<GXDeviceTemplateGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = new Dictionary<GXDeviceTemplateGroup, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in list)
                {
                    List<string> users = await GetUsersAsync(User, it.Id);
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
                await _eventsNotifier.DeviceTemplateGroupDelete(it.Value, new GXDeviceTemplateGroup[] { it.Key });
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
            if (request != null)
            {
                //If device template groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXDeviceTemplateGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXDeviceTemplateGroup>(w => request.Included.Contains(w.Id));
                }
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
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDeviceTemplateGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDeviceTemplateGroup>(q => q.CreationTime);
            }
            GXDeviceTemplateGroup[] groups = (await _host.Connection.SelectAsync<GXDeviceTemplateGroup>(arg)).ToArray();
            if (response != null)
            {
                response.DeviceTemplateGroups = groups;
            }
            return groups;
        }

        /// <inheritdoc />
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
            var group = (await _host.Connection.SingleOrDefaultAsync<GXDeviceTemplateGroup>(arg));
            if (group == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.DeviceTemplateGroup + " " + Properties.Resources.Id + " " + id.ToString());
            }
            ////////////////////////////////////////////////////
            //Get device templates that belongs for this device template group.
            arg = GXSelectArgs.Select<GXDeviceTemplate>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
            arg.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
            group.DeviceTemplates = await _host.Connection.SelectAsync<GXDeviceTemplate>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this device template group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupDeviceTemplateGroup>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXDeviceTemplateGroup> DeviceTemplateGroups,
            Expression<Func<GXDeviceTemplateGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = new Dictionary<GXDeviceTemplateGroup, List<string>>();
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = DeviceTemplateGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = DeviceTemplateGroups.Where(w => w.Id != Guid.Empty).ToList();
            //Get notified users.
            if (newGroups.Any())
            {
                var first = newGroups.First();
                var users = await GetUsersAsync(User, first.Id);
                foreach (var it in newGroups)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedGroups)
            {
                updates[it] = await GetUsersAsync(User, it.Id);
            }
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            foreach (GXDeviceTemplateGroup it in DeviceTemplateGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Check the device templates are already inserted.
                if (it.DeviceTemplates != null && it.DeviceTemplates.Where(w => w.Id == Guid.Empty).Any())
                {
                    throw new ArgumentException(Properties.Resources.JoinedGroupsAreNotInserted);
                }

                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    if (defaultGroups == null)
                    {
                        defaultGroups = await _userGroupRepository.GetDefaultUserGroups(User, creator.Id);
                    }
                    it.UserGroups = defaultGroups;
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
            }
            try
            {
                if (newGroups.Any())
                {
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXDeviceTemplateGroup>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        //User groups must hanlde separetly because users are identified with name and not Guid.
                        e.UserGroups
                    });
                    await _host.Connection.InsertAsync(transaction, args);
                    //Add user group to DeviceTemplate group.
                    foreach (var it in newGroups)
                    {
                        if (it.UserGroups != null)
                        {
                            AddDeviceTemplateGroupToUserGroups(transaction, it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                        }
                    }
                    foreach (var it in newGroups)
                    {
                        list.Add(it.Id);
                    }
                }
                foreach (var it in updatedGroups)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplateGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXDeviceTemplateGroup>(q => new
                    {
                        q.CreationTime,
                        q.UserGroups,
                        q.DeviceTemplates
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXDeviceTemplateGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);

                    //Map user group to device template group.
                    if (it.UserGroups != null && it.UserGroups.Any())
                    {
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                        List<Guid> groups = list2.Select(s => s.Id).ToList();
                        Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                        Guid[] removed = groups.Except(tmp).ToArray();
                        Guid[] added = tmp.Except(groups).ToArray();
                        if (added.Length != 0)
                        {
                            AddDeviceTemplateGroupToUserGroups(transaction, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveDeviceTemplateGroupFromUserGroups(transaction, it.Id, removed);
                        }
                    }
                    //Map device templates to device template group.
                    if (it.DeviceTemplates != null)
                    {
                        List<GXDeviceTemplate> list2 = await GetJoinedDeviceTemplate(User, it.Id);
                        List<Guid> groups = list2.Select(s => s.Id).ToList();
                        Guid[] tmp = it.DeviceTemplates.Select(s => s.Id).ToArray();
                        Guid[] removed = groups.Except(tmp).ToArray();
                        Guid[] added = tmp.Except(groups).ToArray();
                        if (added.Length != 0)
                        {
                            AddDeviceTemplateGroupsToDeviceTemplate(transaction, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveDeviceTemplateGroupsFromDeviceTemplate(transaction, it.Id, removed);
                        }
                    }
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
                await _eventsNotifier.DeviceTemplateGroupUpdate(it.Value,
                     new GXDeviceTemplateGroup[] {
                         new GXDeviceTemplateGroup()
                         {
                             Id = it.Key.Id,
                             Name = it.Key.Name
                         }
                     });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="DeviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupToUserGroups(IDbTransaction transaction, Guid DeviceTemplateGroupId, IEnumerable<Guid> groups)
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
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupFromUserGroups(IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupDeviceTemplateGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupDeviceTemplateGroup>(w => w.UserGroupId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }
        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupsToDeviceTemplate(IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
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
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupsFromDeviceTemplate(IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (var it in groups)
            {
                args.Where.Or<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }
    }
}