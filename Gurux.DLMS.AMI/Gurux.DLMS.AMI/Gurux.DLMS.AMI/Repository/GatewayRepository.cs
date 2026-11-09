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
using Gurux.DLMS.AMI.Client.Pages.Gateway;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
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


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class GatewayRepository : IGatewayRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IGatewayGroupRepository _gatewayGroupRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IGatewayGroupRepository gatewayGroupRepository,
            IServiceProvider serviceProvider,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _gatewayGroupRepository = gatewayGroupRepository;
            _serviceProvider = serviceProvider;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Get all notified users that can access gateway repository.
        /// </summary>
        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXGateway), id, action, GXGatewayPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? gatewayId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByGateway(s => s.Id!,
                ServerHelpers.GetUserId(User), gatewayId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (gatewayId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXGateway), gatewayId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByGateways(s => s.Id!,
                ServerHelpers.GetUserId(User), Ids);
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
        /// Returns gateway groups where gateway belongs.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of gateway groups where gateway belongs.</returns>
        private static async Task<List<GXGatewayGroup>> GetGatewayGroupsByGatewayId(GXDbConnection connection, Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGatewayGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXGatewayGroup, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayGroupId);
            arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return (await connection.SelectAsync<GXGatewayGroup>(arg));
        }

        /// <summary>
        /// Returns joined device groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of joined device groups.</returns>
        private static async Task<List<GXDeviceGroup>> GetDeviceGroupsByGatewayId(GXDbConnection connection, Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXGatewayDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXGatewayDeviceGroup, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return await connection.SelectAsync<GXDeviceGroup>(arg);
        }

        /// <summary>
        /// Returns joined device.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of joined devices.</returns>
        private static async Task<List<GXDevice>> GetDevicesByGatewayId(GXDbConnection connection, Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDevice, GXGatewayDevice>(a => a.Id, b => b.DeviceId);
            arg.Joins.AddInnerJoin<GXGatewayDevice, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return await connection.SelectAsync<GXDevice>(arg);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> gateways,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            gateways = gateways.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXGateway, Guid>(_dbFactory, User, _serviceProvider, gateways,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXGateway>(a => a.Id, q => gateways.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXGateway> list = connection.Select<GXGateway>(arg);
                DateTime now = DateTime.Now;
                var recipients = new Dictionary<Guid, List<string>>();
                foreach (var gateway in list)
                {
                    recipients[gateway.Id] = await GetNotifiedUsers(gateway.Id, NotificationAction.Remove, connection);
                }

                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                foreach (GXGateway it in list)
                {
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "gateway", [it.Id], cancellationToken);
                    it.Removed = now;
                    if (delete)
                    {
                        await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXGateway>(it.Id), cancellationToken);
                    }
                    else
                    {
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, q => q.Removed!), cancellationToken);
                    }

                }

                await transaction.CommitAsync(cancellationToken);
                foreach (var gateway in list)
                {
                    _notificationCache.Add(recipients[gateway.Id], new GXTarget(GXGatewayPolicies.Delete, gateway.Id, gateway.Name));
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
                        await DeleteAsync(gateways, delete, notify, linkedCts.Token);
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

        /// <inheritdoc />
        public async Task<IEnumerable<GXGateway>> ListAsync(
            ListGateways? request,
            ListGatewaysResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the gateways.
                arg = GXSelectArgs.SelectAll<GXGateway>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewaysByUser(s => "*", userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXGateway>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXGateway>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXGateway>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXGateway>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXGateway>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXGateway>(q => q.CreationTime!);
            }
            GXGateway[] gateways = [.. (await connection.SelectAsync<GXGateway>(arg))];
            if (response != null)
            {
                response.Gateways = gateways;
                if (response.Count == 0)
                {
                    response.Count = gateways.Length;
                }
            }
            return gateways;
        }

        /// <inheritdoc />
        public async Task<GXGateway> ReadAsync(Guid id, Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the gateways.
                    arg = GXSelectArgs.SelectAll<GXGateway>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetGatewaysByUser(s => "*", userId);
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXGateway? gateway = await connection.SingleOrDefaultAsync<GXGateway>(arg) ?? throw new GXAmiNotFoundException(Properties.Resources.Gateway + " " + Properties.Resources.Id + " " + id.ToString());
                //Get groups.
                gateway.GatewayGroups = await GetGatewayGroupsByGatewayId(connection, gateway.Id);
                ////////////////////////////////////////////////////
                //Get device groups.
                gateway.DeviceGroups = await GetDeviceGroupsByGatewayId(connection, id);
                ////////////////////////////////////////////////////
                //Get agent.
                arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Joins.AddInnerJoin<GXGateway, GXAgent>(j => j.Agent!, j => j.Id);
                arg.Where.And<GXGateway>(q => q.Removed == null && q.Id == id);
                gateway.Agent = await connection.SingleOrDefaultAsync<GXAgent>(arg);

                ////////////////////////////////////////////////////
                //Get devices.
                gateway.Devices = await GetDevicesByGatewayId(connection, id);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXGateway, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXGateway>(w => w.Id == id);
                gateway.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return gateway;

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
            IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object>>? columns, CancellationToken cancellationToken = default)
        {
            gateways = gateways.ToArray();
            return await GXHistoryMutation.RunAsync<GXGateway, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, gateways,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.Add, GXGatewayPolicies.Edit);
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                DateTime now = DateTime.Now;
                Dictionary<GXGateway, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXGateway gateway in gateways)
                {
                    if (string.IsNullOrEmpty(gateway.Name) && (columns == null || ServerHelpers.Contains(columns, nameof(GXGateway.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (gateway.Id == Guid.Empty)
                    {
                        if (gateway.GatewayGroups == null || gateway.GatewayGroups.Count == 0)
                        {
                            ListGatewayGroups request = new() { Filter = new GXGatewayGroup() { Default = true } };
                            IEnumerable<GXGatewayGroup> groups = await _gatewayGroupRepository.ListAsync(request, cancellationToken: cancellationToken);
                            gateway.GatewayGroups = [.. groups];
                        }
                        gateway.CreationTime = now;
                        gateway.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(gateway);
                        args.Exclude<GXGateway>(q => new
                        {
                            q.Updated,
                            q.GatewayGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.Removed
                        });
                        connection.Insert(args);
                        AddGatewayToGatewayGroups(connection, gateway.Id, gateway.GatewayGroups);
                        AddDeviceGroupsToGateway(connection, gateway.Id, gateway.DeviceGroups);
                        AddDevicesToGateway(connection, gateway.Id, gateway.Devices);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXGateway>(q => q.ConcurrencyStamp!, where => where.Id == gateway.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != gateway.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        gateway.Updated = now;
                        gateway.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(gateway, columns);
                        args.Exclude<GXGateway>(q => new
                        {
                            q.CreationTime,
                            q.GatewayGroups,
                            q.DeviceGroups,
                            q.Devices,
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            gateway.Creator == null ||
                            string.IsNullOrEmpty(gateway.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXGateway>(q => q.Creator!);
                        }

                        connection.Update(args);
                        //Map gateway groups to gateway.
                        if (gateway.GatewayGroups != null && gateway.GatewayGroups.Count != 0)
                        {
                            List<GXGatewayGroup> gatewayGroups = await GetGatewayGroupsByGatewayId(connection, gateway.Id);
                            var comparer = new UniqueComparer<GXGatewayGroup, Guid>();
                            List<GXGatewayGroup> removedGatewayGroups = [.. gatewayGroups.Except(gateway.GatewayGroups, comparer)];
                            List<GXGatewayGroup> addedGatewayGroups = [.. gateway.GatewayGroups.Except(gatewayGroups, comparer)];
                            if (removedGatewayGroups.Count != 0)
                            {
                                RemoveGatewaysFromGatewayGroup(connection, gateway.Id, removedGatewayGroups);
                            }
                            if (addedGatewayGroups.Count != 0)
                            {
                                AddGatewayToGatewayGroups(connection, gateway.Id, addedGatewayGroups);
                            }
                        }
                        //Map device groups to gateway.
                        if (gateway.DeviceGroups != null)
                        {
                            List<GXDeviceGroup> deviceGroups = await GetDeviceGroupsByGatewayId(connection, gateway.Id);
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedDeviceGroups = [.. deviceGroups.Except(gateway.DeviceGroups, comparer)];
                            List<GXDeviceGroup> addedDeviceGroups = [.. gateway.DeviceGroups.Except(deviceGroups, comparer)];
                            if (removedDeviceGroups.Count != 0)
                            {
                                RemoveDeviceGroupsFromGateway(connection, gateway.Id, removedDeviceGroups);
                            }
                            if (addedDeviceGroups.Count != 0)
                            {
                                AddDeviceGroupsToGateway(connection, gateway.Id, addedDeviceGroups);
                            }
                        }
                        //Map devices to gateway.
                        if (gateway.Devices != null)
                        {
                            List<GXDevice> devices = await GetDevicesByGatewayId(connection, gateway.Id);
                            var comparer = new UniqueComparer<GXDevice, Guid>();
                            List<GXDevice> removedDevices = [.. devices.Except(gateway.Devices, comparer)];
                            List<GXDevice> addedDevices = [.. gateway.Devices.Except(devices, comparer)];
                            if (removedDevices.Count != 0)
                            {
                                RemoveDevicesFromGateway(connection, gateway.Id, removedDevices);
                            }
                            if (addedDevices.Count != 0)
                            {
                                AddDevicesToGateway(connection, gateway.Id, addedDevices);
                            }
                        }
                    }
                    _notificationCache.Add(await GetNotifiedUsers(gateway.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXGatewayPolicies.Edit, gateway.Id, gateway.Name));
                }
                return [.. gateways.Select(s => s.Id)];

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
                        return await UpsertAsync(gateways, columns, linkedCts.Token);
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
        /// Map gateway group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="groups">Gateway groups where the gateway is added.</param>
        private static void AddGatewayToGatewayGroups(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXGatewayGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXGatewayGroupGateway> list = [];
            foreach (GXGatewayGroup it in groups)
            {
                list.Add(new GXGatewayGroupGateway()
                {
                    GatewayId = gatewayId,
                    GatewayGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateway group and gateway.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="groups">Gateway groups where the gateway is removed.</param>
        private static void RemoveGatewaysFromGatewayGroup(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXGatewayGroup> groups)
        {
            foreach (var it in groups)
            {
                connection.Delete(GXDeleteArgs.Delete<GXGatewayGroupGateway>(w => w.GatewayId == gatewayId && w.GatewayGroupId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(Guid gatewayId, GatewayStatus status, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXGatewayPolicies.Edit);
            GXSelectArgs args = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name }, where => where.Id == gatewayId && where.Removed == null);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXGateway? gateway = await connection.SingleOrDefaultAsync<GXGateway>(args) ?? throw new GXAmiNotFoundException(Properties.Resources.Gateway + " " + Properties.Resources.Id + " " + gatewayId.ToString());
            gateway.Status = status;
            gateway.Detected = DateTime.Now;
            GXUpdateArgs update;
            update = GXUpdateArgs.Update(gateway, c => new { c.Status, c.Detected });
            await connection.UpdateAsync(update, cancellationToken);
            //Only part of the gateway properties are send.
            GXGateway tmp = new()
            {
                Id = gateway.Id,
                Name = gateway.Name,
                Detected = gateway.Detected,
                Status = gateway.Status
            };
            _notificationCache.Add(await GetNotifiedUsers(gateway.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXGatewayPolicies.Edit, gateway.Id, gateway.Name));
            //Update gateway state for the gateway log.
            GXLog? log = new(TraceLevel.Info)
            {
                Creator = gateway.Id
            };
            switch (status)
            {
                case GatewayStatus.Connected:
                    log.Message = Properties.Resources.Connected;
                    break;
                case GatewayStatus.Offline:
                    log.Message = Properties.Resources.Offline;
                    break;
                case GatewayStatus.Error:
                    log.Message = Properties.Resources.Error;
                    break;
                default:
                    log = null;
                    break;
            }
            if (log != null)
            {
                //Add gateway log. Idle status is not logged.
                ILogRepository repository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("gateway");
                await repository.AddAsync(status.ToString(), [log], cancellationToken);
            }
        }

        /// <summary>
        /// Map gateway to device group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="gateways">Device groups where the gateway is added.</param>
        private static void AddDeviceGroupsToGateway(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXDeviceGroup>? gateways)
        {
            if (gateways != null)
            {
                DateTime now = DateTime.Now;
                List<GXGatewayDeviceGroup> list = [];
                foreach (var it in gateways)
                {
                    list.Add(new GXGatewayDeviceGroup()
                    {
                        GatewayId = gatewayId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateway and device group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="groups">Device groups where the gateway is removed.</param>
        private static void RemoveDeviceGroupsFromGateway(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXDeviceGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayDeviceGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXGatewayDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                    w.GatewayId == gatewayId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map devices to gateway.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="devices">Added devices.</param>
        private static void AddDevicesToGateway(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXDevice>? devices)
        {
            if (devices != null)
            {
                DateTime now = DateTime.Now;
                List<GXGatewayDevice> list = [];
                foreach (var it in devices)
                {
                    list.Add(new GXGatewayDevice()
                    {
                        GatewayId = gatewayId,
                        DeviceId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateway and device.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="devices">Removed devices.</param>
        private static void RemoveDevicesFromGateway(GXDbConnection connection,
            Guid gatewayId, IEnumerable<GXDevice> devices)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayDevice>();
            foreach (var it in devices)
            {
                args.Where.Or<GXGatewayDevice>(w => w.DeviceId == it.Id &&
                    w.GatewayId == gatewayId);
            }
            connection.Delete(args);
        }

        /// <inheritdoc />
        public Task ResetAsync(IEnumerable<Guid> gateways, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
