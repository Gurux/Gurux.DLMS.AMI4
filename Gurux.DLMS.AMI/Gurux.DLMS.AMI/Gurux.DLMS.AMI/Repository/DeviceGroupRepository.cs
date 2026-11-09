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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Properties;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <inheritdoc />
public class DeviceGroupRepository : IDeviceGroupRepository
{
    private readonly ClaimsPrincipal User;

    private readonly IDbConnectionFactory _dbFactory;

    private readonly IUserRepository _userRepository;

    private readonly IUserGroupRepository _userGroupRepository;

    private readonly NotificationCache _notificationCache;

    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DeviceGroupRepository(IAuthorizationService authorizationService, IDbConnectionFactory dbFactory, IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IUserGroupRepository userGroupRepository, NotificationCache notificationCache)
    {
        if (httpContextAccessor.HttpContext?.User == null)
        {
            throw new UnauthorizedAccessException();
        }
        User = httpContextAccessor.HttpContext.User;
        _dbFactory = dbFactory;
        _userRepository = userRepository;
        _userGroupRepository = userGroupRepository;
        _notificationCache = notificationCache;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Returns agent groups where device group belongs.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="deviceGroupId">Device group ID</param>
    /// <returns>List of agent groups where device group id belongs.</returns>
    /// <param name="transaction">The database transaction.</param>
    private static List<GXAgentGroup> GetAgentGroupsByDeviceGroupId(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId)
    {
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXAgentGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXAgentGroup gXAgentGroup) => gXAgentGroup.Id, (GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.AgentGroupId);
        arg.Joins.AddInnerJoin((GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.DeviceGroupId, (GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id);
        arg.Where.And((GXDeviceGroup where) => where.Removed == null && where.Id == deviceGroupId);
        return connection.Select<GXAgentGroup>(transaction, arg);
    }

    private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action, GXDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        return GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDeviceGroup), id, action, "device-group.view", connection, transaction, notifications: _notificationCache);
    }

    /// <inheritdoc />
    public async Task<List<GXDeviceGroup>> GetDeviceGroupsByDeviceId(Guid deviceId)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXDeviceGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id, (GXDeviceGroupDevice gXDeviceGroupDevice) => gXDeviceGroupDevice.DeviceGroupId);
        arg.Joins.AddInnerJoin((GXDeviceGroupDevice gXDeviceGroupDevice) => gXDeviceGroupDevice.DeviceId, (GXDevice gXDevice) => gXDevice.Id);
        arg.Where.And((GXDevice where) => where.Removed == null && where.Id == deviceId);
        List<GXDeviceGroup> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            result = await connection.SelectAsync<GXDeviceGroup>(arg);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXUserGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXUserGroup gXUserGroup) => gXUserGroup.Id, (GXUserGroupDeviceGroup gXUserGroupDeviceGroup) => gXUserGroupDeviceGroup.UserGroupId);
        arg.Joins.AddInnerJoin((GXUserGroupDeviceGroup gXUserGroupDeviceGroup) => gXUserGroupDeviceGroup.DeviceGroupId, (GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id);
        arg.Where.And((GXDeviceGroup where) => where.Removed == null && where.Id == deviceGroupId);
        return await connection.SelectAsync<GXUserGroup>(transaction, arg);
    }

    /// <inheritdoc />
    public static List<GXDevice> GetDevicessByDeviceGroupId(GXDbConnection connection, IDbTransaction transaction, ClaimsPrincipal user, Guid deviceGroupId)
    {
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXDevice where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXDevice gXDevice) => gXDevice.Id, (GXDeviceGroupDevice gXDeviceGroupDevice) => gXDeviceGroupDevice.DeviceId);
        arg.Joins.AddInnerJoin((GXDeviceGroupDevice gXDeviceGroupDevice) => gXDeviceGroupDevice.DeviceGroupId, (GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id);
        arg.Joins.AddInnerJoin((GXDeviceGroup j) => j.Id, (GXUserGroupDeviceGroup j) => j.DeviceGroupId);
        arg.Joins.AddInnerJoin((GXUserGroupDeviceGroup x) => x.UserGroupId, (GXUserGroup y) => y.Id);
        arg.Joins.AddInnerJoin((GXUserGroup x) => x.Id, (GXUserGroupUser y) => y.UserGroupId);
        arg.Joins.AddInnerJoin((GXUserGroupUser x) => x.UserId!, (GXUser y) => y.Id!);
        string id = ServerHelpers.GetUserId(user);
        arg.Where.And((GXUser q) => q.Removed == null && q.Id == id);
        arg.Where.And((GXDeviceGroup where) => where.Removed == null && where.Id == deviceGroupId);
        return connection.Select<GXDevice>(transaction, arg);
    }

    /// <inheritdoc />
    public async Task<List<GXAgentGroup>> GetJoinedAgentGroups(Guid deviceGroupId)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXAgentGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXAgentGroup gXAgentGroup) => gXAgentGroup.Id, (GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.AgentGroupId);
        arg.Joins.AddInnerJoin((GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.DeviceGroupId, (GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id);
        arg.Where.And((GXDeviceGroup where) => where.Removed == null && where.Id == deviceGroupId);
        List<GXAgentGroup> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            result = await connection.SelectAsync<GXAgentGroup>(arg);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<GXDeviceGroup>> GetDeviceGroupsByAgentId(Guid agentGroupId)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXDeviceGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id, (GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.DeviceGroupId);
        arg.Joins.AddInnerJoin((GXAgentGroupDeviceGroup gXAgentGroupDeviceGroup) => gXAgentGroupDeviceGroup.AgentGroupId, (GXAgentGroup gXAgentGroup) => gXAgentGroup.Id);
        arg.Where.And((GXAgentGroup where) => where.Removed == null && where.Id == agentGroupId);
        List<GXDeviceGroup> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            result = await connection.SelectAsync<GXDeviceGroup>(arg);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<GXDeviceGroup>> GetDeviceGroupsByGatewayId(Guid gatewayGroupId)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = GXSelectArgs.SelectAll((GXDeviceGroup where) => where.Removed == null);
        arg.Joins.AddInnerJoin((GXDeviceGroup gXDeviceGroup) => gXDeviceGroup.Id, (GXGatewayDeviceGroup gXGatewayDeviceGroup) => gXGatewayDeviceGroup.DeviceGroupId);
        arg.Joins.AddInnerJoin((GXGatewayDeviceGroup gXGatewayDeviceGroup) => gXGatewayDeviceGroup.GatewayId, (GXGatewayGroup gXGatewayGroup) => gXGatewayGroup.Id);
        arg.Where.And((GXGatewayGroup where) => where.Removed == null && where.Id == gatewayGroupId);
        List<GXDeviceGroup> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            result = await connection.SelectAsync<GXDeviceGroup>(arg);
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs args = GXQuery.GetUsersByDeviceGroup(s => s.Id!, ServerHelpers.GetUserId(User), groupId);
        List<string> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            if (groupId.HasValue)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXDeviceGroup), groupId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<string> ret = [.. (await connection.SelectAsync<GXUser>(args)).Select(s => s.Id).OfType<string>()];
            if (User != null && User.IsInRole("admin"))
            {
                IList self = ret;
                self.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(["admin"]));
            }
            result = ret;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs args = GXQuery.GetUsersByDeviceGroups(s => s.Id!, ServerHelpers.GetUserId(User), groupIds);
        List<string> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            List<string> ret = [.. (await connection.SelectAsync<GXUser>(args)).Select(s => s.Id).OfType<string>()];
            if (User != null && User.IsInRole("admin"))
            {
                IList self = ret;
                self.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(["admin"]));
            }
            result = ret;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
    {
        groups = groups.ToArray();
        await GXHistoryMutation.RunDeleteAsync<GXDeviceGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
    async () =>
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.delete");
        await using GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync();
        IEnumerable<GXDeviceGroup> exists;
        if (User.IsInRole("admin"))
        {
            GXSelectArgs arg = GXSelectArgs.Select(s => new { s.Id, s.Name }, (GXDeviceGroup w) => groups.Contains(w.Id));
            exists = connection.Select<GXDeviceGroup>(arg);
        }
        else
        {
            GXSelectArgs arg2 = GXQuery.GetDeviceGroupsByUser(userId: ServerHelpers.GetUserId(User), columns: s => new { s.Id, s.Name }, groupId: null, removed: DateTimeOffset.MinValue, exclude: null, include: groups);
            exists = connection.Select<GXDeviceGroup>(arg2);
            if (!exists.Any())
            {
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
        }
        DateTime now = DateTime.Now;
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (GXDeviceGroup it in exists)
            {
                if (!groups.Contains(it.Id))
                {
                    throw new GXAmiForbiddenException(it.Id.ToString());
                }
                it.Removed = now;
                NotificationCache notificationCache = _notificationCache;
                notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction), new GXTarget("device-group.delete", it.Id, it.Name));
                if (!delete)
                {
                    connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                }
            }
            if (delete)
            {
                await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (GXDatabaseException ex)
        {
            if (ex.ErrorType == DatabaseErrorType.Deadlock)
            {
                CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5L));
                try
                {
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    await Task.Delay(1000, linkedCts.Token);
                    await DeleteAsync(groups, delete, notify, linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    throw ex;
                }
                return;
            }
            throw;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GXDeviceGroup>> ListAsync(ListDeviceGroups? request, ListDeviceGroupsResponse? response, CancellationToken cancellationToken = default)
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
        GXSelectArgs arg = ((request == null || !request.AllUsers || !User.IsInRole("admin")) ? GXQuery.GetDeviceGroupsByUser(userId: ServerHelpers.GetUserId(User), columns: s => "*", groupId: null, removed: request?.Filter?.Removed, exclude: request?.Exclude, include: request?.Included) : GXSelectArgs.SelectAll<GXDeviceGroup>());
        if (request != null)
        {
            if (request.Included != null && request.Included.Any())
            {
                arg.Where.And((GXDeviceGroup w) => request.Included.Contains(w.Id));
            }
            else
            {
                if (request.Filter?.UserGroups != null)
                {
                    GXUserGroup? ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        GXUser? user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin((GXUserGroupDeviceGroup j) => j.UserGroupId, (GXUserGroup j) => j.Id);
                            arg.Joins.AddLeftJoin((GXUserGroup j) => j.Id, (GXUserGroupUser j) => j.UserGroupId);
                            arg.Joins.AddLeftJoin((GXUserGroupUser j) => j.UserId!, (GXUser j) => j.Id!);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            if (request.Exclude?.Any() ?? false)
            {
                arg.Where.And((GXDeviceGroup w) => !request.Exclude.Contains(w.Id));
            }
            if (request.Included?.Any() ?? false)
            {
                arg.Where.And((GXDeviceGroup w) => request.Included.Contains(w.Id));
            }
        }
        GXDateTimeFilters.Apply<GXDeviceGroup>(arg, request?.DateFilters);
        IEnumerable<GXDeviceGroup> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            if (request != null && request.Count != 0)
            {
                var total = GXSelectArgs.Select<GXDeviceGroup>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        return [];
                    }
                }

                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDeviceGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add((GXDeviceGroup q) => q.CreationTime!);
            }
            GXDeviceGroup[] groups = [.. (await connection.SelectAsync<GXDeviceGroup>(arg))];
            if (request?.Select != null && request.Select.Contains("device"))
            {
                GXDeviceGroup[] array = groups;
                foreach (GXDeviceGroup it in array)
                {
                    arg = GXSelectArgs.Select(s => new { s.Id, s.Name }, (GXDevice w) => w.Removed == null);
                    arg.Joins.AddInnerJoin((GXDevice j) => j.Id, (GXDeviceGroupDevice j) => j.DeviceId);
                    arg.Joins.AddInnerJoin((GXDeviceGroupDevice j) => j.DeviceGroupId, (GXDeviceGroup j) => j.Id);
                    arg.Where.And((GXDeviceGroup w) => w.Removed == null && w.Id == it.Id);
                    if (request.Select.Contains("device-template"))
                    {
                        arg.Columns.Add<GXDeviceTemplate>();
                        arg.Joins.AddInnerJoin((GXDevice j) => j.Template!, (GXDeviceTemplate j) => j.Id);
                    }
                    GXDeviceGroup gXDeviceGroup = it;
                    gXDeviceGroup.Devices = await connection.SelectAsync<GXDevice>(arg);
                }
            }
            if (response != null)
            {
                response.DeviceGroups = groups;
            }
            result = groups;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<GXDeviceGroup> ReadAsync(Guid id, Expression<Func<GXDeviceGroup, object>>? columns = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.view");
            GXSelectArgs arg = ((!User.IsInRole("admin")) ? GXQuery.GetDeviceGroupsByUser(userId: ServerHelpers.GetUserId(User), columns: s => "*", groupId: id) : GXSelectArgs.SelectAll((GXDeviceGroup w) => w.Id == id));
            arg.Columns.Exclude((GXDeviceGroup gXDeviceGroup5) => gXDeviceGroup5.Devices!);
            arg.Distinct = true;
            GXDeviceGroup result;
            await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
            {
                GXDeviceGroup? group = await connection.SingleOrDefaultAsync<GXDeviceGroup>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                arg = GXSelectArgs.Select(s => new { s.Id, s.Name }, (GXDevice w) => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin((GXDeviceGroupDevice j) => j.DeviceId, (GXDevice j) => j.Id);
                arg.Where.And((GXDeviceGroupDevice q) => q.Removed == null && q.DeviceGroupId == id);
                GXDeviceGroup gXDeviceGroup = group;
                gXDeviceGroup.Devices = await connection.SelectAsync<GXDevice>(arg);
                arg = GXSelectArgs.Select(s => new { s.Id, s.Name }, (GXUserGroup w) => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin((GXUserGroupDeviceGroup j) => j.UserGroupId, (GXUserGroup j) => j.Id);
                arg.Where.And((GXUserGroupDeviceGroup q) => q.Removed == null && q.DeviceGroupId == id);
                GXDeviceGroup gXDeviceGroup2 = group;
                gXDeviceGroup2.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                arg = GXSelectArgs.Select(s => new { s.Id, s.Name }, (GXAgentGroup w) => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin((GXAgentGroup j) => j.Id, (GXAgentGroupDeviceGroup j) => j.AgentGroupId);
                arg.Joins.AddInnerJoin((GXAgentGroupDeviceGroup j) => j.DeviceGroupId, (GXDeviceGroup j) => j.Id);
                arg.Where.And((GXDeviceGroup q) => q.Removed == null);
                arg.Where.And((GXAgentGroupDeviceGroup q) => q.Removed == null && q.DeviceGroupId == id);
                GXDeviceGroup gXDeviceGroup3 = group;
                gXDeviceGroup3.AgentGroups = await connection.SelectAsync<GXAgentGroup>(arg);
                arg = GXSelectArgs.Select(s => new { s.Id, s.UserName }, (GXUser q) => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin((GXDeviceGroup s) => s.Creator!, (GXUser o) => o.Id!);
                arg.Where.And((GXDeviceGroup w) => w.Id == id);
                GXDeviceGroup gXDeviceGroup4 = group;
                gXDeviceGroup4.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                if (columns == null || ServerHelpers.Contains(columns, nameof(GXDeviceGroup.Properties)))
                {
                    group.Properties = await GXPropertyStores.DeviceGroup.ListAsync(connection, null, group, cancellationToken);
                }
                result = group;
            }
            return result;
        }
        catch (GXDatabaseException ex)
        {
            GXDatabaseException ex2 = ex;
            if (ex2.ErrorType == DatabaseErrorType.Deadlock)
            {
                CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5L));
                try
                {
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    await Task.Delay(1000, linkedCts.Token);
                    return await ReadAsync(id, columns, linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    throw ex2;
                }
            }
            throw;
        }
    }

    /// <summary>Inserts a new group and its memberships in the caller's transaction without notifications or history.</summary>
    internal static async Task UpsertAsync(GXDbConnection connection, System.Data.IDbTransaction transaction,
        GXDeviceGroup group, CancellationToken cancellationToken = default)
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
        if (group.UserGroups?.Count is not > 0 || group.UserGroups.Any(g => g.Id == Guid.Empty))
            {
            throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.TargetMustBelongToOneGroup);
        }
        var args = GXInsertArgs.Insert(group);
        args.Exclude<GXDeviceGroup>(group => new { group.Updated, group.Removed, group.UserGroups, group.AgentGroups, group.Properties });
        await connection.InsertAsync(transaction, args, cancellationToken);
        var memberships = group.UserGroups.Select(userGroup => new GXUserGroupDeviceGroup
        {
            UserGroupId = userGroup.Id,
            DeviceGroupId = group.Id,
            CreationTime = DateTime.Now
        }).ToArray();
        await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        if (group.AgentGroups?.Count > 0)
        {
            var agents = group.AgentGroups.Select(agent => new GXAgentGroupDeviceGroup
            {
                AgentGroupId = agent.Id, DeviceGroupId = group.Id, CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(agents), cancellationToken);
        }
        if (group.Properties != null)
            {
            await GXPropertyStores.DeviceGroup.ReplaceAsync(connection, transaction, group, group.Properties, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXDeviceGroup> deviceGroups, Expression<Func<GXDeviceGroup, object>>? columns, CancellationToken cancellationToken = default)
    {
        deviceGroups = deviceGroups.ToArray();
        return await GXHistoryMutation.RunAsync<GXDeviceGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, deviceGroups,
    async () =>
    {
        DateTime now = DateTime.Now;
        List<GXDeviceGroup> newGroups = [.. deviceGroups.Where(w => w.Id == Guid.Empty)];
        List<GXDeviceGroup> updatedGroups = [.. deviceGroups.Where(w => w.Id != Guid.Empty)];
        IEnumerable<Guid> result;
        await using (GXDbConnection connection = await _dbFactory.GetHistoryConnectionAsync())
        {
            IEnumerable<Guid> enumerable;
            await using (DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    foreach (GXDeviceGroup it in deviceGroups)
                    {
                        if (string.IsNullOrEmpty(it.Name))
                        {
                            throw new ArgumentException(Resources.InvalidName);
                        }
                        if (it.AgentGroups != null && it.AgentGroups.Where(w => w.Id == Guid.Empty).Any())
                        {
                            throw new ArgumentException(Resources.JoinedGroupsAreNotInserted);
                        }
                        if (it.Devices != null && it.Devices.Where(w => w.Id == Guid.Empty).Any())
                        {
                            throw new ArgumentException(Resources.JoinedGroupsAreNotInserted);
                        }
                        List<GXUserGroup>? userGroups = it.UserGroups;
                        if (userGroups == null || userGroups.Count == 0)
                        {
                            GXDeviceGroup gXDeviceGroup = it;
                            gXDeviceGroup.UserGroups = await _userGroupRepository.GetDefaultUserGroupsAsync(ServerHelpers.GetUserId(User));
                            List<GXUserGroup>? userGroups2 = it.UserGroups;
                            if (userGroups2 == null || userGroups2.Count == 0)
                            {
                                throw new ArgumentException(Resources.TargetMustBelongToOneGroup);
                            }
                        }
                    }
                    if (newGroups.Count != 0)
                    {
                        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.add");
                        foreach (var it in newGroups)
                        {
                            it.Creator = new GXUser { Id = ServerHelpers.GetUserId(User) };
                            it.CreationTime = now;
                            await UpsertAsync(connection, transaction, it, cancellationToken);
                            _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction), new GXTarget("device-group.add", it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                        }
                    }
                    if (updatedGroups.Count != 0)
                    {
                        await _authorizationService.EnsureAnyPolicyAsync(User, "device-group.edit");
                        foreach (GXDeviceGroup it4 in updatedGroups)
                        {
                            GXSelectArgs m = GXSelectArgs.Select(q => q.ConcurrencyStamp!, (GXDeviceGroup where) => where.Id == it4.Id);
                            string? updated = connection.SingleOrDefault<string>(transaction, m);
                            if (!string.IsNullOrEmpty(updated) && updated != it4.ConcurrencyStamp)
                            {
                                throw new ArgumentException(Resources.ContentEdited);
                            }
                            it4.Updated = now;
                            it4.ConcurrencyStamp = Guid.NewGuid().ToString();
                            GXUpdateArgs args2 = GXUpdateArgs.Update(it4, columns);
                            args2.Exclude((GXDeviceGroup q) => new { q.UserGroups, q.CreationTime, q.Devices, q.AgentGroups, q.Properties });
                            if (!User.IsInRole("admin") || it4.Creator == null || string.IsNullOrEmpty(it4.Creator.Id))
                            {
                                args2.Exclude((GXDeviceGroup q) => q.Creator!);
                            }
                            connection.Update(transaction, args2);
                            List<Guid> groups = [.. (await GetJoinedUserGroups(connection, transaction, it4.Id)).Select(s => s.Id)];
                            Guid[] tmp = [.. (it4.UserGroups ?? []).Select(s => s.Id)];
                            Guid[] removed = [.. groups.Except(tmp)];
                            Guid[] added = [.. tmp.Except(groups)];
                            if (added.Length != 0)
                            {
                                AddDeviceGroupToUserGroups(connection, transaction, it4.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveDeviceGroupFromUserGroups(connection, transaction, it4.Id, removed);
                            }
                            if (it4.Devices != null)
                            {
                                List<GXDevice> devices = GetDevicessByDeviceGroupId(connection, transaction, User, it4.Id);
                                UniqueComparer<GXDevice, Guid> comparer2 = new();
                                List<GXDevice> removedDevices = [.. devices.Except(it4.Devices, comparer2)];
                                List<GXDevice> addedDevices = [.. it4.Devices.Except(devices, comparer2)];
                                if (removedDevices.Count != 0)
                                {
                                    RemoveDevicesFromDeviceGroup(connection, transaction, it4.Id, removedDevices);
                                }
                                if (addedDevices.Count != 0)
                                {
                                    AddDevicesToDeviceGroup(connection, transaction, it4.Id, addedDevices);
                                }
                            }
                            if (it4.AgentGroups != null)
                            {
                                List<GXAgentGroup> agentGroups = GetAgentGroupsByDeviceGroupId(connection, transaction, it4.Id);
                                UniqueComparer<GXAgentGroup, Guid> comparer3 = new();
                                List<GXAgentGroup> removedAgentGroups = [.. agentGroups.Except(it4.AgentGroups, comparer3)];
                                List<GXAgentGroup> addedAgentGroups = [.. it4.AgentGroups.Except(agentGroups, comparer3)];
                                if (removedAgentGroups.Count != 0)
                                {
                                    RemoveAgenGroupsFromDeviceGroup(connection, transaction, it4.Id, removedAgentGroups);
                                }
                                if (addedAgentGroups.Count != 0)
                                {
                                    AddAgentGroupsToDeviceGroup(connection, transaction, it4.Id, addedAgentGroups);
                                }
                            }
                            NotificationCache notificationCache2 = _notificationCache;
                            if (it4.Properties != null)
                            {
                                await GXPropertyStores.DeviceGroup.ReplaceAsync(connection, transaction, it4, it4.Properties, cancellationToken);
                            }
                            notificationCache2.Add(await GetNotifiedUsers(it4.Id, NotificationAction.Edit, connection, transaction), new GXTarget("device-group.edit", it4.Id, it4.Name) { ConcurrencyStamp = it4.ConcurrencyStamp });
                        }
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (GXDatabaseException ex)
                {
                    if (ex.ErrorType != DatabaseErrorType.Deadlock)
                    {
                        throw;
                    }
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(deviceGroups, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                enumerable = [.. deviceGroups.Select(s => s.Id)];
            }
            result = enumerable;
        }
        return result;
    }, cancellationToken);
    }

    /// <summary>
    /// Map device group to user groups.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Group IDs of the device groups where the device is added.</param>
    public void AddDeviceGroupToUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<Guid> groups)
    {
        DateTime now = DateTime.Now;
        List<GXUserGroupDeviceGroup> list = [];
        foreach (Guid ug in groups)
        {
            list.Add(new GXUserGroupDeviceGroup
            {
                DeviceGroupId = deviceGroupId,
                UserGroupId = ug,
                CreationTime = now
            });
        }
        connection.Insert(transaction, GXInsertArgs.InsertRange(list));
    }

    /// <summary>
    /// Remove map between device group and user groups.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Group IDs of the device groups where the device is removed.</param>
    public void RemoveDeviceGroupFromUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<Guid> groups)
    {
        foreach (Guid ug in groups)
        {
            connection.Delete(transaction, GXDeleteArgs.Delete((GXUserGroupDeviceGroup w) => w.UserGroupId == ug && w.DeviceGroupId == deviceGroupId));
        }
    }

    /// <summary>
    /// Map devices to device group.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Group IDs of the device groups where the device is added.</param>
    public void AddDevicesToDeviceGroup(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXDevice> groups)
    {
        DateTime now = DateTime.Now;
        List<GXDeviceGroupDevice> list = [];
        foreach (GXDevice it in groups)
        {
            list.Add(new GXDeviceGroupDevice
            {
                DeviceGroupId = deviceGroupId,
                DeviceId = it.Id,
                CreationTime = now
            });
        }
        connection.Insert(transaction, GXInsertArgs.InsertRange(list));
    }

    /// <summary>
    /// Remove map between devices and device group.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Device groups where the device is removed.</param>
    public void RemoveDevicesFromDeviceGroup(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXDevice> groups)
    {
        GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXDeviceGroupDevice>();
        foreach (GXDevice it in groups)
        {
            args.Where.Or((GXDeviceGroupDevice w) => w.DeviceId == it.Id && w.DeviceGroupId == deviceGroupId);
        }
        connection.Delete(transaction, args);
    }

    /// <summary>
    /// Map agent groups to device group.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Agent groups where the agent is added.</param>
    public void AddAgentGroupsToDeviceGroup(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXAgentGroup> groups)
    {
        DateTime now = DateTime.Now;
        List<GXAgentGroupDeviceGroup> list = [];
        foreach (GXAgentGroup it in groups)
        {
            list.Add(new GXAgentGroupDeviceGroup
            {
                DeviceGroupId = deviceGroupId,
                AgentGroupId = it.Id,
                CreationTime = now
            });
        }
        connection.Insert(transaction, GXInsertArgs.InsertRange(list));
    }

    /// <summary>
    /// Remove map between device groups and agent groups.
    /// </summary>
    /// <param name="connection">DB connection.</param>
    /// <param name="transaction">Transaction.</param>
    /// <param name="deviceGroupId">Device group ID.</param>
    /// <param name="groups">Agent groups where the agent is removed.</param>
    public void RemoveAgenGroupsFromDeviceGroup(GXDbConnection connection, IDbTransaction transaction, Guid deviceGroupId, IEnumerable<GXAgentGroup> groups)
    {
        GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXAgentGroupDeviceGroup>();
        foreach (GXAgentGroup it in groups)
        {
            args.Where.Or((GXAgentGroupDeviceGroup w) => w.AgentGroupId == it.Id && w.DeviceGroupId == deviceGroupId);
        }
        connection.Delete(transaction, args);
    }
}
