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
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class TaskRepository : ITaskRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAttributeRepository _attributeRepository;
        private readonly IObjectRepository _objectRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ITaskLateBindHandler _taskHandler;
        private readonly IScriptRepository _scriptRepository;
        private readonly IGXModuleService _moduleService;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ILogRepository _systemLogRepository;
        private readonly IAuthorizationService _authorizationService;

        public TaskRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IServiceProvider serviceProvider,
            IAttributeRepository attributeRepository,
            IObjectRepository objectRepository,
            IDeviceRepository deviceRepository,
            ITaskLateBindHandler taskHandler,
            IScriptRepository scriptRepository,
            IGXModuleService moduleService,
            IScheduleRepository scheduleRepository,
            NotificationCache notificationCache,
            IEnumTypeRepository enumTypeRepository,
            ILogRepository systemLogRepository)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userManager = userManager;
            _userRepository = userRepository;
            _systemLogRepository = systemLogRepository.ForSource("system");
            _serviceProvider = serviceProvider;
            _attributeRepository = attributeRepository;
            _objectRepository = objectRepository;
            _deviceRepository = deviceRepository;
            _taskHandler = taskHandler;
            _scriptRepository = scriptRepository;
            _moduleService = moduleService;
            _scheduleRepository = scheduleRepository;
            _notificationCache = notificationCache;
            _enumTypeRepository = enumTypeRepository;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXTask), id, action, GXTaskPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> tasks, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXTask> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXTask>(s => new { s.Id }, w => tasks.Contains(w.Id));
                exists = connection.Select<GXTask>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetTasksByUser(s => s.Id, userId, null, null, tasks);
                exists = connection.Select<GXTask>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", tasks));
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
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
                        await DeleteAsync(tasks, linkedCts.Token);
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
            foreach (GXTask it in exists)
            {
                var users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                _notificationCache.Add(users, new GXTarget(GXTaskPolicies.Delete, it.Id, it.Data));
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXTask>> ListAsync(
            ListTasks? request,
            ListTasksResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the tasks.
                arg = GXSelectArgs.SelectAll<GXTask>();
            }
            else
            {
                //Get all tasks that user has created.
                string userId = ServerHelpers.GetUserId(User);
                GXUser user = new() { Id = userId };
                arg = GXSelectArgs.SelectAll<GXTask>(w => w.Creator == user);
            }
            arg.Distinct = true;
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXTask>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXTask>(o => o.CreationTime!);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXTask>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXTask>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXTask>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTask>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    try
                    {
                        response.Count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
                        if (response.Count == 0)
                        {
                            //No need to retrieve data when none is available.
                            return [];
                        }
                    }
                    catch (TimeoutException)
                    {
                        //Return -1 if amount of the tasks is too large.
                        response.Count = -1;
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            var baseQuery = arg;
            GXTask[] detailTasks = [];
            if (request?.Select?.Any(s => s is "Device" or "Object" or "Attribute") == true)
                {
                detailTasks = (await connection.SelectAsync<GXTask>(baseQuery, cancellationToken)).ToArray();
            }
            var detailIds = detailTasks.Select(t => t.Id).ToArray();
            var tasksById = detailTasks.ToDictionary(t => t.Id);

            GXSelectArgs DetailsQuery()
            {
                var query = GXSelectArgs.SelectAll<GXTask>();
                query.Distinct = true;
                query.Joins.Append(baseQuery.Joins);
                query.Where.Append(baseQuery.Where);
                // Page task identities before joining their one-to-many detail rows.
                query.Where.And<GXTask>(t => detailIds.Contains(t.Id));
                query.Descending = baseQuery.Descending;
                query.OrderBy.Add<GXTask>(string.IsNullOrEmpty(request?.OrderBy) ? nameof(GXTask.CreationTime) : request.OrderBy);
                return query;
            }
            DateTime now = DateTime.Now;
            GXTask[] tasks = [];
            if (request != null)
            {
                if (request.Select != null && request.Select.Contains("Device"))
                {
                    //Select devices.
                    arg = DetailsQuery();
                    arg.Columns.Add<GXDevice>();
                    arg.Columns.Add<GXDeviceTemplate>();
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template, s.Object });
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Joins.AddInnerJoin<GXTask, GXDevice>(a => a.Device!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device!);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(b => b.Id, a => a.Object!);
                    arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Exclude<GXDevice>(e => e.Tasks!);
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.DeviceTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Device, e.Tasks, e.Attributes, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Tasks, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    foreach (var detail in await connection.SelectAsync<GXTask>(arg, cancellationToken))
                        {
                        tasksById[detail.Id].Device = detail.Device;
                    }
                }
                if (request.Select != null && request.Select.Contains("Attribute"))
                {
                    //Select attributes.
                    arg = DetailsQuery();
                    arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template, s.Object });
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Joins.AddInnerJoin<GXTask, GXAttribute>(a => a.Attribute!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXObject>(a => a.Object!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Exclude<GXTask>(e => e.Target!);
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Device, e.Tasks, e.Attributes, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Tasks, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    foreach (var detail in await connection.SelectAsync<GXTask>(arg, cancellationToken))
                        {
                        tasksById[detail.Id].Attribute = detail.Attribute;
                    }
                }
                if (request.Select != null && request.Select.Contains("Object"))
                {
                    //Select objects.
                    arg = DetailsQuery();
                    arg.Columns.Add<GXObject>();
                    arg.Joins.AddInnerJoin<GXTask, GXObject>(a => a.Object!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object!);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Add<GXAttribute>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Columns.Exclude<GXTask>(e => e.Target!);
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Tasks, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Object, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name, e.Weight });
                    foreach (var detail in await connection.SelectAsync<GXTask>(arg, cancellationToken))
                        {
                        tasksById[detail.Id].Object = detail.Object;
                    }
                }
                tasks = detailTasks;
            }
            if (request?.Select?.Any(s => s is "Device" or "Object" or "Attribute") != true)
            {
                tasks = [.. (await connection.SelectAsync<GXTask>(arg))];
            }
            if (response != null)
            {
                response.Tasks = tasks;
                if (response.Count == 0)
                {
                    response.Count = tasks.Length;
                }
            }
            return tasks;
        }

        /// <inheritdoc />
        public async Task<GXTask> ReadAsync(Guid id, Expression<Func<GXTask, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.View);

                GXSelectArgs arg = GXSelectArgs.SelectAll<GXTask>(where => where.Id == id);
                arg.Distinct = true;
                // Preserve the task's own foreign-key identities first. A wide
                // ID-only left-join projection can discard target references;
                // the explicit queries below hydrate each referenced graph.
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXTask? task = (await connection.SingleOrDefaultAsync<GXTask>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                // Preserve native GUID/string values in one scalar projection;
                // ID-only joined DTOs are not materialized by every provider.
                var referenceQuery = GXSelectArgs.Select<GXTask>(t => new
                {
                    t.ScriptMethod, t.DeviceGroup, t.Device, t.Object, t.Attribute,
                    t.TriggerSchedule, t.TriggerScript, t.OperatingAgent, t.TriggerUser, t.Creator
                }, t => t.Id == id);
                var references = (await connection.SelectAsync<object[]>(referenceQuery, cancellationToken)).Single();
                Guid? Reference(int index)
                {
                    var value = references[index];
                    if (value is null or DBNull) {
                        return null;
                    }
                    if (value is Guid guid) {
                        return guid;
                    }
                    if (value is byte[] bytes && bytes.Length == 16) {
                        return new Guid(bytes);
                    }
                    if (Guid.TryParse(Convert.ToString(value), out var parsed)) {
                        return parsed;
                    }
                    throw new InvalidDataException("Invalid task reference identifier.");
                }
                if (Reference(0) is Guid scriptMethodId) {
                    task.ScriptMethod = new() { Id = scriptMethodId };
                }
                if (Reference(1) is Guid deviceGroupId) {
                    task.DeviceGroup = new() { Id = deviceGroupId };
                }
                if (Reference(2) is Guid deviceId) {
                    task.Device = new() { Id = deviceId };
                }
                if (Reference(3) is Guid objectId) {
                    task.Object = new() { Id = objectId };
                }
                if (Reference(4) is Guid attributeId) {
                    task.Attribute = new() { Id = attributeId };
                }
                if (Reference(5) is Guid scheduleId) {
                    task.TriggerSchedule = new() { Id = scheduleId };
                }
                if (Reference(6) is Guid scriptId) {
                    task.TriggerScript = new() { Id = scriptId };
                }
                if (Reference(7) is Guid operatingAgentId) {
                    task.OperatingAgent = new() { Id = operatingAgentId };
                }
                var triggerUserId = references[8] is DBNull ? null : Convert.ToString(references[8]);
                if (!string.IsNullOrEmpty(triggerUserId)) {
                    task.TriggerUser = new() { Id = triggerUserId };
                }
                var creatorId = references[9] is DBNull ? null : Convert.ToString(references[9]);
                if (!string.IsNullOrEmpty(creatorId)) {
                    task.Creator = new() { Id = creatorId };
                }
                if (task.ScriptMethod != null)
                {
                    //Get modules with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id }, where => where.Id == task.ScriptMethod!.Id);
                    task.ScriptMethod = await connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                }
                if (task.DeviceGroup != null)
                {
                    //Get device group with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id }, where => where.Id == task.DeviceGroup!.Id);
                    task.DeviceGroup = await connection.SingleOrDefaultAsync<GXDeviceGroup>(arg);
                }
                if (task.Device != null)
                {
                    //Get device with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id }, where => where.Id == task.Device!.Id);
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttribute>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device!);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object!);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Exclude<GXObject>(e => e.Device!);
                    arg.Columns.Exclude<GXAttribute>(e => e.Object!);
                    arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate!);
                    task.Device = await connection.SingleOrDefaultAsync<GXDevice>(arg);
                    if (task.Device?.Objects != null)
                    {
                        // Hydrate the third level explicitly. The task reader
                        // needs every object's attributes, not just its template.
                        foreach (var deviceObject in task.Device.Objects)
                        {
                            var attributes = GXSelectArgs.SelectAll<GXAttribute>(a => a.Object == deviceObject);
                            attributes.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, t => t.Id);
                            attributes.Columns.Add<GXAttributeTemplate>();
                            attributes.Columns.Exclude<GXAttribute>(a => a.Object!);
                            attributes.Columns.Exclude<GXAttributeTemplate>(a => a.ObjectTemplate!);
                            deviceObject.Attributes = [.. await connection.SelectAsync<GXAttribute>(attributes, cancellationToken)];
                        }
                    }
                }
                if (task.Object != null)
                {
                    //Get object with own query. It's faster with some DBs.
                    arg = GXSelectArgs.SelectAll<GXObject>(where => where.Id == task.Object!.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object!);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Add<GXAttribute>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Object, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    task.Object = await connection.SingleOrDefaultAsync<GXObject>(arg);
                }
                if (task.Attribute != null)
                {
                    //Get attribute with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXAttribute>(s => new { s.Id, s.Template, s.Object }, where => where.Id == task.Attribute!.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXObject>(a => a.Object!, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template!, b => b.Id);
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Attributes, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    task.Attribute = await connection.SingleOrDefaultAsync<GXAttribute>(arg);
                }
                if (task.TriggerSchedule != null)
                {
                    //Get attribute with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXSchedule>(s => new { s.Id }, where => where.Id == task.TriggerSchedule!.Id);
                    task.TriggerSchedule = await connection.SingleOrDefaultAsync<GXSchedule>(arg);
                }
                if (task.TriggerUser != null)
                {
                    //Get attribute with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXUser>(s => new { s.Id }, where => where.Id == task.TriggerUser!.Id);
                    task.TriggerUser = await connection.SingleOrDefaultAsync<GXUser>(arg);
                }
                if (task.TriggerScript != null)
                {
                    //Get attribute with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXScript>(s => new { s.Id }, where => where.Id == task.TriggerScript!.Id);
                    task.TriggerScript = await connection.SingleOrDefaultAsync<GXScript>(arg);
                }
                if (task.OperatingAgent != null)
                {
                    //Get agent with own query. It's faster with some DBs.
                    arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id }, where => where.Id == task.OperatingAgent!.Id);
                    task.OperatingAgent = await connection.SingleOrDefaultAsync<GXAgent>(arg);
                }
                return task;
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
        public async Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken)
        {
            GXWorkflowService.ThrowIfExecutionCancelled();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Add);
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            DateTime now = DateTime.Now;
            GXSelectArgs arg;
            GXDevice? dev;
            GXObject? obj;
            List<GXTask> tasks3 = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                tasks = tasks.ToArray();
                foreach (var invocation in tasks)
                {
                    var workflowSource = GXWorkflowService.CurrentOperationId;
                    if (workflowSource != null)
                    {
                        if (invocation.WorkflowOperationId == null || invocation.WorkflowOperationId == workflowSource)
                        {
                            invocation.WorkflowOperationId = workflowSource;
                            invocation.WorkflowActorId = GXWorkflowService.CurrentActorId;
                        }
                        else
                        {
                            var parent = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                                GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == invocation.WorkflowOperationId), cancellationToken);
                            if (parent?.Code != "schedule.run" || parent.ParentId != workflowSource || invocation.TriggerSchedule == null ||
                                parent.TargetId != invocation.TriggerSchedule.Id.ToString())
                                {
                                throw new UnauthorizedAccessException("A workflow task cannot leave its current event chain.");
                            }
                            invocation.WorkflowActorId = parent.ActorId;
                        }
                    }
                    bool inheritedWorkflow = workflowSource != null;
                    if (!inheritedWorkflow && invocation.WorkflowActorId != null && invocation.WorkflowActorId != creator.Id)
                        {
                        throw new UnauthorizedAccessException("A scheduled invocation cannot claim another initiating user.");
                    }
                    if (invocation.WorkflowOperationId != null && !inheritedWorkflow)
                    {
                        if (invocation.WorkflowOperationId == Guid.Empty || invocation.TriggerSchedule == null)
                            {
                            throw new UnauthorizedAccessException("Scheduled operation correlation requires a visible schedule.");
                        }
                        await _authorizationService.EnsureAnyPolicyAsync(User, GXSchedulePolicies.View);
                        Guid scheduleId = invocation.TriggerSchedule.Id;
                        var visibleSchedule = User.IsInRole(GXRoles.Admin)
                            ? GXSelectArgs.Select<GXSchedule>(q => q.Id, q => q.Id == scheduleId && q.Removed == null)
                            : GXQuery.GetSchedulesByUser(q => q.Id, creator.Id, scheduleId);
                        if (await connection.SingleOrDefaultAsync<GXSchedule>(transaction, visibleSchedule, cancellationToken) == null)
                            {
                            throw new UnauthorizedAccessException("The scheduled invocation is not accessible.");
                        }
                    }
                }
                // A scheduled invocation must persist every expanded task before readers can
                // finish its first task; the general late-binding queue cannot provide that boundary.
                var expanded = new List<GXTask>();
                foreach (var scheduled in tasks)
                {
                    if (scheduled.WorkflowOperationId == null || (scheduled.Device == null && scheduled.DeviceGroup == null))
                    {
                        expanded.Add(scheduled);
                        continue;
                    }
                    IEnumerable<GXDevice> devices = scheduled.DeviceGroup != null
                        ? DeviceGroupRepository.GetDevicessByDeviceGroupId(connection, transaction, User, scheduled.DeviceGroup.Id)
                        : new[] { scheduled.Device! };
                    foreach (var device in devices)
                    {
                        if (scheduled.Object?.Attributes?.Count > 0)
                        {
                            foreach (var attribute in scheduled.Object.Attributes)
                            {
                                var item = ClientHelpers.Clone(scheduled);
                                item.Device = null;
                                item.DeviceGroup = null;
                                item.TargetDevice = device.Id;
                                item.Attribute = ClientHelpers.Clone(attribute);
                                item.Attribute.Object = scheduled.Object;
                                item.Object = null;
                                item.Index = attribute.Template?.Index;
                                expanded.Add(item);
                            }
                        }
                        else
                        {
                            var item = ClientHelpers.Clone(scheduled);
                            item.Device = null;
                            item.DeviceGroup = null;
                            item.TargetDevice = device.Id;
                            expanded.Add(item);
                        }
                    }
                }
                tasks = expanded;
                //Handle device groups and create a own task for each device.
                List<GXTask> removed = [];
                foreach (GXTask it in tasks)
                {
                    if (it.DeviceGroup != null)
                    {
                        //Get devices that belong to this device group.
                        var devices = DeviceGroupRepository.GetDevicessByDeviceGroupId(connection, transaction, User, it.DeviceGroup.Id);
                        //If user wants to read attributes from the selected device groups.
                        if (it.Object?.Attributes != null && it.Object.Attributes.Count != 0)
                        {
                            foreach (var dev2 in devices)
                            {
                                List<GXTask> tasks2 = [];
                                foreach (var att in it.Object.Attributes)
                                {
                                    GXTask t = ClientHelpers.Clone(it);
                                    t.TargetDevice = dev2.Id;
                                    t.DeviceGroup = null;
                                    t.Attribute = ClientHelpers.Clone(att);
                                    t.Index = att.Template!.Index;
                                    //Update attribute object.
                                    t.Attribute.Object = it.Object;
                                    t.Creator = it.Creator;
                                    tasks2.Add(t);
                                }
                                _taskHandler.AddRange(User, tasks2);
                            }
                        }
                        else
                        {
                            List<GXTask> tasks2 = [];
                            foreach (var dev2 in devices)
                            {
                                GXTask t = ClientHelpers.Clone(it);
                                t.DeviceGroup = null;
                                t.Device = dev2;
                                t.Creator = it.Creator;
                                tasks2.Add(t);
                            }
                            _taskHandler.AddRange(User, tasks2);
                        }
                        removed.Add(it);
                    }
                    if (it.Device != null)
                    {
                        //If user wants to read attributes from the selected device groups.
                        if (it.Object?.Attributes != null && it.Object.Attributes.Count != 0)
                        {
                            List<GXTask> tasks2 = [];
                            foreach (var att in it.Object.Attributes)
                            {
                                GXTask t = ClientHelpers.Clone(it);
                                t.TargetDevice = it.Device.Id;
                                t.DeviceGroup = null;
                                t.Attribute = ClientHelpers.Clone(att);
                                t.Index = att.Template?.Index ?? throw new ArgumentException("Attribute template is missing.");
                                //Update attribute object.
                                t.Attribute.Object = it.Object;
                                t.Creator = it.Creator;
                                tasks2.Add(t);
                            }
                            _taskHandler.AddRange(User, tasks2);
                        }
                        else
                        {
                            List<GXTask> tasks2 = [];
                            GXTask t = ClientHelpers.Clone(it);
                            t.Device = null;
                            t.TargetDevice = it.Device.Id;
                            t.Creator = it.Creator;
                            tasks2.Add(t);
                            _taskHandler.AddRange(User, tasks2);
                        }
                        removed.Add(it);
                    }
                    if (it.TargetDevice != null)
                    {
                        GXSelectArgs args = GXQuery.GetDevicesByUser(s => s.Id, creator.Id, it.TargetDevice.Value);
                        dev = (await connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                        if (dev == null)
                        {
                            throw new GXAmiForbiddenException(it.TargetDevice.ToString());
                        }
                        it.Device = dev;
                        it.Target = dev.Name;
                        if (it.TaskType == TaskType.Read && it.Object == null && it.Attribute == null &&
                            it.ScriptMethod == null && it.Module == null)
                        {
                            // Materialize missing objects before the device read can be claimed.
                            // Reuse the normal late-binding path for template authorization and attributes.
                            var templates = GXSelectArgs.SelectAll<GXObjectTemplate>(t => t.Removed == null);
                            templates.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(o => o.DeviceTemplate!, t => t.Id);
                            templates.Joins.AddInnerJoin<GXDeviceTemplate, GXDevice>(t => t.Id, d => d.Template!);
                            templates.Where.And<GXDevice>(d => d.Id == dev.Id && d.Removed == null);
                            templates.Where.And<GXDeviceTemplate>(t => t.Removed == null);
                            var existingQuery = GXSelectArgs.Select<GXObjectTemplate>(t => t.Id);
                            existingQuery.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(t => t.Id, o => o.Template!);
                            existingQuery.Joins.AddInnerJoin<GXObject, GXDevice>(o => o.Device!, d => d.Id);
                            existingQuery.Where.And<GXDevice>(d => d.Id == dev.Id);
                            var existing = await connection.SelectAsync<GXObjectTemplate>(transaction, existingQuery, cancellationToken);
                            // A removed instance is intentionally excluded from automatic recreation.
                            var instantiated = existing.Select(t => t.Id).ToHashSet();
                            foreach (var template in await connection.SelectAsync<GXObjectTemplate>(transaction, templates, cancellationToken))
                            {
                                if (!instantiated.Contains(template.Id))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    await ObjectRepository.CreateLateBindObject(connection, transaction, creator.Id, dev, template.Id);
                                    instantiated.Add(template.Id);
                                }
                            }
                        }
                    }
                }
                tasks3.AddRange(tasks);
                tasks3.RemoveAll(w => removed.Contains(w));
                Dictionary<(Guid DeviceId, Guid ObjectTemplateId), GXObject> objects = [];
                foreach (GXTask it in tasks3)
                {
                    bool dynamic = false;
                    it.Index ??= 0;
                    it.Order ??= 0;
                    if (it.TargetDevice == null && it.Object == null && it.Attribute == null &&
                        it.ScriptMethod == null && it.Module == null)
                    {
                        throw new ArgumentNullException(Properties.Resources.UnknownTaskTarget);
                    }
                    if (it.Object != null && it.Object.Id != Guid.Empty)
                    {
                        //Get device ID from the database.
                        arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Dynamic });
                        arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device!);
                        arg.Where.And<GXObject>(q => q.Id == it.Object.Id);
                        dev = (await connection.SingleOrDefaultAsync<GXDevice>(transaction, arg));
                        if (dev == null)
                        {
                            //Target device must set when late binding is used.
                            if (it.TargetDevice == null)
                            {
                                throw new GXAmiForbiddenException(it.Object.Id.ToString());
                            }
                            GXSelectArgs args = GXQuery.GetDevicesByUser(s => s.Id, creator.Id, it.TargetDevice.Value);
                            dev = (await connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                            if (dev == null)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            it.Object = await ObjectRepository.CreateLateBindObject(connection, transaction, creator.Id, dev, it.Object.Id);
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            it.Target = dev.Name + " " + it.Object.Template?.Name;
                        }
                        else
                        {
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            //Get object name from the database. Client can send all kind of names.
                            arg = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name!);
                            arg.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(j => j.Id, j => j.Template!);
                            arg.Where.And<GXObject>(w => w.Id == it.Object.Id);
                            it.Target = dev.Name + " " + (await connection.SingleOrDefaultAsync<string>(transaction, arg));
                        }
                    }
                    else if (it.Attribute != null)
                    {
                        //Get device ID from the database.
                        arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Dynamic });
                        arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device!);
                        arg.Joins.AddInnerJoin<GXObject, GXAttribute>(s => s.Id, o => o.Object!);
                        if (it.Attribute.Id != Guid.Empty)
                        {
                            //Search by ID.
                            arg.Where.And<GXAttribute>(q => q.Id == it.Attribute.Id);
                            dev = (await connection.SingleOrDefaultAsync<GXDevice>(transaction, arg));
                        }
                        else
                        {
                            //Search by logical name and index.
                            if (it.TargetDevice == null ||
                                it.Attribute?.Object?.Template == null ||
                                string.IsNullOrEmpty(it.Attribute.Object.Template.LogicalName) ||
                                it.Attribute.Template == null ||
                                it.Attribute.Template.Index == 0)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            arg.Columns.Add<GXObject>(s => new { s.Id, s.Device });
                            arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                            arg.Columns.Add<GXAttribute>();
                            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(s => s.Template!, o => o.Id);
                            arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice.Value);
                            arg.Where.And<GXObjectTemplate>(w => w.Removed == null && w.LogicalName == it.Attribute.Object.Template.LogicalName);
                            arg.Where.And<GXAttributeTemplate>(w => w.Removed == null && w.Index == it.Attribute.Template.Index);
                            obj = (await connection.SingleOrDefaultAsync<GXObject>(transaction, arg));
                            if (obj != null)
                            {
                                it.Object = obj;
                                it.Attribute = it.Object?.Attributes?.FirstOrDefault();
                                dev = it.Object?.Device;
                            }
                            else
                            {
                                dev = null;
                            }
                        }
                        if (dev == null)
                        {
                            //Target device must set when late binding is used.
                            if (it.TargetDevice == null)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            GXSelectArgs args = GXQuery.GetDevicesByUser(s => s.Id, creator.Id, it.TargetDevice.Value);
                            if (it.Attribute == null)
                            {
                                throw new ArgumentException("Task attribute is missing.");
                            }
                            if (it.Attribute.Id == Guid.Empty)
                            {
                                args.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                                args.Columns.Add<GXDeviceTemplate>(s => s.Id);
                            }
                            dev = (await connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                            if (dev == null)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            GXAttributeTemplate? at;
                            if (it.Attribute.Id == Guid.Empty)
                            {
                                if (dev.Template == null ||
                                    it.Attribute.Template == null ||
                                    it.Attribute.Object?.Template == null)
                                {
                                    throw new GXAmiForbiddenException();
                                }
                                //If attribute ID is unknown and logical name is used.
                                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Index, s.ObjectTemplate },
                                    w => w.Removed == null && w.Index == it.Attribute.Template.Index);
                                arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate!, j => j.Id);
                                arg.Where.And<GXObjectTemplate>(w => w.Removed == null && w.LogicalName == it.Attribute.Object.Template.LogicalName);
                                arg.Where.And<GXDeviceTemplate>(w => w.Removed == null && w.Id == dev.Template.Id);
                                at = (await connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                                it.Object = null;
                            }
                            else
                            {
                                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Index, s.ObjectTemplate },
                                   w => w.Removed == null && w.Id == it.Attribute.Id);
                                arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
                                at = (await connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                            }
                            if (at?.ObjectTemplate == null)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            //Check that previous attribute didn't create the object.
                            //This happens when multiple attribute tasks handle the same object.
                            var objectKey = (dev.Id, at.ObjectTemplate.Id);
                            if (!objects.TryGetValue(objectKey, out obj))
                            {
                                obj = await ObjectRepository.CreateLateBindObject(connection,
                                transaction, creator.Id, dev, at.ObjectTemplate.Id);
                                objects.Add(objectKey, obj);
                            }
                            //Search attribute by index.
                            it.Attribute = obj.Attributes?.Where(w => w.Template!.Index == at.Index).SingleOrDefault();
                            if (it.Attribute == null)
                            {
                                throw new GXAmiForbiddenException();
                            }
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            it.Target = dev.Name + " " + it.Attribute.Object?.Template?.Name + " " + it.Attribute.Template?.Name;
                        }
                        else
                        {
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            //Get object and attribute names from the database. Client can send all kind of names.
                            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Name, s.ObjectTemplate });
                            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name });
                            arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template!);
                            arg.Where.And<GXAttribute>(w => w.Id == it.Attribute!.Id);
                            var a = (await connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                            it.Target = dev.Name + " " + a?.ObjectTemplate?.Name + " " + a?.Name;
                        }
                        //Update the new attribute value.
                        if (it.TaskType == TaskType.Write)
                        {
                            if (string.IsNullOrEmpty(it.Attribute!.Value) && !string.IsNullOrEmpty(it.Data))
                            {
                                it.Attribute.Value = it.Data;
                                it.Attribute.LastWrite = now;
                            }
                            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(it.Attribute, u => u.Value!), cancellationToken);
                        }
                    }
                    else if (it.TargetDevice == null &&
                        it.ScriptMethod != null &&
                        it.Module != null)
                    {
                        throw new ArgumentNullException(Properties.Resources.UnknownTaskTarget);
                    }
                    //If script is triggered by user.
                    if (it.TriggerSchedule == null && it.TriggerScript == null)
                    {
                        it.TriggerUser = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                    }
                    else
                    {
                        throw new GXAmiForbiddenException();
                    }
                    if (it.TriggerUser == null)
                    {
                        if (it.TriggerSchedule != null)
                        {
                            it.TriggerUser = it.TriggerSchedule.Creator;
                        }
                        else if (it.TriggerScript != null)
                        {
                            it.TriggerUser = it.TriggerScript.Creator;
                        }
                        else if (it.TriggerModule != null)
                        {
                            it.TriggerUser = it.TriggerModule.Creator;
                        }
                        if (it.TriggerUser == null)
                        {
                            throw new GXAmiForbiddenException();
                        }
                    }
                    it.Creator = creator;
                    it.CreationTime = now;
                    if (dynamic)
                    {
                        //Get notified agents.
                        arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id });
                        arg.Joins.AddInnerJoin<GXAgent, GXAgentGroupAgent>(j => j.Id, j => j.AgentId);
                        arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
                        arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.AgentGroupId);
                        arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                        arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                        arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                        arg.Where.And<GXDevice>(w => w.Id == it.TargetDevice);
                        GXAgent? agent = await connection.SingleOrDefaultAsync<GXAgent>(transaction, arg);
                        if (agent != null)
                        {
                            it.TargetAgent = agent.Id;
                        }
                        //Get notified agents from gateway devices.
                        arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Agent });
                        arg.Columns.Add<GXAgent>(s => s.Id);
                        arg.Joins.AddInnerJoin<GXGateway, GXAgent>(j => j.Agent!, j => j.Id);
                        arg.Joins.AddInnerJoin<GXGateway, GXGatewayDevice>(j => j.Id, j => j.GatewayId);
                        arg.Joins.AddInnerJoin<GXGatewayDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                        arg.Where.And<GXGateway>(w => w.Removed == null);
                        arg.Where.And<GXAgent>(w => w.Removed == null);
                        arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice);
                        var gw = await connection.SingleOrDefaultAsync<GXGateway>(transaction, arg);
                        if (gw != null && gw.Agent != null)
                        {
                            it.TargetAgent = gw.Agent.Id;
                            it.TargetGateway = gw.Id;
                        }
                        else
                        {
                            //Get notified agents from gateway device groups.
                            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Agent });
                            arg.Columns.Add<GXAgent>(s => s.Id);
                            arg.Joins.AddInnerJoin<GXGateway, GXAgent>(j => j.Agent!, j => j.Id);
                            arg.Joins.AddInnerJoin<GXGateway, GXGatewayDeviceGroup>(j => j.Id, j => j.GatewayId);
                            arg.Joins.AddInnerJoin<GXGatewayDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                            arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                            arg.Where.And<GXGateway>(w => w.Removed == null);
                            arg.Where.And<GXAgent>(w => w.Removed == null);
                            arg.Where.And<GXDeviceGroup>(w => w.Removed == null);
                            arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice);
                            gw = await connection.SingleOrDefaultAsync<GXGateway>(transaction, arg);
                            if (gw != null && gw.Agent != null)
                            {
                                it.TargetAgent = gw.Agent.Id;
                                it.TargetGateway = gw.Id;
                            }
                        }
                    }
                    else if (it.ScriptMethod != null)
                    {
                        await _scriptRepository.RunAsync(it.ScriptMethod.Id, cancellationToken);
                    }
                    else if (it.Module != null)
                    {
                        GXScheduleModule? tmp = new()
                        {
                            ModuleId = it.Module.Id,
                            ScheduleId = it.TriggerSchedule?.Id ?? Guid.Empty,
                        };
                        tmp = await _scheduleRepository.GetModuleSettingsAsync(tmp, cancellationToken);
                        await _moduleService.ExecuteAsync(it.Module, tmp?.Settings, cancellationToken);
                    }
                    arg = GXQuery.GetUsersByDevice(s => s.Id!, null, it.TargetDevice);
                    var users = await connection.SelectAsync<string>(transaction, arg);
                    _notificationCache.Add(users, new GXTarget(GXTaskPolicies.Add));
                }
                var insert = GXInsertArgs.InsertRange(tasks3);
                insert.Exclude<GXTask>(e => new { e.Ready, e.Result, e.Updated, e.Start, e.OperatingAgent });
                insert.Exclude<GXAttribute>(e => new { e.Object, e.Template });
                insert.Exclude<GXObject>(e => new { e.Device, e.Template });
                await connection.InsertAsync(transaction, insert, cancellationToken);
                GXWorkflowService.ThrowIfExecutionCancelled();
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            return [.. tasks.Select(s => s.Id)];
        }

        /// <summary>
        /// Update task execution times to attribute, object and device.
        /// </summary>
        /// <param name="executedTask">Executed task</param>
        /// <param name="dbTask">Task from the database.</param>
        /// <param name="obj">Updated Object.</param>
        /// <param name="connection">The database connection.</param>
        private async Task UpdateObjectExecutionTimes(GXDbConnection connection, GXTask executedTask, GXTask dbTask, GXObject obj)
        {
            //Device name is mandatory.
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
            args.Where.And<GXObject>(w => w.Id == obj.Id);
            obj.Device = await connection.SingleOrDefaultAsync<GXDevice>(args);
            if (obj.Device == null)
            {
                throw new Exception("Device is unknown.");
            }
            //Save occurred exception or clear the old one.
            if (!string.IsNullOrEmpty(executedTask.Result))
            {
                obj.LastError = executedTask.Ready;
                obj.LastErrorMessage = executedTask.Result;
            }
            else
            {
                obj.LastError = null;
                obj.LastErrorMessage = null;
            }
            {
                Expression<Func<GXObject, object>> columns;
                switch (dbTask.TaskType)
                {
                    case TaskType.Read:
                        obj.LastRead = executedTask.Ready;
                        columns = q => new { q.LastRead, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Write:
                        obj.LastWrite = executedTask.Ready;
                        columns = q => new { q.LastWrite, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Action:
                        obj.LastAction = executedTask.Ready;
                        columns = q => new { q.LastAction, q.LastError, q.LastErrorMessage };
                        break;
                    default:
                        throw new Exception("Unknown task type.");
                }
                await _objectRepository.UpsertAsync([obj], columns);
            }
            {
                Expression<Func<GXDevice, object>> columns;
                switch (dbTask.TaskType)
                {
                    case TaskType.Read:
                        obj.Device.LastRead = executedTask.Ready;
                        columns = q => new { q.LastRead, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Write:
                        obj.Device.LastWrite = executedTask.Ready;
                        columns = q => new { q.LastWrite, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Action:
                        obj.Device.LastAction = executedTask.Ready;
                        columns = q => new { q.LastAction, q.LastError, q.LastErrorMessage };
                        break;
                    default:
                        throw new Exception("Unknown task type.");
                }
                await _deviceRepository.UpsertAsync([obj.Device], default, columns);
            }
        }

        private static string TaskOperationCode(GXTask task)
            => (task.TargetDevice != null ? "device." : "task.") + task.TaskType.GetValueOrDefault().ToString().ToLowerInvariant();

        private static string TaskOperationKey(GXTask task)
            => $"{TaskOperationCode(task)}:{task.Id:N}:{task.Start!.Value.ToUnixTimeMilliseconds()}";

        internal static async Task<GXWorkflowOperation> StartTaskOperationAsync(GXDbConnection connection, IDbTransaction transaction,
            GXTask task, string? actorId, CancellationToken token)
        {
            if (task.Start == null || task.OperatingAgent == null) {
                throw new InvalidOperationException("The task has not been claimed.");
            }
            // Preserve a distinct claim even if a task is restarted within the same clock millisecond.
            string key = TaskOperationKey(task);
            while (await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                GXSelectArgs.Select<GXWorkflowOperation>(o => o.Id, o => o.IdempotencyKey == key), token) != null)
            {
                task.Start = task.Start.Value.AddMilliseconds(1);
                key = TaskOperationKey(task);
            }
            return await WorkflowOperationRepository.StartRecordAsync(connection, transaction, new GXWorkflowOperation
            {
                Id = Guid.NewGuid(), IdempotencyKey = key, Code = TaskOperationCode(task),
                TargetId = task.TargetDevice?.ToString() ?? task.Id.ToString(), ParentId = task.WorkflowOperationId,
                ActorId = actorId, Data = JsonSerializer.Serialize(new { TaskId = task.Id, AgentId = task.OperatingAgent.Id, ClaimTime = task.Start.Value })
            }, token);
        }

        internal async Task<IReadOnlyList<GXTask>> CompleteOperationsAsync(IEnumerable<GXTask> reports, CancellationToken token)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Edit);
            ArgumentNullException.ThrowIfNull(reports);
            var tasks = reports.Select(t => new GXTask { Id = t.Id, Start = t.Start, Result = t.Result, Data = t.Data }).ToArray();
            var completed = new Dictionary<Guid, GXWorkflowOperation>();
            await WorkflowOperationRepository.Gate.WaitAsync(token);
            try
            {
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(token);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
                string userId = ServerHelpers.GetUserId(User);
                foreach (var report in tasks)
                {
                    var query = GXSelectArgs.Select<GXTask>(t => new { t.Id, t.Start, t.Ready, t.Result, t.Data,
                        t.TargetDevice, t.TaskType, t.WorkflowOperationId }, t => t.Id == report.Id);
                    query.Joins.AddInnerJoin<GXTask, GXAgent>(t => t.OperatingAgent!, a => a.Id);
                    query.Columns.Add<GXAgent>(a => a.Id);
                    query.Where.And<GXAgent>(a => a.Enabled == true && a.Removed == null);
                    var task = await connection.SingleOrDefaultAsync<GXTask>(transaction, query, token);
                    if (task?.Start == null || task.OperatingAgent == null || report.Start != task.Start)
                        {
                        throw new ArgumentException("The task claim is missing or stale.");
                    }
                    Guid agentId = task.OperatingAgent.Id;
                    var membership = GXRepositoryUsers.Query(typeof(GXAgent), agentId);
                    membership.Where.And<GXUser>(u => u.Id == userId);
                    if ((await connection.SelectAsync<GXUser>(transaction, membership, token)).Count == 0)
                        {
                        throw new GXAmiForbiddenException();
                    }
                    string key = TaskOperationKey(task);
                    var operation = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                        GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.IdempotencyKey == key), token)
                        ?? throw new ArgumentException("The task operation is unavailable.");
                    if (task.Ready == null)
                    {
                        task.Ready = DateTimeOffset.UtcNow;
                        task.Result = report.Result;
                        task.Data = report.Data;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(task, t => new { t.Ready, t.Result, t.Data }), token);
                        operation = await WorkflowOperationRepository.FinishRecordAsync(connection, transaction, operation.Id,
                            string.IsNullOrEmpty(task.Result) ? WorkflowOperationState.Completed : WorkflowOperationState.Failed,
                            string.IsNullOrEmpty(task.Result) ? null : "task-failed", task.Result?[..Math.Min(task.Result.Length, 2048)], token);
                    }
                    report.Ready = task.Ready;
                    report.Result = task.Result;
                    report.Data = task.Data;
                    completed[operation.Id] = operation;
                }
                await transaction.CommitAsync(token);
                foreach (var operation in completed.Values)
                    {
                    await WorkflowOperationRepository.PublishPendingAsync(connection, _serviceProvider.GetRequiredService<IWorkflowHandler>(), operation);
                }
            }
            finally { WorkflowOperationRepository.Gate.Release(); }
            return tasks;
        }
        /// <inheritdoc />
        public async Task DoneAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken)
        {
            tasks = await CompleteOperationsAsync(tasks, cancellationToken);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            //Update attribute exceptions.
            foreach (GXTask it in tasks)
            {
                GXTask task = await ReadAsync(it.Id, cancellationToken: cancellationToken);
                await GXScheduleTaskCompletion.CompleteAsync(_dbFactory,
                    _serviceProvider.GetRequiredService<IWorkflowHandler>(), task, cancellationToken);
                if (task.Attribute != null)
                {
                    if (!string.IsNullOrEmpty(it.Result))
                    {
                        task.Attribute.LastError = it.Ready;
                    }
                    else
                    {
                        task.Attribute.LastError = null;
                    }
                    task.Attribute.Exception = it.Result;
                    Expression<Func<GXAttribute, object>> columns;
                    switch (task.TaskType)
                    {
                        case TaskType.Read:
                            //Read time is not update to profile generic buffer.
                            if (task.Attribute.Object?.Template?.ObjectType == 7 &&
                                task.Attribute.Template?.Index == 2)
                            {
                                columns = q => new { q.LastError, q.Exception };
                            }
                            else
                            {
                                task.Attribute.Read = it.Ready;
                                columns = q => new { q.Read, q.LastError, q.Exception };
                            }
                            break;
                        case TaskType.Write:
                            task.Attribute.LastWrite = it.Ready;
                            columns = q => new { q.LastWrite, q.LastError, q.Exception };
                            break;
                        case TaskType.Action:
                            task.Attribute.LastAction = it.Ready;
                            columns = q => new { q.LastAction, q.LastError, q.Exception };
                            break;
                        default:
                            throw new Exception("Unknown task type.");
                    }
                    await _attributeRepository.UpsertAsync([task.Attribute], columns, cancellationToken);

                    //Update object exception and execution time.
                    GXSelectArgs args = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
                    args.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.LogicalName, s.Name });
                    args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                    args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
                    args.Where.And<GXAttribute>(w => w.Id == task.Attribute.Id);
                    args.Distinct = true;
                    GXObject? obj = await connection.SingleOrDefaultAsync<GXObject>(args);
                    if (obj != null)
                    {
                        await UpdateObjectExecutionTimes(connection, it, task, obj);
                    }
                }
                if (task.Object != null)
                {
                    if (task.Index != 0)
                    {
                        var att = task.Object.Attributes?.Where(w => w.Template?.Index == task.Index).SingleOrDefault();
                        if (att != null)
                        {
                            att.Object = task.Object;
                            if (!string.IsNullOrEmpty(it.Result))
                            {
                                att.LastError = it.Ready;
                            }
                            else
                            {
                                att.LastError = null;
                            }
                            att.Exception = it.Result;
                            Expression<Func<GXAttribute, object>> columns;
                            switch (task.TaskType)
                            {
                                case TaskType.Read:
                                    att.Read = it.Ready;
                                    columns = q => new { q.Read, q.LastError, q.Exception };
                                    break;
                                case TaskType.Write:
                                    att.LastWrite = it.Ready;
                                    columns = q => new { q.LastWrite, q.LastError, q.Exception };
                                    break;
                                case TaskType.Action:
                                    att.LastAction = it.Ready;
                                    columns = q => new { q.LastAction, q.LastError, q.Exception };
                                    break;
                                default:
                                    throw new Exception("Unknown task type.");
                            }

                            await _attributeRepository.UpsertAsync([att], columns, cancellationToken);
                        }
                    }
                    await UpdateObjectExecutionTimes(connection, it, task, task.Object);
                    var users = await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection);
                    _notificationCache.Add(users, new GXTarget(GXTaskPolicies.Edit, it.Id, it.Data));
                }
            }
        }

        /// <inheritdoc />
        public async Task RestartAsync(IEnumerable<GXTask> tasks, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Edit);
            ArgumentNullException.ThrowIfNull(tasks);
            var identifiers = tasks.Select(t => t.Id).Distinct().ToArray();
            await WorkflowOperationRepository.Gate.WaitAsync(cancellationToken);
            try
            {
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var operations = new List<GXWorkflowOperation>();
                var notifications = new List<(Guid Id, List<string> Users)>();
                string userId = ServerHelpers.GetUserId(User);
                foreach (Guid id in identifiers)
                {
                    var task = await connection.SingleOrDefaultAsync<GXTask>(transaction,
                        GXSelectArgs.SelectAll<GXTask>(t => t.Id == id), cancellationToken)
                        ?? throw new UnauthorizedAccessException("The task is unavailable.");
                    var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXTask), id,
                        transaction, token: cancellationToken);
                    if (!User.IsInRole(GXRoles.Admin) && !members.Contains(userId))
                        {
                        throw new UnauthorizedAccessException("The task is unavailable.");
                    }
                    if (task.Start != null)
                    {
                        string key = TaskOperationKey(task);
                        var operation = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                            GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.IdempotencyKey == key), cancellationToken);
                        if (operation?.State == WorkflowOperationState.Running)
                        {
                            operation = await WorkflowOperationRepository.FinishRecordAsync(connection, transaction,
                                operation.Id, WorkflowOperationState.Cancelled, null, null, cancellationToken);
                            operations.Add(operation);
                        }
                    }
                    task.OperatingAgent = null;
                    task.Start = task.Ready = null;
                    task.Result = null;
                    if (!Gurux.DLMS.AMI.Client.Shared.GXProfileReadIntent.IsSerializedIntent(task.Data))
                        {
                        task.Data = null;
                    }
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(task,
                        q => new { q.Start, q.Ready, q.Result, q.Data, q.OperatingAgent }), cancellationToken);
                    notifications.Add((id, await GetNotifiedUsers(id, NotificationAction.Edit, connection, transaction)));
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var notification in notifications)
                    {
                    _notificationCache.Add(notification.Users, new GXTarget(GXTaskPolicies.Edit, notification.Id, null));
                }
                foreach (var operation in operations)
                    {
                    await WorkflowOperationRepository.PublishPendingAsync(connection,
                        _serviceProvider.GetRequiredService<IWorkflowHandler>(), operation);
                }
            }
            finally { WorkflowOperationRepository.Gate.Release(); }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXTask>> GetNextAsync(string? connectionInfo, Guid agentId, Guid? DeviceId,
            Guid? gatewayId, bool listener, CancellationToken cancellationToken)
        {
            await WorkflowOperationRepository.Gate.WaitAsync(cancellationToken);
            try { return await GetNextCoreAsync(connectionInfo, agentId, DeviceId, gatewayId, listener, cancellationToken); }
            finally { WorkflowOperationRepository.Gate.Release(); }
        }
        private async Task<IEnumerable<GXTask>> GetNextCoreAsync(
            string? connectionInfo,
            Guid agentId,
            Guid? DeviceId,
            Guid? gatewayId,
            bool listener, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Edit);
            //Check that agent is active and not removed.
            GXSelectArgs arg = GXSelectArgs.Select<GXAgent>(c => c.Id, q => q.Id == agentId && q.Enabled == true && q.Removed == null);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            GXAgent? agent;
            GXTask[] tasks = [];
            var operations = new List<GXWorkflowOperation>();
            try
            {
                agent = await connection.SingleOrDefaultAsync<GXAgent>(transaction, arg, cancellationToken);
                if (agent == null)
                {
                    throw new UnauthorizedAccessException();
                }
                string userId = ServerHelpers.GetUserId(User);
                var membership = GXRepositoryUsers.Query(typeof(GXAgent), agentId);
                membership.Where.And<GXUser>(u => u.Id == userId);
                if ((await connection.SelectAsync<GXUser>(transaction, membership, cancellationToken)).Count == 0)
                    {
                    throw new UnauthorizedAccessException();
                }
                arg = GXQuery.GetNextTasks(c => c.Id, userId, agentId, DeviceId, gatewayId, false, listener);
                var pending = await connection.SelectAsync<GXTask>(transaction, arg, cancellationToken);
                if (pending.Count == 0)
                {
                    // An explicit device still scopes the fallback to that meter.
                    // A gateway poll must never claim a meter outside its gateway mappings.
                    if (gatewayId == null || gatewayId == Guid.Empty)
                    {
                        arg = GXQuery.GetNextTasks(c => c.Id, userId, agentId, DeviceId, gatewayId, true, listener);
                        pending = await connection.SelectAsync<GXTask>(transaction, arg, cancellationToken);
                    }
                    if (pending.Count == 0)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return tasks;
                    }
                }
                // Read every task before claiming the batch. The serializable transaction
                // prevents a competing agent from claiming the same meter concurrently.
                var batch = new List<GXTask>(pending.Count);
                foreach (var item in pending)
                {
                    batch.Add(await ReadAsync(item.Id, null, cancellationToken));
                }
                foreach (var task in batch)
                {
                    task.Start = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    task.OperatingAgent = agent;
                    operations.Add(await StartTaskOperationAsync(connection, transaction, task,
                        task.WorkflowOperationId != null ? task.WorkflowActorId : task.TriggerUser?.Id ?? task.Creator?.Id ?? userId, cancellationToken));
                    GXUpdateArgs args = GXUpdateArgs.Update(task, q => new { q.Start, q.OperatingAgent });
                    await connection.UpdateAsync(transaction, args, cancellationToken);
                    // Trim details that the executing agent does not need.
                    task.Target = null;
                    task.CreationTime = null;
                    task.TriggerUser = null;
                    task.Object?.CreationTime = null;
                }
                tasks = [.. batch];
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            foreach (var operation in operations)
                {
                await WorkflowOperationRepository.PublishPendingAsync(connection, _serviceProvider.GetRequiredService<IWorkflowHandler>(), operation);
            }
            //Update agent detected time.
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateStatusAsync(agent.Id, connectionInfo, AgentStatus.Detected, null, cancellationToken);
            }
            if (DeviceId != null && DeviceId != Guid.Empty)
            {
                Guid id = DeviceId.Value;
                //Update device detected time.
                using IServiceScope scope = _serviceProvider.CreateScope();
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                await repository.UpdateStatusAsync(id, DeviceStatus.Connected, cancellationToken);
            }
            else if (gatewayId != null && gatewayId != Guid.Empty)
            {
                Guid id = gatewayId.Value;
                //Update gateway detected time.
                using IServiceScope scope = _serviceProvider.CreateScope();
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                await repository.UpdateStatusAsync(id, GatewayStatus.Detected, cancellationToken);
            }
            return tasks;
        }

        /// <inheritdoc />
        public async Task ClearAsync(CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTaskPolicies.Clear);
            bool admin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddLeftJoin<GXUser, GXTask>(y => y.Id!, x => x.TriggerUser!);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg.Where.And<GXUser>(w => w.Id == id);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.Task, cancellationToken);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            List<string> list;
            try
            {
                var users = await connection.SelectAsync<GXUser>(transaction, arg)!;
                list = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                //Notification users if their tasks are cleared.
                list.AddDistinct(users.Select(s => s.Id));
                if (admin)
                {
                    //Admin clears all tasks.
                    connection.Truncate<GXTask>(transaction);
                }
                else if (users.Count != 0)
                {
                    GXUser tmp = users[0];
                    GXDeleteArgs args = GXDeleteArgs.Delete<GXTask>(w => w.TriggerUser == tmp);
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                _notificationCache.Add(list, new GXTarget(GXTaskPolicies.Clear));
            }
            catch (Exception)
            {
                await transaction.CommitAsync(cancellationToken);
                throw;
            }
            if (await _authorizationService.CanAccessPolicyAsync(User, GXSystemLogPolicies.Add))
            {
                //Add clean to the clear to log.
                await _systemLogRepository.AddAsync(TargetType.Task, [new GXLog(TraceLevel.Info) { Message = Properties.Resources.Clear }], cancellationToken);
            }
        }
    }
}






