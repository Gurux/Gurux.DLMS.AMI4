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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ObjectRepository : IObjectRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            NotificationCache notificationCache,
            IAuthorizationService authorizationService)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXObject), id, action, GXDevicePolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View, GXAgentPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByObject(ServerHelpers.GetUserId(User), objectId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (objectId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXObject), objectId, token: cancellationToken);
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

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? objectIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View, GXAgentPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByObjects(ServerHelpers.GetUserId(User), objectIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> objects,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            objects = objects.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXObject, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXObject>(a => a.Id, q => objects.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXObject> list = connection.Select<GXObject>(arg);
                DateTime now = DateTime.Now;
                var removedRecipients = new HashSet<string>();
                foreach (GXObject it in list)
                {
                    removedRecipients.UnionWith(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection));
                    it.Removed = now;
                    List<string> users2 = await GetUsersAsync(it.Id, cancellationToken);
                    if (users2.Count == 0)
                    {
                        //User doesn't have access rights for this object.
                        throw new UnauthorizedAccessException();
                    }
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXObject>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                }
                List<string>? users = [.. removedRecipients];
                List<GXObject> list2 = [];
                foreach (var it in list)
                {
                    list2.Add(new GXObject() { Id = it.Id });
                }
                users = [.. removedRecipients];
                if (users != null)
                {
                    _notificationCache.Add(users, new GXTarget(GXObjectPolicies.Delete));
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
                        await DeleteAsync(objects, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXObject>> ListAsync(
            ListObjects? request,
            ListObjectsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
            GXSelectArgs arg;
            GXObjectTemplate? objectTemplate = null;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            string userId = ServerHelpers.GetUserId(User);
            if (allUsers)
            {
                //Admin can see all the devices.
                arg = GXSelectArgs.SelectAll<GXDevice>();
            }
            else
            {
                arg = GXSelectArgs.Select<GXDevice>(s => s.Id, q => q.Removed == null);
                GXQuery.GetDevicesByUser(arg, userId);
            }
            arg.Joins.AddLeftJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
            arg.Joins.AddLeftJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>();
            arg.Columns.Exclude<GXDevice>(e => new { e.Creator, e.CreationTime });
            arg.Columns.Add<GXObject>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
            arg.Columns.Exclude<GXDeviceTemplate>(e => new { e.Objects });
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            GXDevice? device = null;
            try
            {
                if (request != null)
                {
                    if (request.Devices != null && request.Devices.Any())
                    {
                        arg.Where.And<GXDevice>(w => request.Devices.Contains(w.Id));
                    }

                    if (request.Select != null && request.Select.Contains("DeviceTemplate"))
                    {
                    }
                    else if (request.Select != null && request.Select.Contains("Device"))
                    {
                    }
                    if (request.Select != null && request.Select.Contains(TargetType.Attribute))
                    {
                        arg.Columns.Add<GXAttribute>();
                        arg.Joins.AddLeftJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                        if (request.Select != null && request.Select.Contains(TargetType.AttributeTemplate))
                        {
                            arg.Columns.Add<GXAttributeTemplate>();
                            arg.Joins.AddLeftJoin<GXAttribute, GXAttributeTemplate>(j => j.Template!, j => j.Id);
                        }
                    }

                    if (request.Filter?.Device != null)
                    {
                        var dg = request.Filter.Device.DeviceGroups?.FirstOrDefault();
                        if (dg != null)
                        {
                            var ug = dg.UserGroups?.FirstOrDefault();
                            if (ug != null)
                            {
                                var user = ug.Users?.FirstOrDefault();
                                if (user != null)
                                {
                                    if (allUsers)
                                    {
                                        arg.Joins.AddLeftJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                                    }
                                    arg.Joins.AddLeftJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                                    arg.Joins.AddLeftJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                                    arg.Joins.AddLeftJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                                    arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                                    arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                                    user.UserGroups = null;
                                    arg.Where.FilterBy(user);
                                }
                            }
                        }
                        else if (request.Filter.Device.Id is Guid id && id != Guid.Empty)
                        {
                            arg.Where.And<GXDevice>(w => w.Id == id);
                        }
                        if (request.Filter.Device.Name is string dn)
                        {
                            arg.Where.And<GXDevice>(w => w.Name!.Contains(dn));
                        }
                        if (request.Filter.Device?.Template?.Name is string tn)
                        {
                            arg.Where.And<GXDeviceTemplate>(w => w.Name!.Contains(tn));
                        }
                        device = request.Filter.Device;
                        request.Filter.Device = null;
                    }
                    if (request.Filter?.Template != null)
                    {
                        objectTemplate = request.Filter.Template;
                        request.Filter.Template = null;
                    }
                    arg.Where.FilterBy(request.Filter);
                    if (request.ObjectTypes != null && request.ObjectTypes.Any())
                    {
                        int?[] tmp = [.. request.ObjectTypes.Cast<int?>()];
                        arg.Where.And<GXObjectTemplate>(w => tmp.Contains(w.ObjectType));
                    }
                    if (request.IgnoredObjectTypes != null && request.IgnoredObjectTypes.Any())
                    {
                        int?[] tmp = [.. request.IgnoredObjectTypes.Cast<int?>()];
                        arg.Where.And<GXObjectTemplate>(w => !tmp.Contains(w.ObjectType));
                    }
                }
                //Get devices before template filter or device is not retured if all objects are late binded.
                var dateFiltered = GXDateTimeFilters.Apply<GXObject>(arg, request?.DateFilters);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                // Project devices separately from the object graph. With no persisted objects,
                // the null object-template join can otherwise hide the device's template.
                GXSelectArgs devicesQuery = GXSelectArgs.Select<GXDevice>(d => new { d.Id, d.Name, d.Template });
                devicesQuery.Columns.Add<GXDeviceTemplate>(t => t.Id);
                devicesQuery.Joins.Append(arg.Joins);
                devicesQuery.Where.Append(arg.Where);
                devicesQuery.Distinct = true;
                GXDevice[] devs = [.. (await connection.SelectAsync<GXDevice>(devicesQuery))];
                if (objectTemplate != null)
                {
                    arg.Where.FilterBy(objectTemplate);
                }
                long count2 = 0;
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXObject>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXObject>(w => request.Included.Contains(w.Id));
                }
                if (request != null && request.Count != 0)
                {
                    //Return total row count. This can be used for paging.
                    GXSelectArgs total = GXSelectArgs.Select<GXObject>(q => GXSql.Count(1));
                    total.Joins.Append(arg.Joins);
                    total.Where.Append(arg.Where);
                    if (response != null)
                    {
                        response.Count = connection.SingleOrDefault<long>(total);
                        count2 = response.Count;
                    }
                    if (!dateFiltered && count2 < request.Index)
                    {
                        //Handle index for late bind objects.
                        arg.Index = count2;
                        request.Index -= count2;
                    }
                    else
                    {
                        arg.Index = request.Index;
                    }
                    arg.Count = request.Count;
                }
                if (request != null && !string.IsNullOrEmpty(request.OrderBy))
                {
                    arg.Descending = request.Descending;
                    arg.OrderBy.Add<GXObject>(request.OrderBy);
                }
                else
                {
                    arg.Descending = true;
                    arg.OrderBy.Add<GXObject>(q => q.Id);
                }
                List<GXObject> objects = [.. (await connection.SelectAsync<GXObject>(arg))];
                Guid[] deviceTemplateIds = [.. devs.Where(s => s.Template != null).Select(s => s.Template!.Id)];
                arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
                arg.Columns.Add<GXObjectTemplate>();
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
                arg.Where.And<GXDeviceTemplate>(q => deviceTemplateIds.Contains(q.Id));
                if (objectTemplate != null)
                {
                    arg.Where.FilterBy(objectTemplate);
                }
                GXDeviceTemplate[] deviceTemplates = dateFiltered ? [] : [.. (await connection.SelectAsync<GXDeviceTemplate>(arg))];
                long count = 0;
                long index = 0;
                if (request != null && request.Count != 0)
                {
                    count = request.Count;
                    if (request?.Exclude != null && request.Exclude.Any())
                    {
                        count += Math.Min(request.Exclude.LongCount(), long.MaxValue - count);
                    }
                    index = request?.Index ?? 0;
                }
                long objectCount = 0;
                bool lateBinded = false;
                if (deviceTemplates.Length > 0)
                {
                    foreach (var dev in devs)
                    {
                        GXDeviceTemplate? devTemplate = deviceTemplates.SingleOrDefault(w => w.Id == dev.Template?.Id);
                        if (devTemplate == null)
                        {
                            continue;
                        }
                        devTemplate.Objects ??= [];
                        //Remove device objects that are not in device template.
                        if (dev.Objects != null)
                        {
                            var tmp = devTemplate.Objects.Select(s => s.Id);
                            var tmp2 = dev.Objects.Where(s => s.Template != null).Select(s => s.Template!.Id);
                            List<GXObject> remove = [];
                            foreach (var it in dev.Objects)
                            {
                                if (it.Template == null || !tmp.Contains(it.Template.Id))
                                {
                                    remove.Add(it);
                                }
                            }
                            dev.Objects.RemoveAll(w => remove.Contains(w));
                        }
                        objectCount += devTemplate.Objects.Count;
                        if (dev.Objects == null || dev.Objects.Count != devTemplate.Objects.Count)
                        {
                            //Get late bind objects.
                            foreach (var it in devTemplate.Objects)
                            {
                                it.DeviceTemplate = null;
                                if (count != 0 && count == objects.Count - index)
                                {
                                    break;
                                }
                                if (!objects.Where(w => w.Template != null && w.Template.Id == it.Id &&
                                w.Device != null && w.Device.Id == dev.Id).Any())
                                {
                                    lateBinded = true;
                                    objects.Add(new GXObject(it)
                                    {
                                        //Template ID is used as a ID for late bind objects.
                                        Id = it.Id,
                                        Latebind = true,
                                        Device = new GXDevice() { Id = dev.Id, Name = dev.Name },
                                    });
                                }
                            }
                        }
                        else
                        {
                            int removed = dev.Objects.Count;
                            if (request?.Exclude != null && request.Exclude.Any())
                            {
                                objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                                removed = dev.Objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                                objects.AddRange(dev.Objects);
                            }
                            count -= removed;
                        }
                    }
                }
                if (lateBinded)
                {
                    if (response != null)
                    {
                        response.Count = objectCount;
                        if (request?.Exclude != null && request.Exclude.Any())
                        {
                            response.Count -= request.Exclude.Count();
                        }
                    }
                }
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    //Remove excluded objects.
                    objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                }
                if (request != null && request.Count != 0 && (request.Count < objects.Count || (lateBinded && request.Index > 0)))
                {
                    //Remove extra objects.
                    objects = [.. objects.Skip((int)Math.Min(request.Index, objects.Count)).Take((int)Math.Min(request.Count, objects.Count))];
                }
                //Only device id and name are send.
                foreach (var obj in objects)
                {
                    if (obj.Device != null)
                    {
                        obj.Device = new GXDevice() { Id = obj.Device.Id, Name = obj.Device.Name };
                    }
                }
                if (request?.Select != null && request.Select.Contains("Attribute"))
                {
                    arg = GXSelectArgs.Select<GXAttribute>(s => new { s.Id, s.Template, s.Value });
                    arg.Columns.Add<GXAttributeTemplate>(s => new { s.Id, s.Name });
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template!, j => j.Id);
                    foreach (var it in objects)
                    {
                        arg.Where.Clear();
                        arg.Where.And<GXAttribute>(w => w.Object == it);
                        it.Attributes = [.. (await connection.SelectAsync<GXAttribute>(arg))];
                    }
                }

                if (response != null)
                {
                    response.Objects = [.. objects];
                    if (response.Count == 0 || response.Count < objects.Count)
                    {
                        response.Count = objects.Count;
                    }
                }
                return [.. objects];
            }
            finally
            {
                //Return original filter device if used.
                if (request?.Filter != null)
                {
                    request.Filter.Device = device;
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXObject> ReadAsync(Guid id, Expression<Func<GXObject, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View,
                    GXAgentPolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXQuery.GetObjectsByUser(s => s.Id, userId, id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXObject? obj = await connection.SingleOrDefaultAsync<GXObject>(arg);
                if (obj == null)
                {
                    //If late binding.
                    arg = GXQuery.GetObjectTemplatesByUser(userId, id);
                    arg.Distinct = true;
                    GXObjectTemplate? template = await connection.SingleOrDefaultAsync<GXObjectTemplate>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                    obj = new GXObject(template)
                    {
                        Id = template.Id,
                        Latebind = true
                    };
                    //Get attributes.
                    arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(w => w.ObjectTemplate == template && w.Removed == null);
                    arg.Columns.Add<GXAttributeListItem>();
                    arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(x => x.Id, y => y.ObjectTemplate!);
                    arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template!);
                    arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate!);
                    arg.Columns.Exclude<GXAttributeListItem>(e => e.Template!);
                    arg.OrderBy.Add<GXAttributeTemplate>(o => o.Index);
                    var templates = await connection.SelectAsync<GXAttributeTemplate>(arg);
                    foreach (var it in templates)
                    {
                        (obj.Attributes ??= []).Add(new GXAttribute(it)
                        {
                            Id = it.Id,
                            Value = it.DefaultValue,
                        });
                    }
                }
                else
                {
                    //Get object template.
                    arg = GXSelectArgs.SelectAll<GXObjectTemplate>(w => w.Removed == null);
                    arg.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(y => y.Id, x => x.Template!);
                    arg.Where.And<GXObject>(w => w.Id == obj.Id);
                    obj.Template = await connection.SingleOrDefaultAsync<GXObjectTemplate>(arg);

                    //Get attributes.
                    arg = GXSelectArgs.SelectAll<GXAttribute>(w => w.Object == obj && w.Removed == null);
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Columns.Add<GXAttributeListItem>();
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(x => x.Id, y => y.Object!);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(x => x.Template!, y => y.Id);
                    arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template!);
                    arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate!);
                    arg.Columns.Exclude<GXAttributeListItem>(e => e.Template!);
                    arg.OrderBy.Add<GXAttributeTemplate>(o => o.Index);
                    obj.Attributes = await connection.SelectAsync<GXAttribute>(arg);
                    //Get object parameters.
                    obj.Properties = await GXPropertyStores.Object.ListAsync(connection, null, obj, cancellationToken);
                }
                return obj;

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

        /// <summary>
        /// Return device object using device ID and logical name of the object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userId"></param>
        /// <param name="device"></param>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        /// <param name="connection">The database connection.</param>
        private static async Task<GXObject?> GetDeviceObjectUsingLogicalName(
            GXDbConnection connection,
            IDbTransaction? transaction,
            string userId,
            GXDevice device,
            string logicalName)
        {
            Guid deviceId = device.Id;
            GXSelectArgs args = GXQuery.GetObjectsByUser(s => s.Id, userId);
            args.Columns.Add<GXObjectTemplate>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template!, a => a.Id);
            args.Where.And<GXDevice>(q => q.Id == deviceId);
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObjectTemplate>(q => q.Removed == null && q.LogicalName == logicalName);
            return transaction == null
                ? await connection.SingleOrDefaultAsync<GXObject>(args)
                : await connection.SingleOrDefaultAsync<GXObject>(transaction, args);
        }

        /// <summary>
        /// Return device object using device ID and logical name of the object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userId">User ID.</param>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="logicalName">Logical name of the object.</param>
        /// <param name="index">Attribute index</param>
        /// <returns></returns>
        /// <param name="connection">The database connection.</param>
        public static async Task<GXAttribute?> GetDeviceAttributeUsingLogicalName(
             GXDbConnection connection,
            IDbTransaction? transaction,
            string userId,
            Guid deviceId,
            string logicalName,
            int index)
        {
            GXSelectArgs args = GXQuery.GetAttributesByUser(s => s.Id, userId);
            args.Columns.Add<GXObjectTemplate>();
            args.Columns.Add<GXAttributeTemplate>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template!, a => a.Id);
            args.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(o => o.Template!, a => a.Id);
            args.Where.And<GXDevice>(q => q.Id == deviceId);
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObjectTemplate>(q => q.Removed == null && q.LogicalName == logicalName);
            args.Where.And<GXAttribute>(q => q.Removed == null);
            args.Where.And<GXAttributeTemplate>(q => q.Removed == null && q.Index == index);
            return transaction == null
                ? await connection.SingleOrDefaultAsync<GXAttribute>(args)
                : await connection.SingleOrDefaultAsync<GXAttribute>(transaction, args);
        }

        internal static async Task<GXObject> CreateLateBindObject(GXDbConnection connection,
            IDbTransaction? transaction,
            string userId, GXDevice device, Guid objectTemplateId)
        {
            // Device access authorizes using its assigned model, without catalog-view permissions.
            GXSelectArgs args = GXQuery.GetDevicesByUser(d => d.Id, userId, device.Id);
            args.Columns.Clear();
            args.Columns.Add<GXObjectTemplate>();
            args.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(d => d.Template!, t => t.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(t => t.Id, o => o.DeviceTemplate!);
            args.Where.And<GXDeviceTemplate>(t => t.Removed == null);
            args.Where.And<GXObjectTemplate>(o => o.Id == objectTemplateId && o.Removed == null);
            args.Distinct = true;
            GXObjectTemplate? template = await connection.SingleOrDefaultAsync<GXObjectTemplate>(transaction, args);
            if (template == null)
            {
                throw new GXAmiForbiddenException(objectTemplateId.ToString());
            }
            var attributeTemplates = GXSelectArgs.SelectAll<GXAttributeTemplate>(a => a.Removed == null);
            attributeTemplates.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(a => a.ObjectTemplate!, t => t.Id);
            attributeTemplates.Where.And<GXObjectTemplate>(t => t.Id == objectTemplateId);
            template.Attributes = await connection.SelectAsync<GXAttributeTemplate>(transaction, attributeTemplates);
            // The unique key includes removed objects, which must never be recreated automatically.
            var existingQuery = GXSelectArgs.SelectAll<GXObject>();
            existingQuery.Joins.AddInnerJoin<GXObject, GXDevice>(o => o.Device!, d => d.Id);
            existingQuery.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template!, t => t.Id);
            existingQuery.Where.And<GXDevice>(d => d.Id == device.Id);
            existingQuery.Where.And<GXObjectTemplate>(t => t.Id == objectTemplateId);
            var existing = await connection.SingleOrDefaultAsync<GXObject>(transaction, existingQuery);
            if (existing != null)
            {
                if (existing.Removed != null)
                {
                    throw new ArgumentException("The device object has been removed. Restore it before creating tasks.");
                }
                existing.Device = device;
                existing.Template = template;
                var attributes = GXSelectArgs.SelectAll<GXAttribute>(a => a.Removed == null);
                attributes.Joins.AddInnerJoin<GXAttribute, GXObject>(a => a.Object!, o => o.Id);
                attributes.Where.And<GXObject>(o => o.Id == existing.Id);
                attributes.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, t => t.Id);
                attributes.Columns.Add<GXAttributeTemplate>();
                attributes.Where.And<GXAttributeTemplate>(t => t.Removed == null);
                existing.Attributes = await connection.SelectAsync<GXAttribute>(transaction, attributes);
                foreach (var attribute in existing.Attributes)
                {
                    attribute.Object = existing;
                }
                return existing;
            }
            return await CreateObjectAsync(connection, transaction, device, template, DateTime.Now);
        }

        /// <summary>Creates an object and its attributes from an already authorized template in the caller's transaction.</summary>
        /// <remarks>Authorization and existing-object resolution belong to the caller. Initial values override template defaults.</remarks>
        internal static async Task<GXObject> CreateObjectAsync(GXDbConnection connection, IDbTransaction? transaction,
            GXDevice device, GXObjectTemplate template, DateTime creationTime,
            IEnumerable<GXAttribute>? initialValues = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var obj = new GXObject(template) { CreationTime = creationTime, Device = device };
            obj.Attributes = [];
            foreach (var attributeTemplate in template.Attributes ?? [])
            {
                if (attributeTemplate.Removed == null)
                {
                    obj.Attributes.Add(new GXAttribute(attributeTemplate)
                    {
                        Object = obj,
                        CreationTime = creationTime,
                        ExpirationTime = attributeTemplate.ExpirationTime,
                        Value = attributeTemplate.DefaultValue
                    });
                }
            }
            foreach (var initial in initialValues ?? [])
            {
                if (!string.IsNullOrEmpty(initial.Value))
                {
                    var attributeTemplate = initial.Template ?? throw new ArgumentException("Template is missing.");
                    var attribute = obj.Attributes.SingleOrDefault(a => attributeTemplate.Id != Guid.Empty
                        ? a.Template!.Id == attributeTemplate.Id : a.Template!.Index == attributeTemplate.Index);
                    if (attribute != null)
                    {
                        attribute.Value = initial.Value;
                    }
                }
            }
            var insert = GXInsertArgs.Insert(obj);
            insert.Exclude<GXObject>(o => o.Attributes!);
            await connection.InsertAsync(transaction, insert, cancellationToken);
            if (obj.Attributes.Count != 0)
            {
                await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(obj.Attributes), cancellationToken);
            }
            return obj;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXObject> objects,
            Expression<Func<GXObject, object>>? columns, CancellationToken cancellationToken = default)
        {
            objects = objects.ToArray();
            return await GXHistoryMutation.RunAsync<GXObject, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Edit,
                GXAgentPolicies.Edit);
            string userId = ServerHelpers.GetUserId(User);
            GXDevice? device = null;
            DateTime now = DateTime.Now;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var it in objects)
                {
                    if (it.Id == Guid.Empty)
                    {
                        it.CreationTime = now;
                        var insert = GXInsertArgs.Insert(it);
                        insert.Exclude<GXObject>(p => p.Properties!);
                        await connection.InsertAsync(transaction, insert, cancellationToken);
                    }
                    else
                    {
                        //Check object.
                        GXSelectArgs args = GXQuery.GetObjectsByUser(s => s.Id, userId, it.Id);
                        args.Distinct = true;
                        GXObject? obj = await connection.SingleOrDefaultAsync<GXObject>(transaction, args);
                        if (obj == null)
                        {
                            //Check if this is late binding.
                            if (it.Device == null)
                            {
                                throw new GXAmiForbiddenException(it.Id.ToString());
                            }
                            if (device?.Template == null)
                            {
                                args = GXQuery.GetDevicesByUser(s => s.Id, userId, it.Device.Id);
                                args.Columns.Add<GXDeviceTemplate>(c => c.Id);
                                args.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                                device = await connection.SingleOrDefaultAsync<GXDevice>(transaction, args);
                                if (device?.Template == null)
                                {
                                    throw new GXAmiForbiddenException(it.Device.Id.ToString());
                                }
                            }
                            await CreateLateBindObject(connection, transaction, userId, device, it.Id);
                        }
                        else
                        {
                            GXUpdateArgs arg = GXUpdateArgs.Update(it, columns);
                            arg.Exclude<GXObject>(p => p.Properties!);
                            if (columns == null)
                            {
                                arg.Exclude<GXObject>(e => new { e.CreationTime, e.Device });
                            }
                            await connection.UpdateAsync(transaction, arg, cancellationToken);
                        }
                    }
                    //Update object parameters.
                    if (it.Properties == null)
                    {
                        if (ServerHelpers.Contains(columns, nameof(GXObject.Properties)))
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                    }
                    else
                    {
                        await GXPropertyStores.Object.ReplacePreservingAsync(connection, transaction, it, it.Properties, cancellationToken,
                            GXProfileHistoryParameters.IsManaged);
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
                        return await UpsertAsync(objects, columns, linkedCts.Token);
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
            var ret = objects.Select(s => s.Id).ToArray();
            List<string> users = await GetNotifiedUsers(ret, NotificationAction.Edit, connection);
            List<GXObject> list = [];
            foreach (var it in objects)
            {
                list.Add(new GXObject()
                {
                    Id = it.Id,
                    LastRead = it.LastRead,
                    LastWrite = it.LastWrite,
                    LastAction = it.LastAction,
                    LastError = it.LastError,
                    LastErrorMessage = it.LastErrorMessage
                });
                _notificationCache.Add(users, new GXTarget(GXObjectPolicies.Edit));
            }
            return [.. ret];
        }, cancellationToken);
        }

    }
}
