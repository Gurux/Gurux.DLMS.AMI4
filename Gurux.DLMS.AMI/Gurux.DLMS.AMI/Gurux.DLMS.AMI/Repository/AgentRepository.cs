// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class AgentRepository : IAgentRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IAgentGroupRepository _agentGroupRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AgentRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService,
            IUserRepository userRepository,
            IAgentGroupRepository agentGroupRepository,
            IServiceProvider serviceProvider,
            ITaskRepository taskRepository,
            IEnumTypeRepository enumTypeRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _agentGroupRepository = agentGroupRepository;
            _taskRepository = taskRepository;
            _serviceProvider = serviceProvider;
            _notificationCache = notificationCache;
            _enumTypeRepository = enumTypeRepository;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAgent), id, action, GXAgentPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? agentId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAgent(s => s.Id!, ServerHelpers.GetUserId(User), agentId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (agentId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXAgent), agentId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAgents(s => s.Id!, ServerHelpers.GetUserId(User), agentIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <summary>
        /// Returns agent groups where agent belongs.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="agentId">Agent ID</param>
        /// <returns>List of agent groups where agent belongs.</returns>
        private static async Task<List<GXAgentGroup>> GetAgentGroupsByAgentId(GXDbConnection connection, Guid agentId)
        {
            GXSelectArgs arg = GXQuery.GetAgentGroupsByAgent(s => s.Id, agentId);
            return (await connection.SelectAsync<GXAgentGroup>(arg));
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> agents, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            agents = agents.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXAgent, Guid>(_dbFactory, User, _serviceProvider, agents,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.Delete);
            agents.ValidateIds();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            IEnumerable<GXAgent> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, w => agents.Contains(w.Id));
                exists = connection.Select<GXAgent>(transaction, arg, cancellationToken);
            }
            else
            {
                //Check that the user has access rights to the removed agents.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetAgentsByUser(s => new { s.Id, s.Name }, userId, null,
                   DateTimeOffset.MinValue, null, agents);
                exists = connection.Select<GXAgent>(transaction, arg, cancellationToken);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", agents));
            }
            DateTime now = DateTime.Now;
            try
            {
                foreach (GXAgent it in exists)
                {
                    if (!agents.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "agent", [it.Id], cancellationToken);
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    _notificationCache.Add(users, new GXTarget(GXAgentPolicies.Delete, it.Id, it.Name));
                    if (!delete)
                    {
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, q => q.Removed!), cancellationToken);
                    }
                    else
                    {
                        var args = GXDeleteArgs.Delete<GXTask>(w => w.OperatingAgent == it);
                        await connection.DeleteAsync(transaction, args, cancellationToken);
                    }
                }
                if (delete)
                {
                    GXDeleteArgs args = GXDeleteArgs.Delete<GXAgentGroupAgent>(w => agents.Contains(w.AgentId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(agents, delete, notify, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXAgent>> ListAsync(
            ListAgents? request,
            ListAgentsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the agents.
                arg = GXSelectArgs.SelectAll<GXAgent>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetAgentsByUser(s => "*", userId, null);
            }
            arg.Columns.Exclude<GXAgent>(e => new { e.ListenerSettings, e.NotifySettings, e.RawNotificationSettings, e.ReaderSettings, e.Template });
            arg.Where.And<GXAgent>(w => w.Template == false);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXAgent>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXAgent>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXAgent>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgent>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXAgent>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXAgent>(q => q.CreationTime!);
            }
            var agents = (await connection.SelectAsync<GXAgent>(arg));
            await PopulateAvailableVersionAsync(connection, agents);
            if (response != null)
            {
                response.Agents = agents;
                if (response.Count == 0)
                {
                    response.Count = agents.Count;
                }
            }
            return agents;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXAgent>> ListInstallersAsync(
            ListAgentInstallers? request,
            bool includeRemoved,
            ListAgentInstallersResponse? response,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.View);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            //Admin can see all the agent installers. Installers are not part of any agent group.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>();
            arg.Distinct = true;
            arg.Descending = true;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Versions != null && request.Filter.Versions.Count != 0)
                {
                    GXAgentVersion ver = request.Filter.Versions.First();
                    if (ver.Removed == null)
                    {
                        arg.Where.And<GXAgentVersion>(w => w.Removed == null);
                    }
                    else
                    {
                        includeRemoved = true;
                        arg.Where.And<GXAgentVersion>(w => w.Removed != null);
                    }
                    string? number = ver.Number;
                    if (!string.IsNullOrEmpty(number))
                    {
                        arg.Where.And<GXAgentVersion>(w => w.Number!.Contains(number));
                    }
                    request.Filter.Versions = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Where.And<GXAgent>(w => w.Template == true);
            arg.Joins.AddInnerJoin<GXAgent, GXAgentVersion>(j => j.Id, j => j.Agent!);
            if (!includeRemoved)
            {
                //If removed agents are also included.
                arg.Where.And<GXAgentVersion>(w => w.Removed == null);
            }
            arg.Columns.Add<GXAgentVersion>();
            arg.Columns.Exclude<GXAgentVersion>(e => e.Agent!);
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXAgent>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAgent>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            arg.OrderBy.Add<GXAgent>(q => q.CreationTime!);
            arg.OrderBy.Add<GXAgentVersion>(q => q.CreationTime!);
            GXAgent[] agents = [.. (await connection.SelectAsync<GXAgent>(arg))];
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
        public async Task<GXAgent> ReadAsync(Guid id, Expression<Func<GXAgent, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.View);
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
                    arg.Where.And<GXAgentGroup>(w => w.Removed == null);
                    arg.Where.And<GXAgentGroupAgent>(w => w.Removed == null);
                    //Agent installers are not part of any group.
                    arg.Joins.AddLeftJoin<GXAgent, GXAgentGroupAgent>(x => x.Id, y => y.AgentId);
                    arg.Joins.AddLeftJoin<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
                    arg.Columns.Add<GXAgentGroup>();
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetAgentsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXAgentGroup>(e => e.Agents!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXAgent? agent = await connection.SingleOrDefaultAsync<GXAgent>(arg) ?? throw new GXAmiNotFoundException(Properties.Resources.Agent + " " + Properties.Resources.Id + " " + id.ToString());
                agent.Versions = await GetAgentVersionsByAgentIdAsync(connection, agent);
                await PopulateAvailableVersionAsync(connection, [agent]);
                agent.AgentGroups ??= [];
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAgent, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXAgent>(w => w.Id == id);
                agent.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return agent;

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await ReadAsync(id, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task PopulateAvailableVersionAsync(GXDbConnection connection, IEnumerable<GXAgent> agents)
        {
            var query = GXSelectArgs.Select<GXAgent>(a => new { a.Id, a.AvailableVersion },
                a => a.Template == true && a.CatalogId == "Gurux.DLMS.AMI.Agent");
            var installer = await connection.SingleOrDefaultAsync<GXAgent>(query);
            foreach (var agent in agents)
            {
                if (agent.Template != true)
                {
                    agent.AvailableVersion = installer?.AvailableVersion;
                }
            }
        }

        private static async Task<List<GXAgentVersion>> GetAgentVersionsByAgentIdAsync(GXDbConnection connection, GXAgent agent)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentVersion>(w => w.Agent == agent);
            arg.Distinct = true;
            arg.OrderBy.Add<GXAgentVersion>(o => o.Number!);
            arg.Descending = true;
            return await connection.SelectAsync<GXAgentVersion>(arg);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAgent> agents,
            Expression<Func<GXAgent, object>>? columns,
            CancellationToken cancellationToken)
        {
            agents = agents.ToArray();
            return await GXHistoryMutation.RunAsync<GXAgent, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, agents,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.Add, GXAgentPolicies.Edit);
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                DateTime now = DateTime.Now;
                Dictionary<GXAgent, NotificationAction> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXAgent agent in agents)
                {
                    if (string.IsNullOrEmpty(agent.Name) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXAgent.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    var action = agent.Id == Guid.Empty ? NotificationAction.Add : NotificationAction.Edit;
                    if (agent.Id == Guid.Empty)
                    {
                        //Agent installers are not part of any group.
                        if (!agent.Template.GetValueOrDefault(false))
                        {
                            if (agent.AgentGroups == null || agent.AgentGroups.Count == 0)
                            {
                                agent.AgentGroups = [];
                                ListAgentGroups request = new() { Filter = new GXAgentGroup() { Default = true } };
                                IEnumerable<GXAgentGroup> groups = await _agentGroupRepository.ListAsync(request, cancellationToken: cancellationToken);
                                agent.AgentGroups.AddRange(groups);
                            }
                            if (agent.AgentGroups.Count == 0)
                            {
                                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                            }
                            agent.ReaderSettings ??= JsonSerializer.Serialize(new ReaderSettings());
                            agent.ListenerSettings ??= JsonSerializer.Serialize(new ListenerSettings());
                            agent.NotifySettings ??= JsonSerializer.Serialize(new NotifySettings());
                            agent.RawNotificationSettings ??= JsonSerializer.Serialize(new RawNotificationSettings());
                        }
                        agent.CreationTime = now;
                        agent.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(agent);
                        args.Exclude<GXAgent>(q => new
                        {
                            q.Updated,
                            q.AgentGroups,
                            q.Removed
                        });
                        connection.Insert(args);
                        AddAgentToAgentGroups(connection, agent.Id, agent.AgentGroups);
                        await _enumTypeRepository.GetLogTypeAsync(TargetType.Agent, CrudAction.Create, cancellationToken);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXAgent>(q => q.ConcurrencyStamp!, where => where.Id == agent.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != agent.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        agent.Updated = now;
                        agent.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(agent, columns);
                        args.Exclude<GXAgent>(q => new
                        {
                            q.CreationTime,
                            q.AgentGroups,
                            q.Versions,
                            q.Creator
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                          agent.Creator == null ||
                          string.IsNullOrEmpty(agent.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXAgent>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map agent groups to agent.
                        if (agent.AgentGroups != null && agent.AgentGroups.Count != 0)
                        {
                            List<GXAgentGroup> agentGroups = await GetAgentGroupsByAgentId(connection, agent.Id);
                            var comparer = new UniqueComparer<GXAgentGroup, Guid>();
                            List<GXAgentGroup> removedAgentGroups = [.. agentGroups.Except(agent.AgentGroups, comparer)];
                            List<GXAgentGroup> addedAgentGroups = [.. agent.AgentGroups.Except(agentGroups, comparer)];
                            if (removedAgentGroups.Count != 0)
                            {
                                RemoveAgentsFromAgentGroup(connection, agent.Id, removedAgentGroups);
                            }
                            if (addedAgentGroups.Count != 0)
                            {
                                AddAgentToAgentGroups(connection, agent.Id, addedAgentGroups);
                            }
                        }
                        //Update agent versions.
                        if (agent.Versions != null)
                        {
                            List<GXAgentVersion> agentVersions = await GetAgentVersionsByAgentIdAsync(connection, agent);
                            var comparer1 = new UniqueComparer<GXAgentVersion, Guid>();
                            List<GXAgentVersion> addedAgentVersions = [.. agent.Versions.Except(agentVersions, comparer1)];
                            List<GXAgentVersion> removedAgentVersions = [.. agentVersions.Except(agent.Versions, comparer1)];
                            if (addedAgentVersions.Count != 0)
                            {
                                AddAgentVersionToAgent(connection, agent, addedAgentVersions);
                            }
                            if (removedAgentVersions.Count != 0)
                            {
                                RemoveAgentVersionFromAgent(connection, agent, removedAgentVersions);
                            }
                        }
                        await _enumTypeRepository.GetLogTypeAsync(TargetType.Agent, CrudAction.Update, cancellationToken);
                    }
                    updates[agent] = action;
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, it.Value, connection),
                        new GXTarget(it.Value == NotificationAction.Add ? GXAgentPolicies.Add : GXAgentPolicies.Edit,
                            it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                return [.. agents.Select(s => s.Id)];

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(agents, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Add agent version to agent.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="agent">Agent.</param>
        /// <param name="versions">Added agent versions.</param>
        private static void AddAgentVersionToAgent(GXDbConnection connection,
            GXAgent agent, IEnumerable<GXAgentVersion> versions)
        {
            DateTime now = DateTime.Now;
            foreach (GXAgentVersion it in versions)
            {
                it.Agent = agent;
                it.CreationTime = now;
            }
            connection.Insert(GXInsertArgs.InsertRange(versions));
        }

        /// <summary>
        /// Remove agent version from agent.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="agent">Agent.</param>
        /// <param name="versions">Removed agent versions.</param>
        private static void RemoveAgentVersionFromAgent(GXDbConnection connection,
            GXAgent agent,
            IEnumerable<GXAgentVersion> versions)
        {
            _ = agent;
            DateTime now = DateTime.Now;
            foreach (GXAgentVersion it in versions)
            {
                it.Removed = now;
                connection.Update(GXUpdateArgs.Update(it, c => c.Removed!));
            }
        }

        /// <summary>
        /// Map agent group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="groups">Agent groups where the agent is added.</param>
        private static void AddAgentToAgentGroups(GXDbConnection connection, Guid agentId, IEnumerable<GXAgentGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                DateTime now = DateTime.Now;
                List<GXAgentGroupAgent> list = [];
                foreach (GXAgentGroup it in groups)
                {
                    list.Add(new GXAgentGroupAgent()
                    {
                        AgentId = agentId,
                        AgentGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between agent group and agent.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="agentId">Agent ID.</param>
        /// <param name="groups">Agent groups where the agent is removed.</param>
        private static void RemoveAgentsFromAgentGroup(GXDbConnection connection, Guid agentId, IEnumerable<GXAgentGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                foreach (var it in groups)
                {
                    connection.Delete(GXDeleteArgs.Delete<GXAgentGroupAgent>(w => w.AgentId == agentId && w.AgentGroupId == it.Id));
                }
            }
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(Guid agentId,
            string? connectionInfo,
            AgentStatus status,
            string? data,
            CancellationToken cancellationToken)
        {
            await UpdateStatusCoreAsync(agentId, connectionInfo, status, data, cancellationToken, true, User);
        }

        // Only authenticated, membership-validated hub registrations use this server-owned transition.
        internal Task UpdateConnectionStatusAsync(Guid agentId, string? connectionInfo, bool connected)
            => UpdateStatusCoreAsync(agentId, connectionInfo, connected ? AgentStatus.Connected : AgentStatus.Offline,
                null, CancellationToken.None, false, connected ? User : new ClaimsPrincipal());

        private async Task UpdateStatusCoreAsync(Guid agentId, string? connectionInfo, AgentStatus status,
            string? data, CancellationToken cancellationToken, bool authorize, ClaimsPrincipal actor)
        {
            if (authorize)
                {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.Edit);
            }
            GXSelectArgs args = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name, s.TraceLevel, s.Status }, where => where.Id == agentId && where.Removed == null);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXAgent? agent = await connection.SingleOrDefaultAsync<GXAgent>(args) ?? throw new GXAmiNotFoundException(Properties.Resources.Agent + " " + Properties.Resources.Id + " " + agentId.ToString());
            var previousStatus = agent.Status;
            agent.Status = status;
            agent.Detected = DateTime.Now;
            agent.ConnectionInfo = connectionInfo;
            GXUpdateArgs update;
            if (status == AgentStatus.SerialPortChange)
            {
                //Serial port is added or removed for the agent.
                agent.SerialPorts = data;
                update = GXUpdateArgs.Update(agent, c => new { c.Status, c.Detected, c.SerialPorts, c.ConnectionInfo });
            }
            else
            {
                update = GXUpdateArgs.Update(agent, c => new { c.Status, c.Detected, c.ConnectionInfo });
            }
            await connection.UpdateAsync(update, cancellationToken);
            if (previousStatus != status && (status == AgentStatus.Connected || status == AgentStatus.Offline))
                {
                _serviceProvider.GetRequiredService<IWorkflowHandler>().Publish(new GXWorkflowEventContext
                {
                    EventCode = status == AgentStatus.Connected ? "agent.connected" : "agent.disconnected",
                    TargetId = agent.Id.ToString(),
                    OperationId = Guid.NewGuid(),
                    OccurredAt = DateTimeOffset.UtcNow,
                    Outcome = "success"
                }, actor);
            }
            //Only part of the agent properties are send.
            GXAgent tmp = new()
            {
                Id = agent.Id,
                Name = agent.Name,
                Detected = agent.Detected,
                ConnectionInfo = connectionInfo,
                Status = agent.Status
            };
            _notificationCache.Add(await GetNotifiedUsers(agent.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXAgentPolicies.Edit, agent.Id, agent.Name));
            //Update enum type.
            await _enumTypeRepository.GetLogTypeAsync(TargetType.AgentStatus, status, cancellationToken);
            if (agent.TraceLevel > TraceLevel.Warning)
            {
                //Update agent state for the agent log.
                GXLog? log = new(TraceLevel.Info)
                {
                    Creator = agent.Id,
                    Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, status, cancellationToken)
                };
                switch (status)
                {
                    case AgentStatus.Connected:
                        log.Message = Properties.Resources.Connected + " " + connectionInfo;
                        break;
                    case AgentStatus.Offline:
                        log.Message = Properties.Resources.Offline + " " + connectionInfo;
                        break;
                    case AgentStatus.Error:
                        log.Message = Properties.Resources.Error;
                        break;
                    case AgentStatus.Downloading:
                        log.Message = Properties.Resources.Downloading;
                        break;
                    case AgentStatus.Updating:
                        log.Message = Properties.Resources.Updating;
                        break;
                    case AgentStatus.Restarting:
                        log.Message = Properties.Resources.Restarting + " " + connectionInfo;
                        break;
                    case AgentStatus.SerialPortChange:
                        log.Message = "Serial ports changed.";
                        break;
                    default:
                        log = null;
                        break;
                }
                if (log != null)
                {
                    //Add agent log. Idle status is not logged.
                    ILogRepository repository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("agent");
                    await repository.AddAsync("Status", [log], cancellationToken);
                }
            }
            if (status == AgentStatus.Connected || status == AgentStatus.Offline)
            {
                //Reset running tasks if agent connects or disconnects.
                args = GXSelectArgs.SelectAll<GXTask>(where => where.Start != null && where.Ready == null);
                args.Joins.AddInnerJoin<GXTask, GXAgent>(j => j.OperatingAgent!, j => j.Id);
                args.Where.And<GXAgent>(q => q.Id == agentId);
                List<GXTask> tasks = await connection.SelectAsync<GXTask>(args);
                await _taskRepository.RestartAsync(tasks, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task UpgradeAsync(IEnumerable<GXAgent> agents,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAgentPolicies.Add, GXAgentPolicies.Edit);
            foreach (var it in agents)
            {
                GXAgent agent = await ReadAsync(it.Id, cancellationToken: cancellationToken) ?? throw new GXAmiNotFoundException(Properties.Resources.Agent + " " + Properties.Resources.Id + " " + it.Id.ToString());
                if (string.IsNullOrEmpty(it.UpdateVersion))
                {
                    throw new ArgumentException("Invalid agent version to update.");
                }
                agent.Updated = DateTime.Now;
                agent.UpdateVersion = it.UpdateVersion;
                GXUpdateArgs update = GXUpdateArgs.Update(agent, c => new { c.UpdateVersion, c.Updated });
                {
                    await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                    await connection.UpdateAsync(update, cancellationToken);
                }
                _notificationCache.Add(await GetNotifiedUsers(agent.Id, NotificationAction.Edit),
                        new GXTarget(GXAgentPolicies.Edit, agent.Id, agent.Name));
                _notificationCache.AddAgent(agent.Id, new GXTarget(GXAgentPolicies.Update, agent.Id, agent.Name));
            }
        }

        /// <inheritdoc />
        public async Task ClearCache(IEnumerable<Guid>? Ids,
            IEnumerable<string> names,
            CancellationToken cancellationToken)
        {
            if (Ids != null)
            {
                foreach (var id in Ids)
                {
                    GXAgent agent = await ReadAsync(id, cancellationToken: cancellationToken) ?? throw new GXAmiNotFoundException(
                            Properties.Resources.Agent + " " +
                            Properties.Resources.Id + " " +
                            id.ToString());
                    _notificationCache.Add(await GetNotifiedUsers(id, NotificationAction.Edit), new GXTarget(GXAgentPolicies.Edit, agent.Id, agent.Name));
                }
            }
        }
    }
}
