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

using Gurux.DLMS.AMI.Shared.DTOs.Enums;

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Server.Services;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ValueRepository : IValueRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IWorkflowHandler? _workflowHandler;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ValueRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            NotificationCache notificationCache,
            IAuthorizationService authorizationService, IWorkflowHandler? workflowHandler = null)
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
            _workflowHandler = workflowHandler;
        }

        /// <inheritdoc />
        private async Task<List<string>> GetUsersAsync(Guid? attributeId)
        {
            if (attributeId == null)
            {
                return[];
            }
            return await GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAttribute), attributeId,
                NotificationAction.Edit, GXDevicePolicies.View);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXValue> values, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXDevicePolicies.Add);
            values = values.ToArray();
            List<GXWorkflowEventContext> events = [];
            List<GXValue> recordedValues = [];
            var operationId = Guid.NewGuid();
            HashSet<string> users = [];
            List<GXAttribute> updates = [];
            GXUpdateArgs arg;
            DateTime now = DateTime.Now;
            GXSelectArgs args;
            List<GXObject>? objects = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (GXValue it in values)
            {
                if (it.Attribute == null)
                {
                    throw new ArgumentException("Invalid attribute.");
                }

                //Read time is set for profle generic objects.
                if (it.Read == null)
                {
                    it.Read = now;
                }
                else
                {
                    now = it.Read.Value.LocalDateTime;
                }
                //Update attribute read time and last value.
                GXAttribute value = new()
                {
                    Id = it.Attribute.Id,
                    Value = it.Value,
                    Read = it.Read
                };

                users.UnionWith(await GetUsersAsync(it.Attribute.Id));

                //Read attribute template index and name.
                args = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Index, s.Name });
                args.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template!);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Template = await connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, args)
                    ?? throw new ArgumentException("Attribute template was not found.");
                //Read attribute object ID. It's send at the notification.
                args = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template, s.Device });
                args.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.LogicalName, s.Name, s.ObjectType });
                args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Object = await connection.SingleOrDefaultAsync<GXObject>(transaction, args)
                    ?? throw new ArgumentException("Attribute object was not found.");
                objects.Add(value.Object);
                updates.Add(value);
            }
            //Update last read time for the device.
            List<GXDevice>? devices = null;
            if (objects.Count != 0)
            {
                var ids = objects.Select(s => s.Id).ToArray();
                if (ids.Length > 0)
                {
                    args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
                    args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
                    args.Where.And<GXObject>(w => ids.Contains(w.Id));
                    args.Distinct = true;
                    devices = await connection.SelectAsync<GXDevice>(transaction, args);
                    foreach (var it in devices.OrderBy(d => d.Id))
                    {
                        it.LastRead = now;
                        arg = GXUpdateArgs.Update(it, q => new { q.LastRead });
                        await connection.UpdateAsync(transaction, arg, cancellationToken);
                    }
                }
            }
            // Match object-list reads: lock parent devices before writing objects and attributes.
            foreach (var (value, it) in updates.Zip(values))
            {
                var previous = await connection.SingleOrDefaultAsync<GXAttribute>(transaction,
                    GXSelectArgs.Select<GXAttribute>(a => new { a.Id, a.Value }, a => a.Id == value.Id))
                    ?? throw new ArgumentException("Attribute was not found.");
                //Update read time for the object.
                var obj = value.Object!;
                obj.LastRead = it.Read;
                arg = GXUpdateArgs.Update(obj, q => new { q.LastRead });
                await connection.UpdateAsync(transaction, arg, cancellationToken);
                //Don't do anything If empty row is added for the profile generic.
                //Agent sends empty row if it's not able to read new rows from the meter.
                if (it.Value == null && value.Template!.Index == 2 && obj.Template != null &&
                    obj.Template.ObjectType == (int)Enums.ObjectType.ProfileGeneric)
                {
                    arg = GXUpdateArgs.Update(value, q => new { q.Read });
                    await connection.UpdateAsync(transaction, arg, cancellationToken);
                    continue;
                }
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(value, q => new { q.Value, q.Read }), cancellationToken);
                recordedValues.Add(it);
                events.AddRange(GXValueWorkflowEvents.Create(value.Id, obj.Device?.Id, previous.Value, it.Value, operationId));
            }
            // Insert only actual readings; an empty profile response updates its read time only.
            if (recordedValues.Count != 0)
            {
                connection.Insert(transaction, GXInsertArgs.InsertRange(recordedValues));
            }
            List<Guid> list = [.. recordedValues.Select(v => v.Id)];
            await transaction.CommitAsync(cancellationToken);
            foreach (var context in events) {
                _workflowHandler?.Publish(context, User);
            }
            _notificationCache.Add(users, new GXTarget(GXValuePolicies.Add));
            if (objects != null)
            {
                _notificationCache.Add(users, new GXTarget(GXObjectPolicies.Edit));
            }
            if (devices != null)
            {
                _notificationCache.Add(users, new GXTarget(GXDevicePolicies.Edit));
            }
            return [.. list];
        }

        /// <inheritdoc />
        public async Task ClearAttributeAsync(IEnumerable<GXAttribute> attributes, CancellationToken cancellationToken)
        {
            await ProfileHistoryRepository.Gate.WaitAsync(cancellationToken);
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXDevicePolicies.Edit);
                HashSet<string> users = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                foreach (var it in attributes)
                {
                    users.UnionWith(await GetUsersAsync(it.Id));
                    await GXProfileHistoryParameters.ClearAsync(connection, transaction, it.Id, cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it), cancellationToken);
                    //Reset last read time.
                    it.Read = null;
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.Read!), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                if (users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                }

            }
            finally { ProfileHistoryRepository.Gate.Release(); }
        }

        /// <inheritdoc />
        public async Task ClearDeviceAsync(IEnumerable<GXDevice> devices, CancellationToken cancellationToken)
        {
            await ProfileHistoryRepository.Gate.WaitAsync(cancellationToken);
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXDevicePolicies.Edit);
                HashSet<string> users = [];
                Guid[] guids = [.. devices.Select(s => s.Id)];
                //Reset last read time for the attribute.
                GXSelectArgs args = GXSelectArgs.Select<GXAttribute>(s => s.Id);
                args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object!, j => j.Id);
                args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
                args.Where.And<GXDevice>(w => GXSql.In(w.Id, guids));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                List<GXAttribute> attributes = (await connection.SelectAsync<GXAttribute>(transaction, args));
                foreach (var it in attributes)
                {
                    users.UnionWith(await GetUsersAsync(it.Id));
                    await GXProfileHistoryParameters.ClearAsync(connection, transaction, it.Id, cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it), cancellationToken);
                    it.Read = null;
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.Read!), cancellationToken);
                }
                //Reset last read time for the object.
                args = GXSelectArgs.Select<GXObject>(s => s.Id);
                args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
                args.Where.And<GXDevice>(w => GXSql.In(w.Id, guids));
                List<GXObject> objects = (await connection.SelectAsync<GXObject>(transaction, args));
                foreach (var it in objects)
                {
                    it.LastRead = null;
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.LastRead!), cancellationToken);
                }
                //Reset last read time for the device.
                foreach (var it in devices)
                {
                    it.LastRead = null;
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.LastRead!), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                if (users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                }

            }
            finally { ProfileHistoryRepository.Gate.Release(); }
        }

        /// <inheritdoc />
        public async Task ClearObjectAsync(IEnumerable<GXObject> objects, CancellationToken cancellationToken)
        {
            await ProfileHistoryRepository.Gate.WaitAsync(cancellationToken);
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXDevicePolicies.Edit);
                if (objects != null && objects.Any())
                {
                    HashSet<string> users = [];
                    Guid[] guids = [.. objects.Select(s => s.Id)];
                    GXSelectArgs args = GXSelectArgs.Select<GXAttribute>(s => s.Id);
                    args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object!, j => j.Id);
                    args.Where.And<GXObject>(w => GXSql.In(w.Id, guids));
                    await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                    List<GXAttribute> attributes = (await connection.SelectAsync<GXAttribute>(transaction, args));
                    foreach (var it in attributes)
                    {
                        users.UnionWith(await GetUsersAsync(it.Id));
                        await GXProfileHistoryParameters.ClearAsync(connection, transaction, it.Id, cancellationToken);
                        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it), cancellationToken);
                        //Reset last read time for the attribute.
                        it.Read = null;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.Read!), cancellationToken);
                    }
                    foreach (var it in objects)
                    {
                        //Reset last read time for the object.
                        it.LastRead = null;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, c => c.LastRead!), cancellationToken);
                    }
                    await transaction.CommitAsync(cancellationToken);
                    if (users.Count != 0)
                    {
                        _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                    }
                }

            }
            finally { ProfileHistoryRepository.Gate.Release(); }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> values, CancellationToken cancellationToken)
        {
            // Retain the existing bounded deadlock retry, releasing the profile gate between attempts.
            var ids = values?.ToArray() ?? [];
            try { await DeleteValuesAsync(ids, cancellationToken); return; }
            catch (GXDatabaseException error) when (error.ErrorType == DatabaseErrorType.Deadlock) { }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            while (true)
            {
                await Task.Delay(1000, linked.Token);
                try { await DeleteValuesAsync(ids, linked.Token); return; }
                catch (GXDatabaseException error) when (error.ErrorType == DatabaseErrorType.Deadlock && !linked.IsCancellationRequested)
                { }
            }
        }

        private async Task DeleteValuesAsync(IEnumerable<Guid> values, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXComponentViewPolicies.Add, GXDevicePolicies.Edit);
            var ids = values?.Distinct().ToArray() ?? [];
            if (ids.Length == 0) {
                return;
            }
            await ProfileHistoryRepository.Gate.WaitAsync(cancellationToken);
            try
            {
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                var args = GXSelectArgs.Select<GXAttribute>(a => a.Id);
                args.Joins.AddInnerJoin<GXValue, GXAttribute>(v => v.Attribute!, a => a.Id);
                args.Where.And<GXValue>(v => ids.Contains(v.Id));
                var attributes = await connection.SelectAsync<GXAttribute>(transaction, args, cancellationToken);
                HashSet<string> users = [];
                foreach (var attribute in attributes)
                {
                    users.UnionWith(await GetUsersAsync(attribute.Id));
                    await GXProfileHistoryParameters.ClearAsync(connection, transaction, attribute.Id, cancellationToken);
                }
                foreach (var id in ids)
                {
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXValue>(v => v.Id == id), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                if (users.Count != 0) {
                    _notificationCache.Add(users, new GXTarget(GXAttributePolicies.Edit));
                }
            }
            finally { ProfileHistoryRepository.Gate.Release(); }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXValue>> ListAsync(
            ListValues? request,
            ListValuesResponse? response,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXValuePolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXValue>();
            arg.Distinct = true;
            if (!User.IsInRole(GXRoles.Admin))
            {
                var attributes = GXQuery.GetAttributesByUser(a => GXSql.One, ServerHelpers.GetUserId(User));
                attributes.Where.And<GXObject>(o => o.Removed == null);
                arg.Where.And<GXValue>(v => GXSql.Exists<GXValue, GXAttribute>(v => v.Attribute!, a => a.Id, attributes));
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXValue>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included?.Any() == true)
            {
                arg.Where.And<GXValue>(w => request.Included.Contains(w.Id));
            }
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Attribute != null)
                {
                    if (request.Filter.Attribute.Object != null)
                    {
                        if (request.Filter.Attribute.Object.Device != null)
                        {
                            //Filter by device.
                            arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute!, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object!, j => j.Id);
                            arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
                        }
                        else
                        {
                            //Filter by object.
                            arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute!, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object!, j => j.Id);
                        }
                    }
                    else
                    {
                        //Filter by attribute.
                        arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute!, j => j.Id);
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXValue>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXValue>(o => o.Read!);
            }
            GXDateTimeFilters.Apply<GXValue>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXValue>(q => GXSql.Count(1));
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
            GXValue[] values = [.. (await connection.SelectAsync<GXValue>(arg))];
            if (response != null)
            {
                response.Values = values;
                if (response.Count == 0)
                {
                    response.Count = values.Length;
                }

            }
            return values;
        }

        /// <inheritdoc />
        public Task<GXValue> ReadAsync(Guid id, Expression<Func<GXValue, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            //TODO:
            throw new NotImplementedException();
        }
    }
}
