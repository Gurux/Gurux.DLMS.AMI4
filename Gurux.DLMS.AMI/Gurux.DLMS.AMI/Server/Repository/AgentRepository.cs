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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Linq.Expressions;
using System.Diagnostics;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class AgentRepository : IAgentRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IAgentGroupRepository _agentGroupRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgentRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IAgentGroupRepository agentGroupRepository,
            IServiceProvider serviceProvider,
            ITaskRepository taskRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _agentGroupRepository = agentGroupRepository;
            _taskRepository = taskRepository;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            Guid? agentId)
        {
            GXSelectArgs args = GXQuery.GetUsersByAgent(ServerHelpers.GetUserId(User), agentId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByAgents(ServerHelpers.GetUserId(User), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <summary>
        /// Returns agent groups where agent belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="agentId">Agent ID</param>
        /// <returns>List of agent groups where agent belongs.</returns>
        private async Task<List<GXAgentGroup>> GetAgentGroupsByAgentId(
            ClaimsPrincipal User,
            Guid agentId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(a => a.Id, b => b.AgentGroupId);
            arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(a => a.AgentId, b => b.Id);
            arg.Where.And<GXAgent>(where => where.Removed == null && where.Id == agentId);
            return (await _host.Connection.SelectAsync<GXAgentGroup>(arg));
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> agentrs)
        {
            if (User != null && !User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.AgentManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXAgent>(a => a.Id, q => agentrs.Contains(q.Id));
            List<GXAgent> list = _host.Connection.Select<GXAgent>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXAgent, List<string>> updates = new Dictionary<GXAgent, List<string>>();
            foreach (GXAgent it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXAgent tmp = new GXAgent() { Id = it.Key.Id };
                await _eventsNotifier.AgentDelete(it.Value, new GXAgent[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXAgent[]> ListAsync(
            ClaimsPrincipal User,
            ListAgents? request,
            ListAgentsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agents.
                arg = GXSelectArgs.SelectAll<GXAgent>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentsByUser(userId, null);
            }
            arg.Where.And<GXAgent>(w => w.Template == false);
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            arg.Descending = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgent>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.OrderBy.Add<GXAgent>(q => q.CreationTime);
            GXAgent[] agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToArray();
            if (response != null)
            {
                response.Agents = agents;
                if (response.Count == 0)
                {
                    response.Count = agents.Length;
                }
            }
            return agents;
        }

        /// <inheritdoc />
        public async Task<GXAgent[]> ListInstallersAsync(
            ClaimsPrincipal User,
            ListAgentInstallers? request,
            ListAgentInstallersResponse? response)
        {
            bool isAdmin = true;
            if (User != null)
            {
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (isAdmin || User == null)
            {
                //Admin can see all the agent installers.
                arg = GXSelectArgs.SelectAll<GXAgent>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentsByUser(userId, null);
            }
            arg.Distinct = true;
            arg.Descending = true;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Versions != null && request.Filter.Versions.Any())
                {
                    string? number = request.Filter.Versions[0].Number;
                    if (!string.IsNullOrEmpty(number))
                    {
                        arg.Where.And<GXAgentVersion>(w => w.Number.Contains(number));
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Where.And<GXAgent>(w => w.Template == true);
            arg.Joins.AddInnerJoin<GXAgent, GXAgentVersion>(j => j.Id, j => j.Agent);
            arg.Columns.Add<GXAgentVersion>();
            arg.Columns.Exclude<GXAgentVersion>(e => e.Agent);
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgent>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.OrderBy.Add<GXAgent>(q => q.CreationTime);
            arg.OrderBy.Add<GXAgentVersion>(q => q.CreationTime);
            GXAgent[] agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToArray();
            if (response != null)
            {
                response.Agents = agents;
                if (response.Count == 0)
                {
                    response.Count = agents.Length;
                }
            }
            return agents;
        }


        /// <inheritdoc />
        public async Task<GXAgent> ReadAsync(
            ClaimsPrincipal User,
            Guid id)
        {
            bool isAdmin = false;
            if (User != null)
            {
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (User == null || isAdmin)
            {
                //Admin can see all the agents.
                arg = GXSelectArgs.SelectAll<GXAgent>(w => w.Id == id);
                arg.Joins.AddInnerJoin<GXAgent, GXAgentGroupAgent>(x => x.Id, y => y.AgentId);
                arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
            }
            arg.Columns.Add<GXAgentGroup>();
            arg.Columns.Exclude<GXAgentGroup>(e => e.Agents);
            arg.Distinct = true;
            GXAgent agent = await _host.Connection.SingleOrDefaultAsync<GXAgent>(arg);
            if (agent == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            agent.Versions = await GetAgentVersionsByAgentIdAsync(User, agent);
            return agent;
        }

        private async Task<List<GXAgentVersion>> GetAgentVersionsByAgentIdAsync(ClaimsPrincipal user, GXAgent agent)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentVersion>(w => w.Agent == agent);
            arg.Distinct = true;
            arg.OrderBy.Add<GXAgentVersion>(o => o.Number);
            arg.Descending = true;
            return await _host.Connection.SelectAsync<GXAgentVersion>(arg);
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXAgent> agents)
        {
            return await UpdateAsync(user, agents, null);
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXAgent> agents,
            Expression<Func<GXAgent, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXAgent, List<string>> updates = new Dictionary<GXAgent, List<string>>();
            foreach (GXAgent agent in agents)
            {
                if (string.IsNullOrEmpty(agent.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (agent.Id == Guid.Empty)
                {
                    if (!agent.AgentGroups.Any())
                    {
                        ListAgentGroups request = new ListAgentGroups() { Filter = new GXAgentGroup() { Default = true } };
                        GXAgentGroup[] groups = await _agentGroupRepository.ListAsync(user, request, null, CancellationToken.None);
                        agent.AgentGroups.AddRange(groups);
                    }
                    if (!agent.AgentGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    if (agent.ReaderSettings == null)
                    {
                        agent.ReaderSettings = JsonSerializer.Serialize(new ReaderSettings());
                    }
                    if (agent.ListenerSettings == null)
                    {
                        agent.ListenerSettings = JsonSerializer.Serialize(new ListenerSettings());
                    }
                    if (agent.NotifySettings == null)
                    {
                        agent.NotifySettings = JsonSerializer.Serialize(new NotifySettings());
                    }
                    agent.CreationTime = now;
                    agent.Creator = creator;
                    GXInsertArgs args = GXInsertArgs.Insert(agent);
                    args.Exclude<GXAgent>(q => new { q.Updated, q.AgentGroups, q.Removed });
                    _host.Connection.Insert(args);
                    list.Add(agent.Id);
                    AddAgentToAgentGroups(agent.Id, agent.AgentGroups);
                }
                else
                {
                    if (!agent.AgentGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    GXSelectArgs m = GXSelectArgs.Select<GXAgent>(q => q.ConcurrencyStamp, where => where.Id == agent.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != agent.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    agent.Updated = now;
                    agent.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(agent, columns);
                    args.Exclude<GXAgent>(q => new { q.CreationTime, q.AgentGroups, q.Versions, q.Creator });
                    _host.Connection.Update(args);
                    //Map agent groups to agent.
                    List<GXAgentGroup> agentGroups = await GetAgentGroupsByAgentId(user, agent.Id);
                    var comparer = new UniqueComparer<GXAgentGroup, Guid>();
                    List<GXAgentGroup> removedAgentGroups = agentGroups.Except(agent.AgentGroups, comparer).ToList();
                    List<GXAgentGroup> addedAgentGroups = agent.AgentGroups.Except(agentGroups, comparer).ToList();
                    if (removedAgentGroups.Any())
                    {
                        RemoveAgentsFromAgentGroup(agent.Id, removedAgentGroups);
                    }
                    if (addedAgentGroups.Any())
                    {
                        AddAgentToAgentGroups(agent.Id, addedAgentGroups);
                    }

                    //Update agent versions.
                    List<GXAgentVersion> agentVersions = await GetAgentVersionsByAgentIdAsync(user, agent);
                    var comparer1 = new UniqueComparer<GXAgentVersion, Guid>();
                    List<GXAgentVersion> addedAgentVersions = agent.Versions.Except(agentVersions, comparer1).ToList();
                    if (addedAgentVersions.Any())
                    {
                        AddAgentVersionToAgent(agent, addedAgentVersions);
                    }
                }
                updates[agent] = await GetUsersAsync(user, agent.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.AgentUpdate(it.Value, new GXAgent[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add agent version to agent.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="versions">Added agent versions.</param>
        private void AddAgentVersionToAgent(GXAgent agent, IEnumerable<GXAgentVersion> versions)
        {
            DateTime now = DateTime.Now;
            foreach (GXAgentVersion it in versions)
            {
                it.Agent = agent;
                it.CreationTime = now;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(versions));
        }

        /// <summary>
        /// Map agent group to user groups.
        /// </summary>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="groups">Agent groups where the agent is added.</param>
        private void AddAgentToAgentGroups(Guid agentId, IEnumerable<GXAgentGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupAgent> list = new List<GXAgentGroupAgent>();
            foreach (GXAgentGroup it in groups)
            {
                list.Add(new GXAgentGroupAgent()
                {
                    AgentId = agentId,
                    AgentGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between agent group and agent.
        /// </summary>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="groups">Agent groups where the agent is removed.</param>
        private void RemoveAgentsFromAgentGroup(Guid agentId, IEnumerable<GXAgentGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXAgentGroupAgent> list = new List<GXAgentGroupAgent>();
            foreach (var it in groups)
            {
                list.Add(new GXAgentGroupAgent()
                {
                    AgentId = agentId,
                    AgentGroupId = it.Id,
                    Removed = now
                });
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(list));
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(ClaimsPrincipal User, Guid agentId, AgentStatus status)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, where => where.Id == agentId && where.Removed == null);
            GXAgent agent = await _host.Connection.SingleOrDefaultAsync<GXAgent>(args);
            if (agent == null)
            {
                throw new UnauthorizedAccessException();
            }
            agent.Status = status;
            agent.Detected = DateTime.Now;
            GXUpdateArgs update = GXUpdateArgs.Update(agent, c => new { c.Status, c.Detected });
            await _host.Connection.UpdateAsync(update);
            //Only part of the agent properties are send.
            GXAgent tmp = new GXAgent()
            {
                Id = agent.Id,
                Name = agent.Name,
                Detected = agent.Detected,
                Status = agent.Status
            };
            await _eventsNotifier.AgentStatusChange(await GetUsersAsync(User, agent.Id), new GXAgent[] { tmp });
            if (status == AgentStatus.Connected || status == AgentStatus.Offline)
            {
                //Update agent state for the agent log.
                GXAgentLog log = new GXAgentLog(TraceLevel.Info);
                log.Agent = agent;
                if (status == AgentStatus.Connected)
                {
                    log.Message = Properties.Resources.Connected;
                }
                else if (status == AgentStatus.Offline)
                {
                    log.Message = Properties.Resources.Offline;
                }
                IAgentLogRepository repository = _serviceProvider.GetRequiredService<IAgentLogRepository>();
                await repository.AddAsync(User, new GXAgentLog[] { log });
                //Reset running tasks if agent connects or disconnects.
                args = GXSelectArgs.SelectAll<GXTask>(where => where.Start != null && where.Ready == null);
                args.Joins.AddInnerJoin<GXTask, GXAgent>(j => j.OperatingAgent, j => j.Id);
                args.Where.And<GXAgent>(q => q.Id == agentId);
                List<GXTask> tasks = await _host.Connection.SelectAsync<GXTask>(args);
                await _taskRepository.RestartAsync(User, tasks);
            }
        }

        /// <inheritdoc />
        public async Task UpgradeAsync(ClaimsPrincipal User, IEnumerable<GXAgent> agents)
        {
            foreach (var it in agents)
            {
                GXAgent agent = await ReadAsync(User, it.Id);
                if (agent == null)
                {
                    throw new UnauthorizedAccessException();
                }
                if (string.IsNullOrEmpty(it.UpdateVersion))
                {
                    throw new ArgumentException("Invalid agent version to update.");
                }
                agent.Updated = DateTime.Now;
                GXUpdateArgs update = GXUpdateArgs.Update(agent, c => new { c.UpdateVersion, c.Updated });
                await _host.Connection.UpdateAsync(update);
                //Only part of the agent properties are send.
                GXAgent tmp = new GXAgent()
                {
                    Id = agent.Id,
                    Updated = agent.Updated,
                    UpdateVersion = agent.UpdateVersion,
                    Status = agent.Status
                };
                await _eventsNotifier.AgentStatusChange(await GetUsersAsync(User, agent.Id), new GXAgent[] { tmp });
            }
        }
    }
}
