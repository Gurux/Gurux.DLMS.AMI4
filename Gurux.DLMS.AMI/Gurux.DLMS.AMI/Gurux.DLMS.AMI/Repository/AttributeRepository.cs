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
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class AttributeRepository : IAttributeRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttributeRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IAuthorizationService authorizationService,
            NotificationCache notificationCache)
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
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAttribute), id, action, GXDevicePolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? attributeId, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args = GXQuery.GetUsersByAttribute(attributeId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (attributeId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXAttribute), attributeId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? attributeIds, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View, GXAgentPolicies.View);
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs args = GXQuery.GetUsersByAttributes(userId, attributeIds);
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
            IEnumerable<Guid> attributers,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            attributers = attributers.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXAttribute, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, attributers,
        async () =>
        {
            attributers.ValidateIds();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Delete);
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXAttribute>(a => a.Id, q => attributers.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXAttribute> list = connection.Select<GXAttribute>(arg);
                DateTime now = DateTime.Now;
                var removedRecipients = new HashSet<string>();
                foreach (GXAttribute it in list)
                {
                    removedRecipients.UnionWith(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection));
                    it.Removed = now;
                    List<string> users2 = await GetUsersAsync(it.Id, cancellationToken);
                    if (users2.Count == 0)
                    {
                        //User doesn't have access rights for this attribute.
                        throw new UnauthorizedAccessException();
                    }
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAttribute>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                }
                List<string>? users = [.. removedRecipients];
                List<GXAttribute> list2 = [];
                foreach (var it in list)
                {
                    list2.Add(new GXAttribute() { Id = it.Id });
                }
                users = [.. removedRecipients];
                if (users != null && users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Delete));
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
                        await DeleteAsync(attributers, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXAttribute>> ListAsync(
            ListAttributes? request,
            ListAttributesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
            GXSelectArgs arg;
            GXAttributeTemplate? attributeTemplate = null;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            string userId = ServerHelpers.GetUserId(User);
            if (allUsers)
            {
                //Admin can see all the devices.
                arg = GXSelectArgs.SelectAll<GXDevice>();
            }
            else
            {
                arg = GXQuery.GetDevicesByUser(s => "*", userId);
            }
            arg.Joins.AddLeftJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
            arg.Joins.AddLeftJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
            arg.Joins.AddLeftJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
            arg.Joins.AddLeftJoin<GXAttribute, GXAttributeTemplate>(j => j.Template!, j => j.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>();
            arg.Columns.Exclude<GXDevice>(e => new { e.Creator, e.CreationTime });
            arg.Columns.Add<GXObject>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXAttribute>();
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
            arg.Columns.Exclude<GXDeviceTemplate>(e => new { e.Objects });
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (request != null)
            {
                if (request.Devices != null && request.Devices.Any())
                {
                    arg.Where.And<GXDevice>(w => request.Devices.Contains(w.Id));
                }
                if (request.Filter?.Object != null)
                {
                    var dg = request.Filter.Object.Device?.DeviceGroups?.FirstOrDefault();
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
                    else if (request.Filter.Object.Device?.Id is Guid id && id != Guid.Empty)
                    {
                        arg.Where.And<GXDevice>(w => w.Id == id);
                    }
                    if (request.Filter.Object.Device?.Name is string dn)
                    {
                        arg.Where.And<GXDevice>(w => w.Name!.Contains(dn));
                    }
                    if (request.Filter.Object.Device?.Template?.Name is string tn)
                    {
                        arg.Where.And<GXDeviceTemplate>(w => w.Name!.Contains(tn));
                    }
                    request.Filter.Object.Device = null;
                }
                if (request.Filter?.Template != null)
                {
                    attributeTemplate = request.Filter.Template;
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
            arg.OrderBy.Add<GXDevice>(q => q.Id);
            arg.Count = 1;
            //Get devices before template filter or device is not retured if all objects are late binded.
            var dateFiltered = GXDateTimeFilters.Apply<GXAttribute>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXDevice[] devs = [.. (await connection.SelectAsync<GXDevice>(arg))];
            arg.Count = 0;
            if (attributeTemplate != null)
            {
                arg.Where.FilterBy(attributeTemplate);
            }
            if (request != null)
            {
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXAttribute>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXAttribute>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAttribute>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXAttribute>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXAttribute>(q => q.Id);
            }
            List<GXAttribute> attributes = [.. (await connection.SelectAsync<GXAttribute>(arg))];
            var deviceTemplateIds = devs.Where(s => s.Template != null).Select(s => s.Template!.Id).ToArray();

            arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
            arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
            arg.Where.And<GXDeviceTemplate>(w => deviceTemplateIds.Contains(w.Id));
            if (attributeTemplate != null)
            {
                arg.Where.FilterBy(attributeTemplate);
            }
            if (request?.ObjectTypes != null && request.ObjectTypes.Any())
            {
                int?[] tmp = [.. request.ObjectTypes.Cast<int?>()];
                arg.Where.And<GXObjectTemplate>(w => tmp.Contains(w.ObjectType));
            }
            if (request?.IgnoredObjectTypes != null && request.IgnoredObjectTypes.Any())
            {
                int?[] tmp = [.. request.IgnoredObjectTypes.Cast<int?>()];
                arg.Where.And<GXObjectTemplate>(w => !tmp.Contains(w.ObjectType));
            }
            GXDeviceTemplate[] deviceTemplates = dateFiltered ? [] : [.. (await connection.SelectAsync<GXDeviceTemplate>(arg))];
            //Remove all attributes that are saved, but not found from the template.
            //Filter might cause this.
            foreach (var dev in devs)
            {
                if (dev.Objects != null)
                {
                    var ids = (deviceTemplates.SingleOrDefault(w => w.Id == dev.Template?.Id)?.Objects ?? []).Select(s => s.Id);
                    dev.Objects.RemoveAll(w => w.Template == null || !ids.Contains(w.Template.Id));
                }
            }
            long count = 0;
            if (request != null && request.Count != 0)
            {
                count = request.Count;
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    count += Math.Min(request.Exclude.LongCount(), long.MaxValue - count);
                }
            }
            long attributeCount = 0;
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
                    attributeCount += devTemplate.Objects.SelectMany(s => s.Attributes ?? []).LongCount();
                    if (dev.Objects == null || dev.Objects.Count != devTemplate.Objects.Count)
                    {
                        //Get late bind objects.
                        foreach (var obj in devTemplate.Objects)
                        {
                            obj.DeviceTemplate = null;
                            if (count != 0 && count == attributes.Count)
                            {
                                if (!attributes.Where(w => w.Template != null && w.Template.Id == obj.Id &&
                                    w.Object?.Device != null && w.Object.Device.Id == dev.Id).Any())
                                {
                                    lateBinded = true;
                                }
                                break;
                            }
                            if (!attributes.Where(w => w.Template != null && w.Template.Id == obj.Id &&
                            w.Object?.Device != null && w.Object.Device.Id == dev.Id).Any())
                            {
                                lateBinded = true;
                                foreach (var it in (obj.Attributes ?? []))
                                {
                                    if (!attributes.Where(w => w.Template?.Id == it.Id).Any())
                                    {
                                        attributes.Add(new GXAttribute(it)
                                        {
                                            //Template ID is used as a ID for late bind objects.
                                            Id = it.Id,
                                            Object = new GXObject(obj)
                                            {
                                                Id = obj.Id,
                                                Latebind = true,
                                                Device = new GXDevice() { Id = dev.Id },
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        long removed = dev.Objects.SelectMany(s => s.Attributes ?? []).LongCount();
                        if (request?.Exclude != null && request.Exclude.Any())
                        {
                            attributes.RemoveAll(w => request.Exclude.Contains(w.Id));
                            removed = dev.Objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                            attributes.AddRange([.. dev.Objects.SelectMany(s => s.Attributes ?? [])]);
                        }
                        count -= removed;
                    }
                }
            }
            if (lateBinded)
            {
                if (response != null)
                {
                    response.Count = attributeCount;
                    if (request?.Exclude != null && request.Exclude.Any())
                    {
                        response.Count -= request.Exclude.Count();
                    }
                }
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                //Remove excluded objects.
                attributes.RemoveAll(w => request.Exclude.Contains(w.Id));
            }
            if (request != null && request.Count != 0 && (request.Count < attributes.Count || (lateBinded && request.Index > 0)))
            {
                //Remove extra objects.
                attributes = [.. attributes.Skip((int)Math.Min(request.Index, attributes.Count)).Take((int)Math.Min(request.Count, attributes.Count))];
            }
            //Only device id is send.
            foreach (var att in attributes)
            {
                if (att.Object != null)
                {
                    att.Object = new GXObject()
                    {
                        Id = att.Object.Id,
                        Device = att.Object.Device == null ? null : new GXDevice()
                        {
                            Id = att.Object.Device.Id,
                            Name = att.Object.Device.Name
                        },
                        Template = att.Object.Template == null ? null : new GXObjectTemplate()
                        {
                            Id = att.Object.Template.Id,
                            LogicalName = att.Object.Template.LogicalName,
                            Name = att.Object.Template.Name
                        }
                    };
                }
                if (att.Template != null)
                {
                    att.Template.ObjectTemplate = null;
                }
            }
            if (response != null)
            {
                response.Attributes = [.. attributes];
                if (response.Count == 0 || response.Count < attributes.Count)
                {
                    response.Count = attributes.Count;
                }
            }
            return [.. attributes];
        }

        /// <inheritdoc />
        public async Task<GXAttribute> ReadAsync(Guid id, Expression<Func<GXAttribute, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXQuery.GetAttributesByUser(s => "*", userId, id);
                arg.Columns.Add<GXAttributeTemplate>();
                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(x => x.Template!, y => y.Id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXAttribute? obj = await connection.SingleOrDefaultAsync<GXAttribute>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get attribute properties.
                obj.Properties = await GXPropertyStores.Attribute.ListAsync(connection, null, obj, cancellationToken);
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

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAttribute> attributes,
            Expression<Func<GXAttribute, object>>? columns, CancellationToken cancellationToken = default)
        {
            attributes = attributes.ToArray();
            return await GXHistoryMutation.RunAsync<GXAttribute, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, attributes,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Add);
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                foreach (var att in attributes)
                {
                    if (att.Id == Guid.Empty)
                    {
                        att.CreationTime = now;
                        var insert = GXInsertArgs.Insert(att);
                        insert.Exclude<GXAttribute>(p => p.Properties!);
                        await connection.InsertAsync(transaction, insert, cancellationToken);
                    }
                    var update = GXUpdateArgs.Update(att, columns);
                    update.Exclude<GXAttribute>(p => p.Properties!);
                    await connection.UpdateAsync(transaction, update, cancellationToken);
                    if (att.Properties != null)
                    {
                        await GXPropertyStores.Attribute.ReplaceAsync(connection, transaction, att, att.Properties, cancellationToken);
                    }
                }
                await transaction.CommitAsync(cancellationToken);
                var users = await GetNotifiedUsers(attributes.Select(s => s.Id), NotificationAction.Edit, connection);
                List<GXAttribute> list = [];
                foreach (var it in attributes)
                {
                    list.Add(new GXAttribute()
                    {
                        Id = it.Id,
                        Template = it.Template,
                        Exception = it.Exception,
                        Value = it.Value,
                        Read = it.Read,
                        Object = it.Object == null ? null : new GXObject()
                        {
                            Id = it.Object.Id,
                        }
                    });
                }
                _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                return [.. attributes.Select(s => s.Id)];

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
                        return await UpsertAsync(attributes, columns, linkedCts.Token);
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
        public async Task UpdateDatatypeAsync(
            IEnumerable<GXAttribute> attributes,
            CancellationToken cancellationToken)
        {
            var values = attributes.ToArray();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDevicePolicies.Edit);
            string userId = ServerHelpers.GetUserId(User);
            await GXHistoryScope.RunAsync(_dbFactory, async connection =>
            {
                foreach (var attribute in values)
                {
                    if (attribute.Template == null || attribute.Id == Guid.Empty)
                    {
                        throw new ArgumentException("Invalid attribute or template.");
                    }
                    var query = GXQuery.GetAttributesByUser(a => a.Id, userId, attribute.Id);
                    var templateId = attribute.Template.Id;
                    query.Where.And<GXObject>(o => o.Removed == null);
                    query.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, t => t.Id);
                    query.Where.And<GXAttributeTemplate>(t => t.Id == templateId && t.Removed == null);
                    if (await connection.SingleOrDefaultAsync<GXAttribute>(query, cancellationToken) == null)
                    {
                        throw new GXAmiForbiddenException(attribute.Id.ToString());
                    }
                }
                // Datatype discovery uses device permissions. Raw history snapshots avoid requiring
                // independent template-catalog permissions after the attribute has been authorized.
                await GXHistoryMutation.RunIdsAsync<GXAttributeTemplate, Guid>(_dbFactory, User, null,
                    values.Select(a => a.Template!.Id), async () =>
                    {
                        var users = await GetNotifiedUsers(values.Select(a => a.Id).ToArray(), NotificationAction.Edit);
                        DateTime now = DateTime.Now;
                        var templates = values.Select(a => a.Template!).ToArray();
                        foreach (var template in templates)
                        {
                            template.Updated = now;
                        }
                        await connection.UpdateAsync(GXUpdateArgs.UpdateRange(templates, t => new
                        {
                            t.DataType,
                            t.Updated
                        }), cancellationToken);
                        _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                    }, cancellationToken);
            }, cancellationToken);
        }
    }
}
