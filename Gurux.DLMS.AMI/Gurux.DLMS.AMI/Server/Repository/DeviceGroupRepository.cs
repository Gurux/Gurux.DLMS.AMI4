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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using System.Data;
using Gurux.DLMS.AMI.Client.Pages.Schedule;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DeviceGroupRepository : IDeviceGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IUserGroupRepository userGroupRepository,
        IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _userManager = userManager;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// Returns agent groups where device group belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="deviceGroupId">Device group ID</param>
        /// <returns>List of agent groups where device group id belongs.</returns>
        public List<GXAgentGroup> GetAgentGroupsByDeviceGroupId(ClaimsPrincipal User, Guid deviceGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupDeviceGroup>(a => a.Id, b => b.AgentGroupId);
            arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            arg.Where.And<GXDeviceGroup>(where => where.Removed == null && where.Id == deviceGroupId);
            return _host.Connection.Select<GXAgentGroup>(arg);
        }

        /// <inheritdoc />
        public async Task<List<GXDeviceGroup>> GetDeviceGroupsByDeviceId(ClaimsPrincipal user, Guid deviceId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            arg.Where.And<GXDevice>(where => where.Removed == null && where.Id == deviceId);
            return (await _host.Connection.SelectAsync<GXDeviceGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid deviceGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            arg.Where.And<GXDeviceGroup>(where => where.Removed == null && where.Id == deviceGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <inheritdoc />
        public List<GXDevice> GetDevicessByDeviceGroupId(ClaimsPrincipal user, Guid deviceGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceId);
            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXDeviceGroup>(where => where.Removed == null && where.Id == deviceGroupId);
            return _host.Connection.Select<GXDevice>(arg);
        }

        /// <inheritdoc />
        public async Task<List<GXAgentGroup>> GetJoinedAgentGroups(ClaimsPrincipal User, Guid deviceGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupDeviceGroup>(a => a.Id, b => b.AgentGroupId);
            arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            arg.Where.And<GXDeviceGroup>(where => where.Removed == null && where.Id == deviceGroupId);
            return (await _host.Connection.SelectAsync<GXAgentGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXDeviceGroup>> GetDeviceGroupsByAgentId(ClaimsPrincipal User, Guid agentGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXAgentGroupDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            arg.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == agentGroupId);
            return await _host.Connection.SelectAsync<GXDeviceGroup>(arg);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByDeviceGroup(ServerHelpers.GetUserId(User), groupId);
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
            GXSelectArgs args = GXQuery.GetUsersByDeviceGroups(ServerHelpers.GetUserId(User), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> userGroups,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.DeviceGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXDeviceGroup>(a => new { a.Id, a.Name }, q => userGroups.Contains(q.Id));
            List<GXDeviceGroup> list = _host.Connection.Select<GXDeviceGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceGroup, List<string>> updates = new Dictionary<GXDeviceGroup, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetUsersAsync(User, it.Id);
                    if (!delete)
                    {
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
                GXDeviceGroup tmp = new GXDeviceGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.DeviceGroupDelete(it.Value, new GXDeviceGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListDeviceGroups? request,
            ListDeviceGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the device groups.
                arg = GXSelectArgs.SelectAll<GXDeviceGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If device groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXDeviceGroup>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXDeviceGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDeviceGroup>(q => q.CreationTime);
            }
            GXDeviceGroup[] groups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToArray();
            if (response != null)
            {
                response.DeviceGroups = groups;
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXDeviceGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXDeviceGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXDeviceGroup>(e => e.Devices);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXDeviceGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get devices that belongs for this device group.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            arg.Where.And<GXDeviceGroupDevice>(q => q.Removed == null && q.DeviceGroupId == id);
            group.Devices = await _host.Connection.SelectAsync<GXDevice>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this device group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupDeviceGroup>(q => q.Removed == null && q.DeviceGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            ////////////////////////////////////////////////////
            //Get agent groups that belong for this device group.
            arg = GXSelectArgs.Select<GXAgentGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.AgentGroupId);
            arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
            arg.Where.And<GXAgentGroupDeviceGroup>(q => q.Removed == null && q.DeviceGroupId == id);
            group.AgentGroups = await _host.Connection.SelectAsync<GXAgentGroup>(arg);
            //TODO: Read parameters.
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXDeviceGroup> deviceGroups,
            Expression<Func<GXDeviceGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            string userId = null;
            bool isAdmin = true;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
                userId = _userManager.GetUserId(user);
            }
            List<string> users;
            List<Guid> list = new List<Guid>();
            Dictionary<GXDeviceGroup, List<string>> updates = new Dictionary<GXDeviceGroup, List<string>>();
            var newGroups = deviceGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = deviceGroups.Where(w => w.Id != Guid.Empty).ToList();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXDeviceGroup it in deviceGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    //Check the agents groups are already inserted.
                    if (it.AgentGroups != null && it.AgentGroups.Where(w => w.Id == Guid.Empty).Any())
                    {
                        throw new ArgumentException(Properties.Resources.JoinedGroupsAreNotInserted);
                    }
                    //Check the devices are already inserted.
                    if (it.Devices != null && it.Devices.Where(w => w.Id == Guid.Empty).Any())
                    {
                        throw new ArgumentException(Properties.Resources.JoinedGroupsAreNotInserted);
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
                }
                if (newGroups.Any())
                {
                    foreach (var it in newGroups)
                    {
                        it.CreationTime = now;
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXDeviceGroup>(e => new
                    {
                        e.Updated,
                        e.Removed,
                    });
                    await _host.Connection.InsertAsync(transaction, args);
                    foreach (var it in newGroups)
                    {
                        list.Add(it.Id);
                    }
                    var first = newGroups.First();
                    updates[first] = await GetUsersAsync(user, first.Id);
                }
                foreach (var it in updatedGroups)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXAgentGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    users = await GetUsersAsync(user, it.Id);
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXDeviceGroup>(q => new { q.UserGroups, q.CreationTime, q.Devices, q.AgentGroups });
                    _host.Connection.Update(args);
                    //Map user group to device group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddDeviceGroupToUserGroups(transaction, it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveDeviceGroupFromUserGroups(transaction, it.Id, removed);
                    }
                    //Map devices to device group.
                    if (it.Devices != null)
                    {
                        List<GXDevice> devices = GetDevicessByDeviceGroupId(user, it.Id);
                        var comparer2 = new UniqueComparer<GXDevice, Guid>();
                        List<GXDevice> removedDevices = devices.Except(it.Devices, comparer2).ToList();
                        List<GXDevice> addedDevices = it.Devices.Except(devices, comparer2).ToList();
                        if (removedDevices.Any())
                        {
                            RemoveDevicesFromDeviceGroup(transaction, it.Id, removedDevices);
                        }
                        if (addedDevices.Any())
                        {
                            AddDevicesToDeviceGroup(transaction, it.Id, addedDevices);
                        }
                    }
                    //Map agent groups to device groups.
                    if (it.AgentGroups != null)
                    {
                        List<GXAgentGroup> agentGroups = GetAgentGroupsByDeviceGroupId(user, it.Id);
                        var comparer2 = new UniqueComparer<GXAgentGroup, Guid>();
                        List<GXAgentGroup> removedAgentGroups = agentGroups.Except(it.AgentGroups, comparer2).ToList();
                        List<GXAgentGroup> addedAgentGroups = it.AgentGroups.Except(agentGroups, comparer2).ToList();
                        if (removedAgentGroups.Any())
                        {
                            RemoveAgenGroupsFromDeviceGroup(transaction, it.Id, removedAgentGroups);
                        }
                        if (addedAgentGroups.Any())
                        {
                            AddAgentGroupsToDeviceGroup(transaction, it.Id, addedAgentGroups);
                        }
                    }
                    updates[it] = users;
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
                await _eventsNotifier.DeviceGroupUpdate(it.Value, new GXDeviceGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map device group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Group IDs of the device groups where the device is added.</param>
        public void AddDeviceGroupToUserGroups(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupDeviceGroup> list = new List<GXUserGroupDeviceGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupDeviceGroup()
                {
                    DeviceGroupId = deviceGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between device group and user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Group IDs of the device groups where the device is removed.</param>
        public void RemoveDeviceGroupFromUserGroups(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<Guid> groups)
        {
            foreach (var ug in groups)
            {
                _host.Connection.Delete(transaction, GXDeleteArgs.Delete<GXUserGroupDeviceGroup>(w => w.UserGroupId == ug && w.DeviceGroupId == deviceGroupId));
            }
        }

        /// <summary>
        /// Map devices to device group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Group IDs of the device groups where the device is added.</param>
        public void AddDevicesToDeviceGroup(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXDevice> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceGroupDevice> list = new List<GXDeviceGroupDevice>();
            foreach (var it in groups)
            {
                list.Add(new GXDeviceGroupDevice()
                {
                    DeviceGroupId = deviceGroupId,
                    DeviceId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between devices and device group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Device groups where the device is removed.</param>
        public void RemoveDevicesFromDeviceGroup(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXDevice> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXDeviceGroupDevice>();
            foreach (var it in groups)
            {
                args.Where.Or<GXDeviceGroupDevice>(w => w.DeviceId == it.Id &&
                    w.DeviceGroupId == deviceGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map agent groups to device group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Agent groups where the agent is added.</param>
        public void AddAgentGroupsToDeviceGroup(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXAgentGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupDeviceGroup> list = new List<GXAgentGroupDeviceGroup>();
            foreach (var it in groups)
            {
                list.Add(new GXAgentGroupDeviceGroup()
                {
                    DeviceGroupId = deviceGroupId,
                    AgentGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between device groups and agent groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceGroupId">Device group ID.</param>
        /// <param name="groups">Agent groups where the agent is removed.</param>
        public void RemoveAgenGroupsFromDeviceGroup(IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXAgentGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupDeviceGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXAgentGroupDeviceGroup>(w => w.AgentGroupId == it.Id &&
                    w.DeviceGroupId == deviceGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }
    }
}
