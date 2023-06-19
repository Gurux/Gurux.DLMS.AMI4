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
    public class AgentGroupRepository : IAgentGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgentGroupRepository(
            IGXHost host,
            IServiceProvider serviceProvider,
            IUserGroupRepository userGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _userGroupRepository = userGroupRepository;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        private async Task<List<GXUserGroup>> GetUserGroupsByAgentGroupId(Guid agentGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            arg.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == agentGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private List<GXAgent> GetAgentsByAgentGroupId(Guid agentGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAgent, GXAgentGroupAgent>(a => a.Id, b => b.AgentId);
            arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            arg.Where.And<GXAgentGroup>(where => where.Removed == null && where.Id == agentGroupId);
            return _host.Connection.Select<GXAgent>(arg);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByAgentGroup(ServerHelpers.GetUserId(user), groupId);
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
            GXSelectArgs args = GXQuery.GetUsersByAgentGroups(ServerHelpers.GetUserId(user), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs, bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.AgentGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXAgentGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXAgentGroup> list = _host.Connection.Select<GXAgentGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXAgentGroup, List<string>> updates = new Dictionary<GXAgentGroup, List<string>>();
            foreach (GXAgentGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAgentGroup>(it.Id));
                }
                else
                {
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXAgentGroup tmp = new GXAgentGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.AgentGroupDelete(it.Value, new GXAgentGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXAgentGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListAgentGroups? request,
            ListAgentGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agent groups.
                arg = GXSelectArgs.SelectAll<GXAgentGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If agent groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupAgentGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXAgentGroup>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgentGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXAgentGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXAgentGroup>(q => q.CreationTime);
            }
            GXAgentGroup[] groups = (await _host.Connection.SelectAsync<GXAgentGroup>(arg)).ToArray();
            if (response != null)
            {
                response.AgentGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXAgentGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXAgentGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetAgentGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXAgentGroup>(e => e.Agents);
            arg.Distinct = true;
            var group = await _host.Connection.SingleOrDefaultAsync<GXAgentGroup>(arg);
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get agents that belongs for this agent group.
            arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
            arg.Where.And<GXAgentGroupAgent>(q => q.Removed == null && q.AgentGroupId == id);
            group.Agents = await _host.Connection.SelectAsync<GXAgent>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this agent group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupAgentGroup>(q => q.Removed == null && q.AgentGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            ////////////////////////////////////////////////////
            //Get device groups.
            arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
            arg.Where.And<GXAgentGroupDeviceGroup>(q => q.Removed == null && q.AgentGroupId == id);
            group.DeviceGroups = await _host.Connection.SelectAsync<GXDeviceGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXAgentGroup> AgentGroups,
            Expression<Func<GXAgentGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXAgentGroup, List<string>> updates = new Dictionary<GXAgentGroup, List<string>>();
            foreach (GXAgentGroup it in AgentGroups)
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
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXAgentGroup>(e => new { e.Updated, e.UserGroups, e.Agents, e.DeviceGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    //Add user group to Agent group.
                    if (it.UserGroups != null)
                    {
                        AddAgentGroupToUserGroups(it.Id, it.UserGroups);
                    }
                    if (it.Agents != null)
                    {
                        AddAgentsToAgentGroup(it.Id, it.Agents);
                    }
                    if (it.DeviceGroups != null)
                    {
                        AddDeviceGroupsToAgentGroup(it.Id, it.DeviceGroups);
                    }
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXAgentGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXAgentGroup>(q => new { q.CreationTime, q.UserGroups, q.Agents, q.DeviceGroups });
                    _host.Connection.Update(args);
                    //Map user group to Agent group.
                    {
                        List<GXUserGroup> userGroups = await GetUserGroupsByAgentGroupId(it.Id);
                        var comparer = new UniqueComparer<GXUserGroup, Guid>();
                        List<GXUserGroup> removedUserGroups = userGroups.Except(it.UserGroups, comparer).ToList();
                        List<GXUserGroup> addedUserGroups = it.UserGroups.Except(userGroups, comparer).ToList();
                        if (removedUserGroups.Any())
                        {
                            RemoveAgentGroupFromUserGroups(it.Id, removedUserGroups);
                        }
                        if (addedUserGroups.Any())
                        {
                            AddAgentGroupToUserGroups(it.Id, addedUserGroups);
                        }
                    }
                    //Map agents to Agent group.
                    if (it.Agents != null)
                    {
                        List<GXAgent> agents = GetAgentsByAgentGroupId(it.Id);
                        var comparer = new UniqueComparer<GXAgent, Guid>();
                        List<GXAgent> removedAgents = agents.Except(it.Agents, comparer).ToList();
                        List<GXAgent> addedAgents = it.Agents.Except(agents, comparer).ToList();
                        if (removedAgents.Any())
                        {
                            RemoveAgentsFromAgentGroup(it.Id, removedAgents);
                        }
                        if (addedAgents.Any())
                        {
                            AddAgentsToAgentGroup(it.Id, addedAgents);
                        }
                    }
                    //Map device groups to agent group.
                    if (it.DeviceGroups != null)
                    {
                        List<GXDeviceGroup> deviceGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IDeviceGroupRepository deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                            deviceGroups = await deviceGroupRepository.GetDeviceGroupsByAgentId(User, it.Id);
                        }
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        List<GXDeviceGroup> removedDeviceGroups = deviceGroups.Except(it.DeviceGroups, comparer).ToList();
                        List<GXDeviceGroup> addedDeviceGroups = it.DeviceGroups.Except(deviceGroups, comparer).ToList();
                        if (removedDeviceGroups.Any())
                        {
                            RemoveDeviceGroupsFromAgentGroup(it.Id, removedDeviceGroups);
                        }
                        if (addedDeviceGroups.Any())
                        {
                            AddDeviceGroupsToAgentGroup(it.Id, addedDeviceGroups);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(User, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.AgentGroupUpdate(it.Value, new GXAgentGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map agent group to user groups.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">User groups where the agent is added.</param>
        public void AddAgentGroupToUserGroups(Guid agentGroupId, IEnumerable<GXUserGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupAgentGroup> list = new List<GXUserGroupAgentGroup>();
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupAgentGroup()
                {
                    AgentGroupId = agentGroupId,
                    UserGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agent group and user groups.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">User groups where the agent is removed.</param>
        public void RemoveAgentGroupFromUserGroups(Guid agentGroupId, IEnumerable<GXUserGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupAgentGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupAgentGroup>(w => w.UserGroupId == it.Id &&
                    w.AgentGroupId == agentGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map agents to agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Added Agents.</param>
        public void AddAgentsToAgentGroup(Guid agentGroupId, IEnumerable<GXAgent> agents)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupAgent> list = new List<GXAgentGroupAgent>();
            foreach (var it in agents)
            {
                list.Add(new GXAgentGroupAgent()
                {
                    AgentGroupId = agentGroupId,
                    AgentId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agents and agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Removed agents.</param>
        public void RemoveAgentsFromAgentGroup(Guid agentGroupId, IEnumerable<GXAgent> agents)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupAgent>();
            foreach (var it in agents)
            {
                args.Where.Or<GXAgentGroupAgent>(w => w.AgentId == it.Id &&
                    w.AgentGroupId == agentGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map agents to agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="agents">Device groups where the agent is added.</param>
        public void AddDeviceGroupsToAgentGroup(Guid agentGroupId, IEnumerable<GXDeviceGroup> agents)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupDeviceGroup> list = new List<GXAgentGroupDeviceGroup>();
            foreach (var it in agents)
            {
                list.Add(new GXAgentGroupDeviceGroup()
                {
                    AgentGroupId = agentGroupId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agents and agent group.
        /// </summary>
        /// <param name="agentGroupId">Agent group ID.</param>
        /// <param name="groups">Device groups where the agent is removed.</param>
        public void RemoveDeviceGroupsFromAgentGroup(Guid agentGroupId, IEnumerable<GXDeviceGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupDeviceGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXAgentGroupDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                    w.AgentGroupId == agentGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}