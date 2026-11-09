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
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class AttributeTemplateRepository : IAttributeTemplateRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttributeTemplateRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
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
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAttributeTemplate), id, action, GXDeviceTemplatePolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAttributeTemplate(ServerHelpers.GetUserId(User), objectId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (objectId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXAttributeTemplate), objectId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByAttributeTemplates(ServerHelpers.GetUserId(User), objectIds);
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
            await GXHistoryMutation.RunDeleteAsync<GXAttributeTemplate, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.Delete);
            objects.ValidateIds();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXAttributeTemplate>(a => a.Id, q => objects.Contains(q.Id));
                List<GXAttributeTemplate> list = connection.Select<GXAttributeTemplate>(arg);
                DateTime now = DateTime.Now;
                var removedRecipients = new HashSet<string>();
                foreach (GXAttributeTemplate it in list)
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
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAttributeTemplate>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                }
                List<string>? users = [.. removedRecipients];
                List<GXAttributeTemplate> list2 = [];
                foreach (var it in list)
                {
                    list2.Add(new GXAttributeTemplate() { Id = it.Id });
                }
                users = [.. removedRecipients];
                if (users != null && users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXAttributeTemplatePolicies.Delete));
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
        public async Task<IEnumerable<GXAttributeTemplate>> ListAsync(
            ListAttributeTemplates? request,
            ListAttributeTemplatesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.View);
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetAttributeTemplatesByUser(s => s.Id, userId, null);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXAttributeTemplate>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXAttributeTemplate>(w => request.Included.Contains(w.Id));
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
            GXDateTimeFilters.Apply<GXAttributeTemplate>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAttributeTemplate>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXAttributeTemplate>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXAttributeTemplate>(q => q.Id);
            }
            if (request?.Select != null && request.Select.Contains(TargetType.ObjectTemplate))
            {
                //If client wants to know object template information.
                arg.Columns.Add<GXObjectTemplate>(w => new { w.Id, w.Name, w.LogicalName });
                arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes!);
            }
            GXAttributeTemplate[] templates = [.. (await connection.SelectAsync<GXAttributeTemplate>(arg))];
            if (response != null)
            {
                response.AttributeTemplates = templates;
                if (response.Count == 0)
                {
                    response.Count = templates.Length;
                }
            }
            return templates;
        }

        /// <inheritdoc />
        public async Task<GXAttributeTemplate> ReadAsync(Guid id, Expression<Func<GXAttributeTemplate, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXQuery.GetAttributeTemplatesByUser(s => s.Id, userId, id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXAttributeTemplate? obj = await connection.SingleOrDefaultAsync<GXAttributeTemplate>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
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
            IEnumerable<GXAttributeTemplate> objects,
            Expression<Func<GXAttributeTemplate, object>>? columns, CancellationToken cancellationToken = default)
        {
            objects = objects.ToArray();
            return await GXHistoryMutation.RunAsync<GXAttributeTemplate, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, objects,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXAttributeTemplatePolicies.Add, GXAttributeTemplatePolicies.Edit);
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (var obj in objects)
                {
                    if (obj.Id == Guid.Empty)
                    {
                        obj.CreationTime = now;
                        await connection.InsertAsync(GXInsertArgs.Insert(obj), cancellationToken);
                    }
                    else if (columns != null)
                    {
                        await connection.UpdateAsync(GXUpdateArgs.Update(obj, columns), cancellationToken);
                    }
                }
                List<string> users = await GetNotifiedUsers(objects.Select(s => s.Id).ToArray(), NotificationAction.Edit, connection);
                List<GXAttributeTemplate> list = [];
                foreach (var it in objects)
                {
                    list.Add(new GXAttributeTemplate()
                    {
                        Id = it.Id
                    });
                    _notificationCache.Add(users, new GXTarget(GXAttributeTemplatePolicies.Edit));
                }
                return [.. objects.Select(s => s.Id)];

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
