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

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class TriggerRepository : ITriggerRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITriggerGroupRepository _triggerGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;
        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            ITriggerGroupRepository triggerGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _triggerGroupRepository = triggerGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(Guid? triggerId, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByTrigger(s => s.Id!,
                ServerHelpers.GetUserId(User), triggerId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (triggerId.HasValue && triggerId.Value != Guid.Empty)
                {
                await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXTrigger), triggerId.Value, cancellationToken);
            }
            if (triggerId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXTrigger), triggerId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? triggerId, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByTriggers(s => s.Id!,
                ServerHelpers.GetUserId(User), triggerId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (triggerId != null)
                {
                foreach (var resourceId in triggerId.Distinct())
                    {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXTrigger), resourceId, cancellationToken);
                }
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            IEnumerable<Guid> triggers,
            bool delete, bool notify, CancellationToken cancellationToken)
        {
            triggers = triggers.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXTrigger, Guid>(_dbFactory, User, _serviceProvider, triggers,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXTrigger>(a => a.Id, q => triggers.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXTrigger> list = connection.Select<GXTrigger>(arg);
                foreach (var resource in list)
                {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXTrigger), resource.Id, cancellationToken);
                    await GXWorkflowIdentityAccess.ValidateTriggerMutationAsync(connection, User, resource.Id, cancellationToken);
                }
                DateTime now = DateTime.Now;
                Dictionary<GXTrigger, List<string>> updates = [];
                foreach (GXTrigger it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXTrigger>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(it.Value,
                        new GXTarget(GXTriggerPolicies.Delete, it.Key.Id, it.Key.Name));
                }

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
                        await DeleteAsync(triggers, delete, notify, linkedCts.Token);
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

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXTrigger), id, action, GXTriggerPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<IEnumerable<GXTrigger>> ListAsync(
            ListTriggers? request,
            ListTriggersResponse? response,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.View);
            GXSelectArgs arg;
            if (request != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the triggers.
                arg = GXSelectArgs.SelectAll<GXTrigger>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetTriggersByUser(s => "*", userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXTrigger>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXTrigger>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXTrigger>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await GXAutomationPolicyAccess.ApplyAsync(connection, _authorizationService, User, typeof(GXTrigger), arg, cancellationToken);
            await GXTriggerActivityAccess.ApplyAsync(connection, _authorizationService, User, arg, cancellationToken);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTrigger>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXTrigger>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXTrigger>(q => q.Id);
            }

            GXTrigger[] triggers = [.. (await connection.SelectAsync<GXTrigger>(arg))];
            foreach (var resource in triggers)
                {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXTrigger), resource.Id, cancellationToken);
            }
            //Get trigger activities.
            foreach (var trigger in triggers)
            {
                //There can be multiple activities per trigger for this reason own query is used.
                arg = GXSelectArgs.SelectAll<GXTriggerActivity>(w => w.Trigger == trigger);
                arg.Columns.Exclude<GXTriggerActivity>(e => e.Trigger!);
                trigger.Activities = await GXTriggerActivityAccess.FilterAsync(_authorizationService, User, trigger.ClassName,
                    await connection.SelectAsync<GXTriggerActivity>(arg), cancellationToken);
            }
            if (response != null)
            {
                response.Triggers = triggers;
                if (response.Count == 0)
                {
                    response.Count = triggers.Length;
                }
            }
            return triggers;
        }

        /// <inheritdoc />
        public async Task<GXTrigger> ReadAsync(Guid id, Expression<Func<GXTrigger, object>>? columns,
            CancellationToken cancellationToken)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the triggers.
                    arg = GXSelectArgs.SelectAll<GXTrigger>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetTriggersByUser(s => "*", userId, id);
                }
                //Get trigger activities.
                arg.Columns.Add<GXTriggerActivity>();
                arg.Columns.Exclude<GXTriggerActivity>(e => e.Trigger!);
                arg.Joins.AddInnerJoin<GXTrigger, GXTriggerActivity>(j => j.Id, j => j.Trigger!);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXTrigger? trigger = await connection.SingleOrDefaultAsync<GXTrigger>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXTrigger), id, cancellationToken);
                trigger.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXTrigger), id, cancellationToken);
                trigger.Activities = await GXTriggerActivityAccess.FilterAsync(_authorizationService, User, trigger.ClassName, trigger.Activities, cancellationToken);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXTrigger, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXTrigger>(w => w.Id == id);
                trigger.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return trigger;

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

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object>>? columns, CancellationToken cancellationToken)
        {
            triggers = triggers.ToArray();
            return await GXHistoryMutation.RunAsync<GXTrigger, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, triggers,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.Add, GXTriggerPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                List<Guid> list = [];
                Dictionary<GXTrigger, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXTrigger trigger in triggers)
                {
                    if (trigger.Id != Guid.Empty)
                    {
                        await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User, typeof(GXTrigger), trigger.Id, cancellationToken);
                        await GXWorkflowIdentityAccess.ValidateTriggerMutationAsync(connection, User, trigger.Id, cancellationToken);
                    }
                    await _authorizationService.EnsureAnyPolicyAsync(User, trigger.Id == Guid.Empty ? GXTriggerPolicies.Add : GXTriggerPolicies.Edit);
                    await GXResourcePolicyAccess.ValidateAsync(connection, trigger.Policies, cancellationToken);
                    if (string.IsNullOrEmpty(trigger.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (trigger.TriggerGroups == null || trigger.TriggerGroups.Count == 0)
                    {
                        trigger.TriggerGroups = [];
                        ListTriggerGroups request = new() { Filter = new GXTriggerGroup() { Default = true } };
                        trigger.TriggerGroups.AddRange(await _triggerGroupRepository.ListAsync(request, null, CancellationToken.None));
                        //It's OK if trigger doesn't belong to trigger group.
                    }
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                    if (trigger.Id == Guid.Empty)
                    {
                        trigger.Creator = creator;
                        trigger.CreationTime = now;
                        GXInsertArgs args = GXInsertArgs.Insert(trigger);
                        args.Exclude<GXTrigger>(q => new
                        {
                            q.Updated,
                            q.TriggerGroups
                        });
                        connection.Insert(transaction, args);
                        list.Add(trigger.Id);
                        if (trigger.TriggerGroups != null && trigger.TriggerGroups.Count != 0)
                        {
                            AddTriggerToTriggerGroups(connection, trigger.Id, trigger.TriggerGroups, transaction);
                        }
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXTrigger>(q => q.ConcurrencyStamp!, where => where.Id == trigger.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != trigger.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        trigger.Updated = now;
                        trigger.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(trigger, columns);
                        args.Exclude<GXTrigger>(q => new
                        {
                            q.CreationTime,
                            q.TriggerGroups
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                           trigger.Creator == null ||
                           string.IsNullOrEmpty(trigger.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXTrigger>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);
                        //Map trigger groups to trigger.
                        await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerGroupPolicies.View);
                    var joined = GXSelectArgs.SelectAll<GXTriggerGroup>(g => g.Removed == null);
                    joined.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(g => g.Id, map => map.TriggerGroupId);
                    joined.Where.And<GXTriggerGroupTrigger>(map => map.TriggerId == trigger.Id && map.Removed == null);
                    var triggerGroups = await connection.SelectAsync<GXTriggerGroup>(transaction, joined, cancellationToken);
                        var comparer = new UniqueComparer<GXTriggerGroup, Guid>();
                        List<GXTriggerGroup> removedTriggerGroups = [.. triggerGroups.Except(trigger.TriggerGroups, comparer)];
                        List<GXTriggerGroup> addedTriggerGroups = [.. trigger.TriggerGroups.Except(triggerGroups, comparer)];
                        if (removedTriggerGroups.Count != 0)
                        {
                            RemoveTriggersFromTriggerGroup(connection, trigger.Id, removedTriggerGroups, transaction);
                        }
                        if (addedTriggerGroups.Count != 0)
                        {
                            AddTriggerToTriggerGroups(connection, trigger.Id, addedTriggerGroups, transaction);
                        }

                    }
                    if (trigger.Policies != null)
                        {
                        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXTrigger), trigger.Id, trigger.Policies, cancellationToken, transaction);
                    }
                    await transaction.CommitAsync(cancellationToken);
                    updates[trigger] = await GetNotifiedUsers(trigger.Id, NotificationAction.Edit, connection);
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXTriggerPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                return [.. list];
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
                        return await UpsertAsync(triggers, columns, linkedCts.Token);
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
        /// Map trigger group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerId">Trigger ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void AddTriggerToTriggerGroups(GXDbConnection connection,
            Guid triggerId, IEnumerable<GXTriggerGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            DateTime now = DateTime.Now;
            List<GXTriggerGroupTrigger> list = [];
            foreach (GXTriggerGroup it in groups)
            {
                list.Add(new GXTriggerGroupTrigger()
                {
                    TriggerId = triggerId,
                    TriggerGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between trigger group and trigger.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="triggerId">Trigger ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        /// <param name="transaction">Transaction for the resource and its memberships.</param>
        private static void RemoveTriggersFromTriggerGroup(GXDbConnection connection,
            Guid triggerId, IEnumerable<GXTriggerGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            foreach (var it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXTriggerGroupTrigger>(w => w.TriggerId == triggerId && w.TriggerGroupId == it.Id));
            }
        }

        /// <summary>
        /// Adds missing registered built-in triggers and activities while preserving configured values.
        /// </summary>
        public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAllAsync<GXTrigger>(_dbFactory, User, _serviceProvider,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTriggerPolicies.Add);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var changed = await GXTriggerDefaults.EnsureAsync(connection, transaction, typeof(AgentTrigger).Assembly, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            foreach (var trigger in changed)
                {
                _notificationCache.Add(await GetNotifiedUsers(trigger.Id, NotificationAction.Edit, connection),
                    new GXTarget(GXTriggerPolicies.Edit, trigger.Id, trigger.Name));
            }
        }, cancellationToken);
        }
    }
}
