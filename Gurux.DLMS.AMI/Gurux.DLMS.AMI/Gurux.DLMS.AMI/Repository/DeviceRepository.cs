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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
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
    public class DeviceRepository : IDeviceRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeviceGroupRepository _deviceGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IKeyManagementRepository _keyManagementRepository;
        private readonly IAttributeRepository _attributeRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IDeviceGroupRepository deviceGroupRepository,
            IUserRepository userRepository,
            IKeyManagementRepository keyManagementRepository,
            IAttributeRepository attributeRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _deviceGroupRepository = deviceGroupRepository;
            _userRepository = userRepository;
            _keyManagementRepository = keyManagementRepository;
            _attributeRepository = attributeRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private void PublishDeviceEvent(string code, Guid id, IReadOnlyList<Guid>? groupIds = null)
            => _serviceProvider.GetRequiredService<IWorkflowHandler>().Publish(new GXWorkflowEventContext
            {
                EventCode = code,
                TargetId = id.ToString(),
                DeviceId = id,
                DeviceGroupIds = groupIds,
                OperationId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                Outcome = "success"
            }, User);

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDevice), id, action, GXDevicePolicies.View, connection, transaction, notifications: _notificationCache);

        private async Task EnsureDeviceScopeAsync(IEnumerable<Guid> deviceIds, CancellationToken cancellationToken)
        {
            if (User.IsInRole(GXRoles.Admin))
            {
                return;
            }
            string userId = ServerHelpers.GetUserId(User);
            var ids = deviceIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return;
            }
            var visible = GXQuery.GetDevicesByUser(d => d.Id, userId);
            visible.Where.And<GXDevice>(d => ids.Contains(d.Id) && d.Removed == null);
            visible.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId);
            visible.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(m => m.DeviceGroupId, g => g.Id);
            visible.Joins.AddInnerJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(g => g.Id, m => m.DeviceGroupId);
            visible.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(m => m.UserGroupId, g => g.Id);
            visible.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(g => g.Id, m => m.UserGroupId);
            visible.Where.And<GXDeviceGroupDevice>(m => m.Removed == null);
            visible.Where.And<GXDeviceGroup>(g => g.Removed == null);
            visible.Where.And<GXUserGroupDeviceGroup>(m => m.Removed == null);
            visible.Where.And<GXUserGroup>(g => g.Removed == null);
            visible.Where.And<GXUserGroupUser>(m => m.UserId == userId && m.Removed == null);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var allowed = (await connection.SelectAsync<GXDevice>(visible, cancellationToken))
                .Select(d => d.Id).ToHashSet();
            if (ids.Any(id => !allowed.Contains(id)))
            {
                throw new GXAmiForbiddenException(string.Join(", ", ids.Where(id => !allowed.Contains(id))));
            }
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? deviceId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDevice(ServerHelpers.GetUserId(User), deviceId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (deviceId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXDevice), deviceId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? deviceIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDevices(ServerHelpers.GetUserId(User), deviceIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> devices,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            devices = devices.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXDevice, Guid>(_dbFactory, User, _serviceProvider, devices,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Delete);
            devices = devices.Distinct().ToArray();
            await EnsureDeviceScopeAsync(devices, cancellationToken);
            GXSelectArgs arg = GXSelectArgs.Select<GXDevice>(a => a.Id, q => devices.Contains(q.Id));
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXDevice> list = connection.Select<GXDevice>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDevice, List<string>> updates = [];
            Dictionary<Guid, IReadOnlyList<Guid>> removedGroups = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var it in list)
                {
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "device", [it.Id], cancellationToken);
                    var memberships = await connection.SelectAsync<GXDeviceGroupDevice>(transaction,
                        GXSelectArgs.Select<GXDeviceGroupDevice>(g => g.DeviceGroupId, g => g.DeviceId == it.Id));
                    removedGroups[it.Id] = memberships.Select(g => g.DeviceGroupId).ToArray();
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction),
                            new GXTarget(GXDevicePolicies.Delete, it.Id, it.Name));
                    }
                    updates[it] = users;
                }
                //Delete tasks.
                arg = GXSelectArgs.Select<GXTask>(s => s.Id, w => list.Contains(w.Device!));
                var tasks = connection.Select<GXTask>(transaction, arg, cancellationToken).Select(t => t.Id).ToArray();
                if (tasks.Length != 0)
                {
                    ITaskRepository taskRepository = _serviceProvider.GetRequiredService<ITaskRepository>();
                    await taskRepository.DeleteAsync(tasks, cancellationToken);
                }
                if (delete)
                {
                    // Keep managed-key history while retiring the deleted device's key assignments.
                    var ownedKeys = await connection.SelectAsync<GXKeyManagement>(transaction,
                        GXSelectArgs.Select<GXKeyManagement>(k => new { k.Id, k.ConcurrencyStamp }, k => list.Contains(k.Device!)), cancellationToken);
                    foreach (var key in ownedKeys)
                    {
                        key.Device = null;
                        key.Removed = now;
                        await connection.UpdateAsync(transaction,
                            GXUpdateArgs.Update(key, k => new { k.Device, k.Removed }), cancellationToken);
                    }
                    //Delete tasks.
                    var devs = devices.Cast<Guid?>();
                    var args = GXDeleteArgs.Delete<GXTask>(q => devs.Contains(q.TargetDevice));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    GXDeleteArgs args1;
                    //////////////////////////////////////////////////////
                    //It's faster to remove atribute templates, object templates and device templates with own query than removing just device templates.
                    //////////////////////////////////////////////////////
                    //Delete attributes.
                    arg = GXSelectArgs.Select<GXObject>(a => a.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
                    arg.Where.And<GXDevice>(q => devices.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXAttribute>(w => GXSql.Exists<GXAttribute, GXObject>(j => j.Object!, j => j.Id, arg));
                    await connection.DeleteAsync(transaction, args1, cancellationToken);
                    //Delete objects.
                    arg = GXSelectArgs.Select<GXDevice>(a => a.Id, q => devices.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXObject>(w => GXSql.Exists<GXObject, GXDevice>(j => j.Device!, j => j.Id, arg));
                    await connection.DeleteAsync(transaction, args1, cancellationToken);
                    args = GXDeleteArgs.Delete<GXDeviceGroupDevice>(w => devices.Contains(w.DeviceId));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                    //Delete devices.
                    args = GXDeleteArgs.Delete<GXDevice>(q => devices.Contains(q.Id));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var removed in list)
                    {
                    PublishDeviceEvent("device.delete", removed.Id, removedGroups[removed.Id]);
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
                        await DeleteAsync(devices, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXDevice>> ListAsync(
            ListDevices? request,
            ListDevicesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
            GXSelectArgs arg;
            bool all = request != null && request.AllUsers &&
                User.IsInRole(GXRoles.Admin);
            if (all)
            {
                //Admin can see all the devices.
                arg = GXSelectArgs.SelectAll<GXDevice>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDevicesByUser(s => "*", userId);
            }
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Type, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
            //If devices are filtered by user.
            if (request?.Filter?.DeviceGroups != null &&
                request.Filter.DeviceGroups.SingleOrDefault() is GXDeviceGroup dg)
            {
                if (dg.UserGroups != null &&
                    dg.UserGroups.SingleOrDefault() is GXUserGroup ug)
                {
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            GXSelectArgs userGroups = GXSelectArgs.Select<GXDeviceGroupDevice>(s => s.DeviceId);
                            userGroups.Joins.AddLeftJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                            userGroups.Joins.AddLeftJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                            userGroups.Joins.AddLeftJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            userGroups.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            userGroups.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            userGroups.Where.FilterBy(user2);
                            arg.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, userGroups));
                        }
                    }
                }
                request.Filter.DeviceGroups = null;
            }
            else if ((request?.Filter?.Objects?.SingleOrDefault()?.Template?.LogicalName is string ldn) &&
                (request?.Filter?.Objects?.SingleOrDefault()?.Attributes?.SingleOrDefault()?.Value is string att))
            {
                //If devices are filtered by attribute. E.g. logical device name.
                request.Filter.Objects = null;
                arg.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
                arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
                arg.Where.And<GXObjectTemplate>(w => w.LogicalName == ldn);
                arg.Where.And<GXAttribute>(w => w.Value == att);
            }
            else if (request?.Filter?.Template != null)
            {
                arg.Where.FilterBy(request.Filter.Template);
                request.Filter.Template = null;
            }
            if (request?.Select != null && request.Select.Contains("DeviceTemplate"))
            {
                arg.Columns.Add<GXDeviceTemplate>();
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(x => x.Template!, y => y.Id);
                arg.Where.And<GXDeviceTemplate>(w => w.Removed == null);
            }
            arg.Columns.Exclude<GXDevice>(e => new { e.Settings, e.MediaSettings });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXDevice>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXDevice>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXDevice>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDevice>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXDevice>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDevice>(q => q.CreationTime!);
            }
            var devices = (await connection.SelectAsync<GXDevice>(arg)).ToArray();
            //Empty non needed values.
            foreach (var it in devices)
            {
                it.Actions = null;
                it.DeviceGroups = null;
                it.Errors = null;
                it.Properties = null;
                it.Tasks = null;
                it.Traces = null;
                it.Keys = null;
                if (request?.Select != null && request.Select.Contains(TargetType.Object))
                {
                    //Get objects.
                    arg = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template }, w => w.Device == it && w.Removed == null);
                    if (request.Select.Contains(TargetType.ObjectTemplate))
                    {
                        arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.Attributes });
                        arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
                    }
                    if (request.Select.Contains(TargetType.Attribute))
                    {
                        arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template, s.Object });
                        arg.Columns.Exclude<GXAttribute>(e => e.Object!);
                        arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                        if (request.Select.Contains(TargetType.AttributeTemplate))
                        {
                            arg.Columns.Add<GXAttributeTemplate>(s => new { s.Id, s.Name });
                            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template!, j => j.Id);
                        }
                    }
                    it.Objects = [.. (await connection.SelectAsync<GXObject>(arg))];
                }
                else
                {
                    it.Objects = null;
                }
                if (request?.Select != null && request.Select.Contains(TargetType.DeviceGroup))
                {
                    //Get device groups.
                    arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                    arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                    arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                    arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.Id);
                    it.DeviceGroups = [.. (await connection.SelectAsync<GXDeviceGroup>(arg))];
                }
            }

            if (response != null)
            {
                response.Devices = devices;
                if (response.Count == 0)
                {
                    response.Count = devices.Length;
                }
            }
            return devices;
        }

        /// <inheritdoc />
        public async Task<GXDevice> ReadAsync(Guid id, Expression<Func<GXDevice, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>();
                GXQuery.GetDevicesByUser(arg, userId, id);
                arg.Joins.AddLeftJoin<GXDevice, GXAppearance>(d => d.ProfilePicture!, a => a.Id);
                arg.Columns.Add<GXAppearance>(a => a.Id);
                arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
                arg.Columns.Add<GXDeviceTemplate>();
                arg.Columns.Exclude<GXDeviceGroup>(e => e.Devices!);
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(x => x.Template!, y => y.Id);
                bool isAdmin = User.IsInRole(GXRoles.Admin);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXDevice? device = await connection.SingleOrDefaultAsync<GXDevice>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                // Materialize the template as the query root; joined device reads can omit this reference.
                arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(t => t.Removed == null);
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXDevice>(t => t.Id, d => d.Template!);
                arg.Where.And<GXDevice>(d => d.Id == id);
                device.Template = await connection.SingleOrDefaultAsync<GXDeviceTemplate>(arg)
                    ?? throw new GXAmiForbiddenException(id.ToString());
                //Get device groups.
                arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                arg.Where.And<GXDeviceGroupDevice>(q => q.DeviceId == device.Id && q.Removed == null);
                device.DeviceGroups = await connection.SelectAsync<GXDeviceGroup>(arg);
                //Get device properties.
                device.Properties = await GXPropertyStores.Device.ListAsync(connection, null, device, cancellationToken);
                // Storage reads stay redacted; the authorized Device endpoint can add operational keys to its response snapshot.
                if (!string.IsNullOrEmpty(device.Settings))
                {
                    var settings = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(device.Settings);
                    if (settings != null)
                    {
                        settings.Password = null;
                        settings.BlockCipherKey = null;
                        settings.AuthenticationKey = null;
                        settings.BroadcastKey = null;
                        device.Settings = JsonSerializer.Serialize(settings);
                    }
                }
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDevice, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXDevice>(w => w.Id == id);
                device.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return device;

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

        private static void UpdateKeys(GXKeyManagement management, KeyManagementType type, byte[]? value)
        {
            management.Keys ??= [];
            // Rotation creates a fresh PreActive identity; it never overwrites active material.
            management.Keys.Add(new GXKeyManagementKey { KeyType = type, Key = value });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken,
            Expression<Func<GXDevice, object>>? columns,
            bool lateBinding,
            IEnumerable<GXDeviceGroup>? groups)
        {
            devices = devices.ToArray();
            // Capture only pre-existing devices. The history result must not contain inserted IDs.
            var updatedDevices = devices.Where(d => d.Id != Guid.Empty).ToArray();
            if (updatedDevices.Length == 0)
            {
                // Keep related writes and deferred effects atomic without capturing device history.
                return await GXHistoryScope.RunAsync(_dbFactory,
                    _ => UpsertCoreAsync(devices, cancellationToken, columns, lateBinding, groups), cancellationToken);
            }
            IEnumerable<Guid> ids = [];
            await GXHistoryMutation.RunAsync<GXDevice>(_dbFactory, User, _serviceProvider, updatedDevices,
                async () => { ids = await UpsertCoreAsync(devices, cancellationToken, columns, lateBinding, groups); },
                cancellationToken);
            return ids;
        }

        private async Task<IEnumerable<Guid>> UpsertCoreAsync(
            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken,
            Expression<Func<GXDevice, object>>? columns,
            bool lateBinding,
            IEnumerable<GXDeviceGroup>? groups)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            var newDevices = devices.Where(w => w.Id == Guid.Empty).ToList();
            var updatedDevices = devices.Where(w => w.Id != Guid.Empty).ToList();
            var notificationVersions = updatedDevices.GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First().ConcurrencyStamp);
            Dictionary<GXDevice, List<GXDeviceGroup>> deviceGroups = [];
            List<GXDeviceGroup>? defaultGroups = null;
            //Get notified users.
            if (newDevices.Count != 0)
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Add);
                if (defaultGroups == null)
                {
                    //Get default device groups.
                    ListDeviceGroups request = new()
                    {
                        Filter = new GXDeviceGroup() { Default = true }
                    };
                    defaultGroups = [.. await _deviceGroupRepository.ListAsync(request)];
                    if (defaultGroups.Count == 0)
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
                foreach (var it in newDevices)
                {
                    if (it.DeviceGroups == null || it.DeviceGroups.Count == 0)
                    {
                        it.DeviceGroups = defaultGroups;
                    }
                }
            }
            if (updatedDevices.Count != 0)
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Edit);
                await EnsureDeviceScopeAsync(updatedDevices.Select(d => d.Id), cancellationToken);
                if (columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.Name)))
                {
                    foreach (var it in updatedDevices)
                    {
                        //Get mapped device groups.
                        ListDeviceGroups request = new()
                        {
                            Filter = new GXDeviceGroup() { Default = true }
                        };
                        deviceGroups.Add(it, await _deviceGroupRepository.GetDeviceGroupsByDeviceId(it.Id));
                        if (deviceGroups[it].Count == 0)
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                    }
                }
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            // Validate resource policies before starting the device transaction: policy readers use their own commands.
            foreach (var device in devices)
            {
                if (device.Id == Guid.Empty || columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.ProfilePicture)))
                {
                    await Gurux.DLMS.AMI.Server.Services.GXProfilePictures.ValidateAsync(connection, _authorizationService, User, device.ProfilePicture?.Id, cancellationToken);
                }
            }
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                //Add batch device groups.
                if (groups != null && groups.Any())
                {
                    //Create new device groups.
                    foreach (var group in groups)
                    {
                        if (group.Id == Guid.Empty)
                        {
                            await _deviceGroupRepository.UpsertAsync(
                                [group]);
                        }
                        defaultGroups?.Add(group);
                    }
                    foreach (var it in updatedDevices)
                    {
                        //Add batch groups.
                        deviceGroups[it].AddRange(groups);
                    }
                }
                List<GXKeyManagement> keys = [];
                foreach (GXDevice device in devices)
                {
                    if (string.IsNullOrEmpty(device.Name) && (columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    bool newDevice = device.Id == Guid.Empty;
                    if (newDevice && (device.Template == null || device.Template.Id == Guid.Empty))
                    {
                        throw new ArgumentException("Invalid device template identifier.");
                    }
                    IEnumerable<GXKeyManagement>? tmp = null;
                    //Get device keys and save them to key manager table.
                    if (!string.IsNullOrEmpty(device.Settings))
                    {
                        var s = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(device.Settings);
                        if (s?.Password?.Length > 0 ||
                            s?.BlockCipherKey?.Length > 0 ||
                            s?.AuthenticationKey?.Length > 0)
                        {
                            if (device.Id != Guid.Empty)
                            {
                                //There is no need to get the managemenet keys when a new device is added.
                                ListKeyManagements req = new()
                                {
                                    Filter = new GXKeyManagement()
                                    {
                                        Device = device
                                    }
                                };
                                tmp = await _keyManagementRepository.ListAsync(req, null, cancellationToken);
                            }
                            if (tmp?.FirstOrDefault() == null)
                            {
                                //Check that device system title is not assigned for other meter.
                                if (s.DeviceSystemTitle?.Length > 0)
                                {
                                    ListKeyManagements req = new()
                                    {
                                        Filter = new GXKeyManagement()
                                        {
                                            SystemTitle = s.DeviceSystemTitle,
                                        }
                                    };
                                    tmp = await _keyManagementRepository.ListAsync(req, null, cancellationToken);
                                    //Check that system title is not used yet.
                                    foreach (var it in tmp)
                                    {
                                        if (it.SystemTitle?.Length > 0)
                                        {
                                            throw new ArgumentException(string.Format(Properties.Resources.SystemTitleIsAlreadyInUse,
                                                s.DeviceSystemTitle));
                                        }
                                    }
                                }
                                tmp = [ new(
                                    s.DeviceSystemTitle)
                                {
                                    Name = device.Name,
                                    Device = device
                                }
                                ];
                            }
                            else
                            {
                                //Read all key management information.
                                tmp = [await _keyManagementRepository.ReadAsync(tmp.First().Id)];
                            }
                            if (s?.Password?.Length > 0)
                            {
                                if (s.Authentication == (byte)Enums.Authentication.Low)
                                {
                                    UpdateKeys(tmp.First(), KeyManagementType.LLSPassword, s.Password);
                                }
                                else if (s.Authentication == (byte)Enums.Authentication.High)
                                {
                                    UpdateKeys(tmp.First(), KeyManagementType.HLSPassword, s.Password);
                                }
                                s.Password = null;
                            }
                            if (s?.BlockCipherKey?.Length > 0)
                            {
                                if (s.SecuritySuite == 2)
                                {
                                    if (s.BlockCipherKey.Length != 32)
                                    {
                                        throw new Exception("Invalid block cipher key.");
                                    }
                                }
                                else if (s.BlockCipherKey.Length != 16)
                                {
                                    throw new Exception("Invalid block cipher key.");
                                }
                                UpdateKeys(tmp.First(), KeyManagementType.BlockCipher, s.BlockCipherKey);
                                s.BlockCipherKey = null;
                            }
                            if (s?.AuthenticationKey?.Length > 0)
                            {
                                if (s.SecuritySuite == 2)
                                {
                                    if (s.AuthenticationKey.Length != 32)
                                    {
                                        throw new Exception("Invalid authentication key.");
                                    }
                                }
                                else if (s.AuthenticationKey.Length != 16)
                                {
                                    throw new Exception("Invalid authentication key.");
                                }
                                UpdateKeys(tmp.First(), KeyManagementType.Authentication, s.AuthenticationKey);
                                s.AuthenticationKey = null;
                            }
                            keys.Add(tmp.First());
                            device.Settings = JsonSerializer.Serialize(s);
                        }
                    }
                }

                await UpsertAsync(connection, transaction, newDevices, creator, now, lateBinding, cancellationToken);
                foreach (GXDevice device in updatedDevices)
                {
                    if ((device.DeviceGroups == null || device.DeviceGroups.Count == 0) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.DeviceGroups))))
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    device.Updated = DateTime.Now;
                    GXUpdateArgs args = GXUpdateArgs.Update(device, columns);
                    args.Exclude<GXDevice>(q => new
                    {
                        q.Objects,
                        q.CreationTime,
                        q.DeviceGroups,
                        q.Type,
                        q.Properties,
                        q.Template,
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                       device.Creator == null ||
                       string.IsNullOrEmpty(device.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXDevice>(q => q.Creator!);
                    }
                    connection.Update(transaction, args);
                    //Update objects if exists.
                    if (device.Objects != null)
                    {
                        GXAttribute a = new();
                        foreach (var obj in device.Objects)
                        {
                            if (obj.Attributes != null)
                            {
                                await _attributeRepository.UpsertAsync(obj.Attributes,
                                    c => a.Value!);
                            }
                        }
                    }
                    {
                        //Update device properties.
                        if (device.Properties == null)
                        {
                            if (ServerHelpers.Contains(columns, nameof(GXDevice.Properties)))
                            {
                                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                            }
                        }
                        else
                        {
                            await GXPropertyStores.Device.ReplaceAsync(connection, transaction, device, device.Properties, cancellationToken);
                        }
                    }
                    //Map device groups to device.
                    {
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        if (device.DeviceGroups == null)
                        {
                            if (ServerHelpers.Contains(columns, nameof(GXDevice.DeviceGroups)))
                            {
                                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                            }
                        }
                        else
                        {
                            List<GXDeviceGroup> removedDeviceGroups = [.. deviceGroups[device].Except(device.DeviceGroups, comparer)];
                            List<GXDeviceGroup> addedDeviceGroups = [.. device.DeviceGroups.Except(deviceGroups[device], comparer)];
                            if (removedDeviceGroups.Count != 0)
                            {
                                RemoveDevicesFromDeviceGroup(connection, transaction, device.Id, removedDeviceGroups);
                            }
                            if (addedDeviceGroups.Count != 0)
                            {
                                await AddDeviceToDeviceGroups(connection, transaction, device.Id, addedDeviceGroups, cancellationToken);
                            }
                        }
                    }
                }
                await transaction.CommitAsync();
                foreach (var added in newDevices)
                    {
                    PublishDeviceEvent("device.add", added.Id);
                }
                foreach (var edited in updatedDevices)
                    {
                    PublishDeviceEvent("device.edit", edited.Id);
                }
                if (keys.Count != 0)
                {
                    //Management keys must update after the devices are added for the DB.
                    var createdIds = newDevices.Select(d => d.Id).ToHashSet();
                    using var installedKeys = new GXDeviceKeyImportScope(keys
                        .Where(k => k.Device != null && createdIds.Contains(k.Device.Id))
                        .SelectMany(k => k.Keys ?? []));
                    await _keyManagementRepository.UpsertAsync(keys);
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
                        return await UpsertAsync(devices, linkedCts.Token, columns, lateBinding, groups);
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
            foreach (var it in newDevices)
            {
                _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                       new GXTarget(GXDevicePolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
            }
            foreach (var it in updatedDevices)
            {
                _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                       new GXTarget(GXDevicePolicies.Edit, it.Id, it.Name)
                       {
                           ConcurrencyStamp = it.ConcurrencyStamp != notificationVersions[it.Id] ? it.ConcurrencyStamp : null
                       });
            }
            return [.. devices.Select(s => s.Id)];
        }

        /// <summary>Insert validated new devices using the caller's transaction, without device history.</summary>
        private static async Task UpsertAsync(GXDbConnection connection, IDbTransaction transaction,
            List<GXDevice> newDevices, GXUser creator, DateTime now, bool lateBinding,
            CancellationToken cancellationToken)
        {
            if (newDevices.Count != 0)
            {
                if (lateBinding)
                {
                    foreach (var device in newDevices)
                    {
                        device.TraceLevel ??= TraceLevel.Off;
                        device.Creator = creator;
                        device.CreationTime = now;
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newDevices);
                    args.Exclude<GXDevice>(e => new
                    {
                        e.Objects,
                        e.Updated,
                        e.Removed,
                        e.DeviceGroups,
                        e.Properties
                    });
                    await connection.InsertAsync(transaction, args);
                    foreach (var device in newDevices)
                    {
                        //Add device properties.
                        await AddDeviceProperties(connection, transaction, device, device.Properties, cancellationToken);
                        //Add device to the default device group.
                        await AddDeviceToDeviceGroups(connection, transaction, device.Id, device?.DeviceGroups, cancellationToken);
                    }
                }
                else
                {
                    foreach (GXDevice device in newDevices)
                    {
                        //Add new device.
                        device.TraceLevel ??= TraceLevel.Off;
                        device.Type = device.Template!.Type;
                        GXInsertArgs args = GXInsertArgs.Insert(device);
                        args.Exclude<GXDevice>(q => new
                        {
                            q.Objects,
                            q.DeviceGroups,
                            q.Updated,
                            q.Properties
                        });
                        device.Creator = creator;
                        device.CreationTime = now;
                        await connection.InsertAsync(transaction, args, cancellationToken);
                        GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectTemplate>(q => q.Removed == null);
                        arg.Distinct = true;
                        arg.Columns.Add<GXAttributeTemplate>();
                        arg.Joins.AddLeftJoin<GXObjectTemplate, GXAttributeTemplate>(o => o.Id, a => a.ObjectTemplate!);
                        arg.Joins.AddLeftJoin<GXObjectTemplate, GXDeviceTemplate>(o => o.DeviceTemplate!, a => a.Id);
                        arg.Where.And<GXAttributeTemplate>(q => q.Removed == null);
                        arg.Where.And<GXDeviceTemplate>(q => q.Removed == null && q.Id == device.Template.Id);
                        arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
                        List<GXObjectTemplate> l = await connection.SelectAsync<GXObjectTemplate>(transaction, arg, cancellationToken);
                        foreach (GXObjectTemplate it in l)
                        {
                            var initial = device.Objects?.FirstOrDefault(o => o.Template?.Id == it.Id ||
                                (o.Template?.ObjectType == it.ObjectType && o.Template?.LogicalName == it.LogicalName));
                            await ObjectRepository.CreateObjectAsync(connection, transaction, device, it, now,
                                initial?.Attributes, cancellationToken);
                        }
                        //Add device properties.
                        await AddDeviceProperties(connection, transaction, device, device.Properties, cancellationToken);
                        //Add device to the default device group.
                        await AddDeviceToDeviceGroups(connection, transaction, device.Id, device.DeviceGroups, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Map device template to device groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device ID.</param>
        /// <param name="groups">Device groups where the device is added.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static Task<int> AddDeviceToDeviceGroups(GXDbConnection connection, IDbTransaction transaction,
            Guid deviceTemplateId,
            IEnumerable<GXDeviceGroup>? groups,
            CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceGroupDevice> list = [];
            foreach (GXDeviceGroup it in (groups ?? []))
            {
                list.Add(new GXDeviceGroupDevice()
                {
                    DeviceId = deviceTemplateId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            return connection.InsertAsync(transaction, GXInsertArgs.InsertRange(list), cancellationToken);
        }

        /// <summary>
        /// Remove map between device group and device.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Device template groups where the device template is removed.</param>
        private static void RemoveDevicesFromDeviceGroup(
            GXDbConnection connection, IDbTransaction transaction,
            Guid deviceTemplateId,
            IEnumerable<GXDeviceGroup> groups)
        {
            foreach (var it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXDeviceGroupDevice>(w => w.DeviceId == deviceTemplateId && w.DeviceGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add device parameters.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="device">Device where parameters are added.</param>
        /// <param name="parameters">Added device parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static async Task AddDeviceProperties(GXDbConnection connection, IDbTransaction transaction,
            GXDevice device, IEnumerable<GXDeviceProperty>? parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters != null)
            {
                await GXPropertyStores.Device.ReplaceAsync(connection, transaction, device, parameters, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(
            Guid deviceId,
            DeviceStatus status, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Edit);
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Status },
                where => where.Id == deviceId && where.Removed == null);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXDevice? device = await connection.SingleOrDefaultAsync<GXDevice>(args) ?? throw new GXAmiNotFoundException(Properties.Resources.Device + " " +
                    Properties.Resources.Id + " " + deviceId.ToString());
            var previousStatus = device.Status;
            device.Status = status;
            device.Detected = DateTime.Now;
            GXUpdateArgs update;
            update = GXUpdateArgs.Update(device, c => new { c.Status, c.Detected });
            await connection.UpdateAsync(update, cancellationToken);
            if (previousStatus != status && (status == DeviceStatus.Connected || status == DeviceStatus.Disconnected))
                {
                PublishDeviceEvent(status == DeviceStatus.Connected ? "device.connected" : "device.disconnected", device.Id);
            }
            _notificationCache.Add(await GetNotifiedUsers(device.Id, NotificationAction.StatusChange, connection),
                       new GXTarget(GXDevicePolicies.Edit, device.Id, device.Name,
                       Properties.Resources.Detected + device.Detected + " " + Properties.Resources.Status + " " + device.Status));

            //Update device state for the device log.
            GXLog? log = new(TraceLevel.Info)
            {
                Creator = device.Id
            };
            switch (status)
            {
                case DeviceStatus.Connected:
                    log.Message = Properties.Resources.Connected;
                    break;
                case DeviceStatus.Disconnected:
                    log.Message = Properties.Resources.Disconnected;
                    break;
                case DeviceStatus.Error:
                    log.Message = Properties.Resources.Error;
                    break;
                default:
                    log = null;
                    break;
            }
            if (log != null)
            {
                //Add device log. Idle status is not logged.
                ILogRepository repository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("device-error");
                await repository.AddAsync(TargetType.Device, [log], default);
            }
        }

        /// <inheritdoc />
        public Task ResetAsync(IEnumerable<Guid> devices)
        {
            throw new NotImplementedException();
        }
    }
}
