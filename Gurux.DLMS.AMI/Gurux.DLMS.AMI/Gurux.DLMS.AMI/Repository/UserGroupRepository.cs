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
using Gurux.DLMS.AMI.Client.Pages.ComponentView;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.RegularExpressions;
namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserGroupRepository : IUserGroupRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache _notificationCache;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserGroupRepository(
            IDbConnectionFactory dbFactory,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            _authorizationService = authorizationService;
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _notificationCache = notificationCache;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXUserGroup), id, action, GXUserGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args;
            if (User.IsInRole(GXRoles.Admin))
            {
                args = GXQuery.GetUsersByUserGroup(s => s.Id!, null, groupId);
            }
            else
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.View);
                args = GXQuery.GetUsersByUserGroup(s => s.Id!,
                    ServerHelpers.GetUserId(User), groupId);

            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXUserGroup), groupId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByUserGroups(s => s.Id!, ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXUserGroup, Guid>(_dbFactory, User, _serviceProvider, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXUserGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXUserGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetUserGroupsByUser(s => new { s.Id, s.Name }, userId, null, DateTimeOffset.MinValue,
                    null, groups);
                exists = connection.Select<GXUserGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (delete)
                {
                    var dispatcher = _serviceProvider.GetRequiredService<Gurux.DLMS.AMI.Module.IGXAmiModuleServices>();
                    try
                    {
                        await using var database = GXDatabase.UseConnection(connection.Connection);
                        await dispatcher.InvokeAsync<IGXAmiMembershipLifecycle>((provider, token) => provider.UserGroupsDeletingAsync(database, transaction, [.. exists.Select(g => g.Id)], token), cancellationToken);
                    }
                    catch (Gurux.DLMS.AMI.Module.AmiModuleUnavailableException)
                    {
                    }
                }
                foreach (GXUserGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    _notificationCache.Add(users, new GXTarget(GXUserGroupPolicies.Delete, it.Id, it.Name));
                }
                if (delete)
                {
                    //Delete module groups permanently.
                    var arg = GXSelectArgs.Select<GXModuleGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId);
                    arg.Where.And<GXUserGroupModuleGroup>(w => groups.Contains(w.UserGroupId));
                    var moduleGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (moduleGroups.Count != 0)
                    {
                        IModuleGroupRepository moduleGroupRepository = _serviceProvider.GetRequiredService<IModuleGroupRepository>();
                        await moduleGroupRepository.DeleteAsync(moduleGroups, cancellationToken: cancellationToken);
                    }
                    //Delete block groups permanently.
                    arg = GXSelectArgs.Select<GXBlockGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId);
                    arg.Where.And<GXUserGroupBlockGroup>(w => groups.Contains(w.UserGroupId));
                    var blockGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (blockGroups.Count != 0)
                    {
                        IBlockGroupRepository blockGroupRepository = _serviceProvider.GetRequiredService<IBlockGroupRepository>();
                        await blockGroupRepository.DeleteAsync(blockGroups, cancellationToken: cancellationToken);
                    }
                    //Delete menu groups permanently.
                    arg = GXSelectArgs.Select<GXMenuGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId);
                    arg.Where.And<GXUserGroupMenuGroup>(w => groups.Contains(w.UserGroupId));
                    var menuGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (menuGroups.Count != 0)
                    {
                        IMenuGroupRepository menuGroupRepository = _serviceProvider.GetRequiredService<IMenuGroupRepository>();
                        await menuGroupRepository.DeleteAsync(menuGroups, cancellationToken: cancellationToken);
                    }
                    //Delete component view groups permanently.
                    arg = GXSelectArgs.Select<GXComponentViewGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId);
                    arg.Where.And<GXUserGroupComponentViewGroup>(w => groups.Contains(w.UserGroupId));
                    var componentViewGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (componentViewGroups.Count != 0)
                    {
                        IComponentViewGroupRepository componentViewGroupRepository = _serviceProvider.GetRequiredService<IComponentViewGroupRepository>();
                        await componentViewGroupRepository.DeleteAsync(componentViewGroups, cancellationToken: cancellationToken);
                    }
                    //Delete script view groups permanently.
                    arg = GXSelectArgs.Select<GXScriptGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId);
                    arg.Where.And<GXUserGroupScriptGroup>(w => groups.Contains(w.UserGroupId));
                    var scriptGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (scriptGroups.Count != 0)
                    {
                        IScriptGroupRepository scriptGroupRepository = _serviceProvider.GetRequiredService<IScriptGroupRepository>();
                        await scriptGroupRepository.DeleteAsync(scriptGroups, cancellationToken: cancellationToken);
                    }
                    //Delete manufacturer groups permanently.
                    arg = GXSelectArgs.Select<GXManufacturerGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId);
                    arg.Where.And<GXUserGroupManufacturerGroup>(w => groups.Contains(w.UserGroupId));
                    var manufacturerGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (manufacturerGroups.Count != 0)
                    {
                        IManufacturerGroupRepository manufacturerGroupRepository = _serviceProvider.GetRequiredService<IManufacturerGroupRepository>();
                        await manufacturerGroupRepository.DeleteAsync(manufacturerGroups, cancellationToken: cancellationToken);
                    }

                    //Delete key management groups permanently.
                    arg = GXSelectArgs.Select<GXKeyManagementGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId);
                    arg.Where.And<GXUserGroupKeyManagementGroup>(w => groups.Contains(w.UserGroupId));
                    var keyManagementGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (keyManagementGroups.Count != 0)
                    {
                        IKeyManagementGroupRepository keyManagementGroupRepository = _serviceProvider.GetRequiredService<IKeyManagementGroupRepository>();
                        await keyManagementGroupRepository.DeleteAsync(keyManagementGroups, cancellationToken: cancellationToken);
                    }
                    //Delete gateway groups permanently.
                    arg = GXSelectArgs.Select<GXGatewayGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId);
                    arg.Where.And<GXUserGroupGatewayGroup>(w => groups.Contains(w.UserGroupId));
                    var gatewayGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (gatewayGroups.Count != 0)
                    {
                        IGatewayGroupRepository gatewayGroupRepository = _serviceProvider.GetRequiredService<IGatewayGroupRepository>();
                        await gatewayGroupRepository.DeleteAsync(gatewayGroups, cancellationToken: cancellationToken);
                    }

                    //Delete agent groups permanently.
                    arg = GXSelectArgs.Select<GXAgentGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId);
                    arg.Where.And<GXUserGroupAgentGroup>(w => groups.Contains(w.UserGroupId));
                    var agentGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (agentGroups.Count != 0)
                    {
                        IAgentGroupRepository agentGroupRepository = _serviceProvider.GetRequiredService<IAgentGroupRepository>();
                        await agentGroupRepository.DeleteAsync(agentGroups, cancellationToken: cancellationToken);
                    }
                    //Delete device groups permanently.
                    arg = GXSelectArgs.Select<GXDeviceGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                    arg.Where.And<GXUserGroupDeviceGroup>(w => groups.Contains(w.UserGroupId));
                    var deviceGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (deviceGroups.Count != 0)
                    {
                        IDeviceGroupRepository deviceGroupRepository = _serviceProvider.GetRequiredService<IDeviceGroupRepository>();
                        await deviceGroupRepository.DeleteAsync(deviceGroups, cancellationToken: cancellationToken);
                    }
                    //Delete device template groups permanently.
                    arg = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId);
                    arg.Where.And<GXUserGroupDeviceTemplateGroup>(w => groups.Contains(w.UserGroupId));
                    var deviceTemplateGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (deviceTemplateGroups.Count != 0)
                    {
                        IDeviceTemplateGroupRepository deviceTemplateGroupRepository = _serviceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                        await deviceTemplateGroupRepository.DeleteAsync(deviceTemplateGroups, cancellationToken: cancellationToken);
                    }
                    //Delete trigger groups permanently.
                    arg = GXSelectArgs.Select<GXTriggerGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId);
                    arg.Where.And<GXUserGroupTriggerGroup>(w => groups.Contains(w.UserGroupId));
                    var triggerGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (triggerGroups.Count != 0)
                    {
                        ITriggerGroupRepository triggerGroupRepository = _serviceProvider.GetRequiredService<ITriggerGroupRepository>();
                        await triggerGroupRepository.DeleteAsync(triggerGroups, cancellationToken: cancellationToken);
                    }
                    //Delete workflow groups permanently.
                    arg = GXSelectArgs.Select<GXWorkflowGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId);
                    arg.Where.And<GXUserGroupWorkflowGroup>(w => groups.Contains(w.UserGroupId));
                    var workflowGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (workflowGroups.Count != 0)
                    {
                        IWorkflowGroupRepository workflowGroupRepository = _serviceProvider.GetRequiredService<IWorkflowGroupRepository>();
                        await workflowGroupRepository.DeleteAsync(workflowGroups, cancellationToken: cancellationToken);
                    }

                    //Delete schedule groups permanently.
                    arg = GXSelectArgs.Select<GXScheduleGroup>(s => s.Id);
                    arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
                    arg.Where.And<GXUserGroupScheduleGroup>(w => groups.Contains(w.UserGroupId));
                    var scheduleGroups = connection.Select<Guid>(transaction, arg, cancellationToken);
                    if (scheduleGroups.Count != 0)
                    {
                        IScheduleGroupRepository scheduleGroupRepository = _serviceProvider.GetRequiredService<IScheduleGroupRepository>();
                        await scheduleGroupRepository.DeleteAsync(scheduleGroups, true, cancellationToken: cancellationToken);
                    }
                    GXDeleteArgs args = GXDeleteArgs.Delete<GXUserGroupUser>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);

                    args = GXDeleteArgs.Delete<GXUserGroupComponentViewGroup>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    args = GXDeleteArgs.Delete<GXUserGroupScheduleGroup>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    args = GXDeleteArgs.Delete<GXUserGroupDeviceGroup>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    args = GXDeleteArgs.Delete<GXUserGroupDeviceTemplateGroup>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    args = GXDeleteArgs.Delete<GXUserGroupAgentGroup>(w => groups.Contains(w.UserGroupId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);

                    var del = GXDeleteArgs.DeleteRange(exists);
                    await connection.DeleteAsync(transaction, del, cancellationToken);
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
                        await DeleteAsync(groups, delete, notify, linkedCts.Token);
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
        public async Task<List<GXUserGroup>> GetJoinedUserGroupsAsync(string userId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(a => a.UserId!, b => b.Id!);
            arg.Where.And<GXUser>(where => where.Removed == null && where.Id == userId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXUserGroup>> ListAsync(
            ListUserGroups? request,
            ListUserGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.View);
            GXSelectArgs arg;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            if (allUsers)
            {
                //Admin can see all the user groups.
                arg = GXSelectArgs.SelectAll<GXUserGroup>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXUserGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXUserGroup>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetUserGroupsByUser(s => s.Id, userId, null, request?.Filter?.Removed,
                    request?.Exclude, request?.Included);
                arg.Columns.Clear();
                arg.Columns.Add<GXUserGroup>();
            }
            if (request != null)
            {
                //If user groups are filtered by user.
                if (request.Filter?.Users != null)
                {
                    var user = request.Filter.Users.FirstOrDefault();
                    if (user != null && allUsers)
                    {
                        arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                        arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                    }
                    arg.Where.FilterBy(user);
                    request.Filter.Users = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXUserGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXUserGroup>(q => q.CreationTime!);
            }
            GXDateTimeFilters.Apply<GXUserGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserGroup>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            GXUserGroup[] groups = [.. (await connection.SelectAsync<GXUserGroup>(arg, cancellationToken))];
            if (response != null)
            {
                response.UserGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXUserGroup> ReadAsync(Guid id, Expression<Func<GXUserGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetUserGroupsByUser(s => s.Id, userId, id);
                    arg.Columns.Clear();
                    arg.Columns.Add<GXUserGroup>();
                }
                arg.Columns.Exclude<GXUserGroup>(e => e.Users!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXUserGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get users that belongs for this user group.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName, s.GivenName, s.Surname }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!);
                arg.Where.And<GXUserGroupUser>(q => q.Removed == null && q.UserGroupId == id);
                group.Users = await connection.SelectAsync<GXUser>(arg);

                ////////////////////////////////////////////////////
                //Get schedule groups in this user group.
                arg = GXSelectArgs.Select<GXScheduleGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
                arg.Where.And<GXUserGroupScheduleGroup>(q => q.Removed == null && q.UserGroupId == id);
                group.ScheduleGroups = await connection.SelectAsync<GXScheduleGroup>(arg);
                ////////////////////////////////////////////////////
                //Get device groups.
                arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                arg.Where.And<GXUserGroupDeviceGroup>(q => q.Removed == null && q.UserGroupId == id);
                group.DeviceGroups = await connection.SelectAsync<GXDeviceGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXUserGroup>(w => w.Id == id);
                group.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return group;
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

        /// <summary>Inserts a new group and its memberships in the caller's transaction without notifications or history.</summary>
        internal static async Task UpsertAsync(GXDbConnection connection, System.Data.IDbTransaction transaction,
            GXUserGroup group, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Id != Guid.Empty)
                {
                throw new ArgumentException("This overload only inserts new groups.", nameof(group));
            }
            if (string.IsNullOrEmpty(group.Name))
                {
                throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.InvalidName);
            }
            if (string.IsNullOrEmpty(group.Creator?.Id))
                {
                throw new ArgumentException("A group creator is required.", nameof(group));
            }
            group.CreationTime ??= DateTime.Now;
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXUserGroup>(group => new { group.Users, group.Updated, group.Removed });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var users = group.Users?.Count > 0 ? group.Users.Select(u => u.Id!).ToArray() : [group.Creator.Id];
            AddUsersToGroup(connection, transaction, users, group.Id);
            AddDeviceGroupToUserGroup(connection, transaction, group.Id, group.DeviceGroups);
            AddDeviceTemplateGroupToUserGroup(connection, transaction, group.Id, group.DeviceTemplateGroups);
            AddScheduleGroupToUserGroup(connection, transaction, group.Id, group.ScheduleGroups);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXUserGroup> userGroups,
            Expression<Func<GXUserGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            userGroups = userGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXUserGroup, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, userGroups,
        async () =>
        {
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            var newGroups = userGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = userGroups.Where(w => w.Id != Guid.Empty).ToList();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (newGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.Add);
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        await UpsertAsync(connection, transaction, it, cancellationToken);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction), new GXTarget(GXUserGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                if (updatedGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.Edit);
                    foreach (GXUserGroup userGroup in updatedGroups)
                    {
                        if (string.IsNullOrEmpty(userGroup.Name))
                        {
                            throw new ArgumentException(Properties.Resources.InvalidName);
                        }
                        GXSelectArgs m = GXSelectArgs.Select<GXUserGroup>(q => q.ConcurrencyStamp!, where => where.Id == userGroup.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != userGroup.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        // Membership changes must use stored members, independently of notification preferences.
                        GXSelectArgs memberships = GXSelectArgs.Select<GXUserGroupUser>(q => q.UserId!,
                            q => q.UserGroupId == userGroup.Id && q.Removed == null);
                        List<string> users = (await connection.SelectAsync<GXUserGroupUser>(transaction, memberships))
                            .Select(q => q.UserId!).ToList();
                        userGroup.Updated = now;
                        userGroup.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(userGroup, columns);
                        args.Exclude<GXUserGroup>(q => new
                        {
                            q.CreationTime,
                            q.Users,
                            q.ScheduleGroups,
                            q.DeviceGroups,
                            q.DeviceTemplateGroups,
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            userGroup.Creator == null ||
                            string.IsNullOrEmpty(userGroup.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXUserGroup>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);
                        //Map users to user group.
                        if (userGroup.Users != null && userGroup.Users.Count != 0)
                        {
                            string[] tmp = [.. userGroup.Users.Select(s => s.Id!)];
                            string[] removed = [.. users.Except(tmp)];
                            string[] added = [.. tmp.Except(users)];
                            AddUsersToGroup(connection, transaction, added, userGroup.Id);
                            RemoveUsersFromGroup(connection, transaction, removed, userGroup.Id);
                        }
                    }
                    foreach (GXUserGroup userGroup in userGroups)
                    {
                        //Map device groups to user group.
                        if (userGroup.DeviceGroups != null && userGroup.DeviceGroups.Count != 0)
                        {
                            List<GXDeviceGroup> deviceGroups = await GetJoinedDeviceGroups(connection, transaction, userGroup.Id);
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedDeviceGroups = [.. deviceGroups.Except(userGroup.DeviceGroups, comparer)];
                            List<GXDeviceGroup> addedDeviceGroups = [.. userGroup.DeviceGroups.Except(deviceGroups, comparer)];
                            if (removedDeviceGroups.Count != 0)
                            {
                                RemoveDeviceGroupsFromUserGroup(connection, transaction, userGroup.Id, removedDeviceGroups);
                            }
                            if (addedDeviceGroups.Count != 0)
                            {
                                AddDeviceGroupToUserGroup(connection, transaction, userGroup.Id, addedDeviceGroups);
                            }
                        }

                        //Map device template groups to user group.
                        if (userGroup.DeviceTemplateGroups != null && userGroup.DeviceTemplateGroups.Count != 0)
                        {
                            List<GXDeviceTemplateGroup> groups = await GetJoinedDeviceTemplateGroups(connection, transaction, userGroup.Id);
                            var comparer = new UniqueComparer<GXDeviceTemplateGroup, Guid>();
                            List<GXDeviceTemplateGroup> removedGroups = [.. groups.Except(userGroup.DeviceTemplateGroups, comparer)];
                            List<GXDeviceTemplateGroup> addedGroups = [.. userGroup.DeviceTemplateGroups.Except(groups, comparer)];
                            if (removedGroups.Count != 0)
                            {
                                RemoveDeviceTemplateGroupsFromUserGroup(connection, transaction, userGroup.Id, removedGroups);
                            }
                            if (addedGroups.Count != 0)
                            {
                                AddDeviceTemplateGroupToUserGroup(connection, transaction, userGroup.Id, addedGroups);
                            }
                        }

                        //Map schedule groups to user group.
                        if (userGroup.ScheduleGroups != null && userGroup.ScheduleGroups.Count != 0)
                        {
                            List<GXScheduleGroup> scheduleGroups = await GetJoinedScheduleGroups(connection, transaction, userGroup.Id);
                            var comparer = new UniqueComparer<GXScheduleGroup, Guid>();
                            List<GXScheduleGroup> removedScheduleGroups = [.. scheduleGroups.Except(userGroup.ScheduleGroups, comparer)];
                            List<GXScheduleGroup> addedScheduleGroups = [.. userGroup.ScheduleGroups.Except(scheduleGroups, comparer)];
                            if (removedScheduleGroups.Count != 0)
                            {
                                RemoveScheduleGroupsFromUserGroup(connection, transaction, userGroup.Id, removedScheduleGroups);
                            }
                            if (addedScheduleGroups.Count != 0)
                            {
                                AddScheduleGroupToUserGroup(connection, transaction, userGroup.Id, addedScheduleGroups);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(userGroup.Id, NotificationAction.Edit, connection, transaction),
                            new GXTarget(GXUserGroupPolicies.Edit, userGroup.Id, userGroup.Name) { ConcurrencyStamp = userGroup.ConcurrencyStamp });
                    }
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
                        return await UpsertAsync(userGroups, columns, linkedCts.Token);
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
            return [.. userGroups.Select(s => s.Id)];
        }, cancellationToken);
        }

        /// <summary>
        /// Add users to the user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="users">Users ID.</param>
        /// <param name="groupId">Group ID of the group where the user is added.</param>
        private static void AddUsersToGroup(GXDbConnection connection,
            IDbTransaction transaction, IEnumerable<string> users, Guid groupId)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupUser> list = [];
            foreach (string it in users)
            {
                list.Add(new GXUserGroupUser()
                {
                    UserGroupId = groupId,
                    UserId = it,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove users from the user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="users">Users ID.</param>
        /// <param name="groupId">Group ID of the group where the user is removed.</param>
        private static void RemoveUsersFromGroup(GXDbConnection connection,
            IDbTransaction transaction, IEnumerable<string> users, Guid groupId)
        {
            if (users.Any() == true)
            {
                GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupUser>();
                foreach (var it in users)
                {
                    args.Where.Or<GXUserGroupUser>(w => w.UserGroupId == groupId &&
                        w.UserId == it);
                }
                connection.Delete(transaction, args);
            }
        }
        /// <summary>
        /// Add user to user groups.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="groups">Group IDs of the user groups where the user is added.</param>
        public async Task AddUserToGroupsAsync(string userId, IEnumerable<Guid> groups)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXUser, string>(_dbFactory, User, _serviceProvider, new[] { userId },
        async () =>
        {
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            AddUserToGroups(connection, null, userId, groups);
        }, CancellationToken.None);
        }

        /// <summary>
        /// Add user to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userId">User ID.</param>
        /// <param name="groups">Group IDs of the user groups where the user is added.</param>
        /// <param name="connection">DB connection.</param>
        private static void AddUserToGroups(GXDbConnection connection,
            IDbTransaction? transaction,
            string userId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupUser> list = [];
            foreach (Guid it in groups)
            {
                list.Add(new GXUserGroupUser()
                {
                    UserGroupId = it,
                    UserId = userId,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Map schedule group to user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Added schedule groups.</param>
        private static void AddScheduleGroupToUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXScheduleGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                DateTime now = DateTime.Now;
                List<GXUserGroupScheduleGroup> list = [];
                foreach (GXScheduleGroup it in groups)
                {
                    list.Add(new GXUserGroupScheduleGroup()
                    {
                        UserGroupId = userGroupId,
                        ScheduleGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between schedule group and user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Removed schedule groups.</param>
        private static void RemoveScheduleGroupsFromUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXScheduleGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScheduleGroup>();
                foreach (var it in groups)
                {
                    args.Where.Or<GXUserGroupScheduleGroup>(w => w.UserGroupId == userGroupId &&
                        w.ScheduleGroupId == it.Id);
                }
                connection.Delete(transaction, args);
            }
        }

        /// <summary>
        /// Map device group to user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Added device groups.</param>
        private static void AddDeviceGroupToUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXDeviceGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                DateTime now = DateTime.Now;
                List<GXUserGroupDeviceGroup> list = [];
                foreach (GXDeviceGroup it in groups)
                {
                    list.Add(new GXUserGroupDeviceGroup()
                    {
                        UserGroupId = userGroupId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group and user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Removed device groups.</param>
        private static void RemoveDeviceGroupsFromUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXDeviceGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupDeviceGroup>();
                foreach (var it in groups)
                {
                    args.Where.Or<GXUserGroupDeviceGroup>(w => w.UserGroupId == userGroupId &&
                        w.DeviceGroupId == it.Id);
                }
                connection.Delete(transaction, args);
            }
        }

        /// <summary>
        /// Map device template group to user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Added device template groups.</param>
        private static void AddDeviceTemplateGroupToUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXDeviceTemplateGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                DateTime now = DateTime.Now;
                List<GXUserGroupDeviceTemplateGroup> list = [];
                foreach (GXDeviceTemplateGroup it in groups)
                {
                    list.Add(new GXUserGroupDeviceTemplateGroup()
                    {
                        UserGroupId = userGroupId,
                        DeviceTemplateGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device template group and user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="userGroupId">User group ID.</param>
        /// <param name="groups">Removed device template groups.</param>
        private static void RemoveDeviceTemplateGroupsFromUserGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId, IEnumerable<GXDeviceTemplateGroup>? groups)
        {
            if (groups?.Any() == true)
            {
                GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupDeviceTemplateGroup>();
                foreach (var it in groups)
                {
                    args.Where.Or<GXUserGroupDeviceTemplateGroup>(w => w.UserGroupId == userGroupId &&
                    w.DeviceTemplateGroupId == it.Id);
                }
                connection.Delete(transaction, args);
            }
        }

        /// <summary>
        /// Returns schedule groups that belong for this user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="userGroupId">User group ID</param>
        /// <returns>List of schedule groups.</returns>
        private static async Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid userGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            arg.Where.And<GXUserGroup>(where => where.Removed == null && where.Id == userGroupId);
            return (await connection.SelectAsync<GXScheduleGroup>(transaction, arg));
        }

        /// <summary>
        /// Returns device groups that belong for this user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="userGroupId">User group ID</param>
        /// <returns>List of device groups.</returns>
        private static async Task<List<GXDeviceGroup>> GetJoinedDeviceGroups(
            GXDbConnection connection, IDbTransaction transaction, Guid userGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            arg.Where.And<GXUserGroup>(where => where.Removed == null && where.Id == userGroupId);
            return (await connection.SelectAsync<GXDeviceGroup>(transaction, arg));
        }

        /// <summary>
        /// Returns device template groups that belong for this user group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">DB transaction.</param>
        /// <param name="userGroupId">User group ID</param>
        /// <returns>List of device template groups.</returns>
        private static async Task<List<GXDeviceTemplateGroup>> GetJoinedDeviceTemplateGroups(
            GXDbConnection connection, IDbTransaction transaction, Guid userGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.DeviceTemplateGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            arg.Where.And<GXUserGroup>(where => where.Removed == null && where.Id == userGroupId);
            return (await connection.SelectAsync<GXDeviceTemplateGroup>(transaction, arg));
        }

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetDefaultUserGroupsAsync(string userId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUserGroupsByUser(s => s.Id, userId);
            args.Where.And<GXUserGroup>(w => w.Default == true);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return await connection.SelectAsync<GXUserGroup>(args);
        }
    }
}
