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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
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
    public class ObjectTemplateRepository : IObjectTemplateRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectTemplateRepository(IDbConnectionFactory dbFactory,
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
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXObjectTemplate), id, action, GXDeviceTemplatePolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default)
        {
            GXSelectArgs args = GXQuery.GetUsersByObjectTemplate(ServerHelpers.GetUserId(User), objectId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (objectId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXObjectTemplate), objectId, token: cancellationToken);
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
            GXSelectArgs args = GXQuery.GetUsersByObjectTemplates(ServerHelpers.GetUserId(User), objectIds);
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
            await GXHistoryMutation.RunDeleteAsync<GXObjectTemplate, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplatePolicies.Delete);
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXObjectTemplate>(a => a.Id, q => objects.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXObjectTemplate> list = connection.Select<GXObjectTemplate>(arg);
                DateTime now = DateTime.Now;
                var removedRecipients = new HashSet<string>();
                foreach (GXObjectTemplate it in list)
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
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXObjectTemplate>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                }
                List<string>? users = [.. removedRecipients];
                List<GXObjectTemplate> list2 = [];
                foreach (var it in list)
                {
                    list2.Add(new GXObjectTemplate() { Id = it.Id });
                }
                users = [.. removedRecipients];
                if (users != null && users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXObjectTemplatePolicies.Delete));
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
        public async Task<IEnumerable<GXObjectTemplate>> ListAsync(
            ListObjectTemplates? request,
            ListObjectTemplatesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplatePolicies.View);
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetObjectTemplatesByUser(userId, null);
            arg.Distinct = true;
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXObjectTemplate>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXObjectTemplate>(w => request.Included.Contains(w.Id));
                }
                if (request.DeviceTemplates != null && request.DeviceTemplates.Any())
                {
                    arg.Where.And<GXDeviceTemplate>(w => request.DeviceTemplates.Contains(w.Id));
                }
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
            GXDateTimeFilters.Apply<GXObjectTemplate>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXObjectTemplate>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXObjectTemplate>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXObjectTemplate>(q => q.Id);
            }
            GXObjectTemplate[] objects = [.. (await connection.SelectAsync<GXObjectTemplate>(arg))];
            if (response != null)
            {
                response.ObjectTemplates = objects;
                if (response.Count == 0)
                {
                    response.Count = objects.Length;
                }
            }
            return objects;
        }

        /// <inheritdoc />
        public async Task<GXObjectTemplate> ReadAsync(Guid id, Expression<Func<GXObjectTemplate, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplatePolicies.View);
            try
            {
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXQuery.GetObjectTemplatesByUser(userId, id);
                //Device ID and name are added.
                arg.Columns.Add<GXDeviceTemplate>(c => new { c.Id, c.Name });
                arg.Columns.Exclude<GXDeviceTemplate>(e => e.Objects!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXObjectTemplate? obj = await connection.SingleOrDefaultAsync<GXObjectTemplate>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get attribute templates .
                arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(w => w.ObjectTemplate == obj && w.Removed == null);
                arg.Columns.Add<GXAttributeListItem>();
                arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template!);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate!);
                arg.Columns.Exclude<GXAttributeListItem>(e => e.Template!);
                obj.Attributes = await connection.SelectAsync<GXAttributeTemplate>(arg);
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
            IEnumerable<GXObjectTemplate> objects,
            Expression<Func<GXObjectTemplate, object>>? columns, CancellationToken cancellationToken = default)
        {
            objects = objects.ToArray();
            return await GXHistoryMutation.RunAsync<GXObjectTemplate, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplatePolicies.Edit);
            try
            {
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (var obj in objects)
                {
                    if (obj.Id == Guid.Empty)
                    {
                        obj.CreationTime = now;
                        await connection.InsertAsync(GXInsertArgs.Insert(obj), cancellationToken);
                    }
                    else
                    {
                        if (columns != null)
                        {
                            await connection.UpdateAsync(GXUpdateArgs.Update(obj, columns), cancellationToken);
                        }
                        //Update attribute data type, UI data type, scaler, access rights and expiration times.
                        if (obj.Attributes != null)
                        {
                            foreach (var att in obj.Attributes)
                            {
                                await connection.UpdateAsync(GXUpdateArgs.Update(att, u => new
                                {
                                    u.DefaultValue,
                                    u.DataType,
                                    u.UIDataType,
                                    u.Scaler,
                                    u.Unit,
                                    u.AccessLevel,
                                    u.ExpirationTime
                                }), cancellationToken);
                            }
                        }
                    }
                }
                var ret = objects.Select(s => s.Id).ToArray();
                List<string> users = await GetNotifiedUsers(ret, NotificationAction.Edit, connection);
                List<GXObjectTemplate> list = [];
                foreach (var it in objects)
                {
                    list.Add(new GXObjectTemplate()
                    {
                        Id = it.Id,
                    });
                    _notificationCache.Add(users, new GXTarget(GXObjectTemplatePolicies.Edit));
                }
                return [.. ret];

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
        }, cancellationToken);
        }
    }
}
