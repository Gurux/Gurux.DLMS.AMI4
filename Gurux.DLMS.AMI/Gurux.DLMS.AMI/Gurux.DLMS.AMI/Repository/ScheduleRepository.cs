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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Scheduler;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;

using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class ScheduleRepository : IScheduleRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IScheduleGroupRepository _scheduleGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>Gets or sets the user1.</summary>
        public ClaimsPrincipal User1 => User;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IScheduleGroupRepository scheduleGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _scheduleGroupRepository = scheduleGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXSchedule), id, action, GXSchedulePolicies.View, connection, transaction, notifications: _notificationCache);

        /// <summary>
        /// Returns device groups that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of device groups.</returns>
        private static async Task<List<GXDeviceGroup>> GetDeviceGroupsByScheduleId(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXScheduleToDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Where.And<GXScheduleToDeviceGroup>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXDeviceGroup>(transaction, arg));
        }

        /// <summary>
        /// Returns script methods that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of script methods.</returns>
        private static async Task<List<GXScriptMethod>> GetScriptsByScheduleId(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptMethod>();
            arg.Joins.AddInnerJoin<GXScriptMethod, GXScheduleScript>(a => a.Id, b => b.ScriptMethodId);
            arg.Where.And<GXScheduleScript>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXScriptMethod>(transaction, arg));
        }

        /// <summary>
        /// Returns modules that are bind for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of modules.</returns>
        private static async Task<List<GXModule>> GetModulesByScheduleId(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>();
            arg.Joins.AddInnerJoin<GXModule, GXScheduleModule>(a => a.Id, b => b.ModuleId);
            arg.Where.And<GXScheduleModule>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXModule>(transaction, arg));
        }

        /// <summary>
        /// Returns devices that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of devices.</returns>
        private static async Task<List<GXDevice>> GetDevicesByScheduleId(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDevice, GXScheduleToDevice>(a => a.Id, b => b.DeviceId);
            arg.Where.And<GXScheduleToDevice>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return await connection.SelectAsync<GXDevice>(transaction, arg);
        }

        /// <summary>
        /// Returns device object templates that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of object templates.</returns>
        private static async Task<List<GXObjectTemplate>> GetDeviceObjectTemplatesByScheduleId(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXObjectTemplate, GXScheduleToDeviceObjectTemplate>(a => a.Id, b => b.ObjectTemplateId);
            arg.Where.And<GXScheduleToDeviceObjectTemplate>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXObjectTemplate>(transaction, arg));
        }

        /// <summary>
        /// Returns device group object templates that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of object templates.</returns>
        private static async Task<List<GXObjectTemplate>> GetDeviceGroupObjectTemplatesByScheduleId(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXObjectTemplate, GXScheduleToDeviceGroupObjectTemplate>(a => a.Id, b => b.ObjectTemplateId);
            arg.Where.And<GXScheduleToDeviceGroupObjectTemplate>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXObjectTemplate>(transaction, arg));
        }

        /// <summary>
        /// Returns device attribute templates that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of attribute templates.</returns>
        private static async Task<List<GXAttributeTemplate>> GetDeviceAttributeTemplatesByScheduleId(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXScheduleToDeviceAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
            arg.Where.And<GXScheduleToDeviceAttributeTemplate>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXAttributeTemplate>(transaction, arg));
        }

        /// <summary>
        /// Returns device group attribute templates that are added for the schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of attribute templates.</returns>
        private static async Task<List<GXAttributeTemplate>> GetDeviceGroupAttributeTemplatesByScheduleId(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXScheduleToDeviceGroupAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
            arg.Where.And<GXScheduleToDeviceGroupAttributeTemplate>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await connection.SelectAsync<GXAttributeTemplate>(transaction, arg));
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(
            Guid? scheduleId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersBySchedule(s => s.Id!,
                ServerHelpers.GetUserId(User1), scheduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            return [.. users.Select(s => s.Id!)];
        }
        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(
            IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersBySchedules(s => s.Id!, ServerHelpers.GetUserId(User1), Ids);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            return [.. users.Select(s => s.Id!)];
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            IEnumerable<Guid> schedulers,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            schedulers = schedulers.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXSchedule, Guid>(_dbFactory, User, _serviceProvider, schedulers,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXSchedule> exists;
            if (User1.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXSchedule>(s => new { s.Id, s.Name }, w => schedulers.Contains(w.Id));
                exists = connection.Select<GXSchedule>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User1);
                var arg = GXQuery.GetSchedulesByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, schedulers);
                exists = connection.Select<GXSchedule>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", schedulers));
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var it in exists)
                {
                    if (!schedulers.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(string.Join(", ", schedulers));
                    }
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "schedule", [it.Id], cancellationToken);
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        it.Removed = now;
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    //Notify users
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction),
                        new GXTarget(GXSchedulePolicies.Delete, it.Id, it.Name));
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
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(schedulers, delete, notify, linkedCts.Token);
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

        /// <inheritdoc/>

        public async Task<IEnumerable<GXSchedule>> ListAsync(
            ListSchedules? request,
            ListSchedulesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User1.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the schedules.
                arg = GXSelectArgs.SelectAll<GXSchedule>();
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXSchedule>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXSchedule>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User1);
                arg = GXQuery.GetSchedulesByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
                arg.Columns.Exclude<GXUser>(e => e.Schedules!);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXSchedule>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSchedule>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXSchedule>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSchedule>(q => q.CreationTime!);
                arg.Descending = true;
            }

            if (request?.Select != null && request.Select.Contains("User"))
            {
                //User info is also read.
                arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
                arg.Joins.AddInnerJoin<GXSchedule, GXUser>(j => j.Creator!, j => j.Id!);
            }
            GXSchedule[] schedules = [.. (await connection.SelectAsync<GXSchedule>(arg))];
            if (response != null)
            {
                response.Schedules = schedules;
                if (response.Count == 0)
                {
                    response.Count = schedules.Length;
                }
            }
            return schedules;
        }

        /// <inheritdoc/>
        public async Task<GXSchedule> ReadAsync(Guid id, Expression<Func<GXSchedule, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.View);
                bool isAdmin = User1.IsInRole(GXRoles.Admin);
                GXSelectArgs arg;
                if (isAdmin)
                {
                    //Admin can see all the schedules.
                    arg = GXSelectArgs.SelectAll<GXSchedule>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User1);
                    arg = GXQuery.GetSchedulesByUser(s => "*", userId, id);
                }
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXSchedule? schedule = await connection.SingleOrDefaultAsync<GXSchedule>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get schedule groups with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXScheduleGroup>(s => "*", q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleGroupId);
                arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(y => y.ScheduleId, x => x.Id);
                arg.Where.And<GXSchedule>(w => w.Id == id && w.Removed == null);
                schedule.ScheduleGroups = connection.Select<GXScheduleGroup>(arg);
                //Get devices with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Template }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
                arg.Joins.AddInnerJoin<GXDevice, GXScheduleToDevice>(s => s.Id, o => o.DeviceId);
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(s => s.Template!, o => o.Id);
                arg.Where.And<GXScheduleToDevice>(w => w.ScheduleId == id && w.Removed == null);
                schedule.Devices = connection.Select<GXDevice>(arg);
                //Get devices groups with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceGroup, GXScheduleToDeviceGroup>(s => s.Id, o => o.DeviceGroupId);
                arg.Where.And<GXScheduleToDeviceGroup>(w => w.ScheduleId == id && w.Removed == null);
                schedule.DeviceGroups = connection.Select<GXDeviceGroup>(arg);
                //Get script methods with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScriptMethod, GXScheduleScript>(s => s.Id, o => o.ScriptMethodId);
                arg.Where.And<GXScheduleScript>(w => w.ScheduleId == id && w.Removed == null);
                schedule.ScriptMethods = connection.Select<GXScriptMethod>(arg);
                //Get modules with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXModule>(s => new { s.Id, s.Name, s.Enabled });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXModule, GXScheduleModule>(s => s.Id, o => o.ModuleId);
                arg.Where.And<GXScheduleModule>(w => w.ScheduleId == id && w.Removed == null);
                schedule.Modules = connection.Select<GXModule>(arg);

                //Get device object templates with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXScheduleToDeviceObjectTemplate>(s => s.Id, o => o.ObjectTemplateId);
                arg.Where.And<GXScheduleToDeviceObjectTemplate>(w => w.ScheduleId == id && w.Removed == null);
                schedule.DeviceObjectTemplates = connection.Select<GXObjectTemplate>(arg);
                //Get device attribute templates with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index }, q => q.Removed == null);
                arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName });
                arg.Columns.Exclude<GXObjectTemplate>(s => s.Attributes!);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAttributeTemplate, GXScheduleToDeviceAttributeTemplate>(s => s.Id, o => o.AttributeTemplateId);
                arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(s => s.ObjectTemplate!, o => o.Id);
                arg.Where.And<GXScheduleToDeviceAttributeTemplate>(w => w.ScheduleId == id && w.Removed == null);
                schedule.DeviceAttributeTemplates = connection.Select<GXAttributeTemplate>(arg);

                //Get device group object templates with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXScheduleToDeviceGroupObjectTemplate>(s => s.Id, o => o.ObjectTemplateId);
                arg.Where.And<GXScheduleToDeviceGroupObjectTemplate>(w => w.ScheduleId == id && w.Removed == null);
                schedule.DeviceGroupObjectTemplates = connection.Select<GXObjectTemplate>(arg);
                //Get device group attribute templates with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index }, q => q.Removed == null);
                arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName });
                arg.Columns.Exclude<GXObjectTemplate>(s => s.Attributes!);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXAttributeTemplate, GXScheduleToDeviceGroupAttributeTemplate>(s => s.Id, o => o.AttributeTemplateId);
                arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(s => s.ObjectTemplate!, o => o.Id);
                arg.Where.And<GXScheduleToDeviceGroupAttributeTemplate>(w => w.ScheduleId == id && w.Removed == null);
                schedule.DeviceGroupAttributeTemplates = connection.Select<GXAttributeTemplate>(arg);

                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXSchedule, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXSchedule>(w => w.Id == id);
                schedule.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return schedule;

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

        /// <inheritdoc/>
        public async Task UpdateExecutionTimeAsync(GXSchedule schedule, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.Edit);
            GXUpdateArgs args = GXUpdateArgs.Update(schedule, c => c.ExecutionTime!);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await connection.UpdateAsync(args, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXSchedule> schedulers,
            Expression<Func<GXSchedule, object>>? columns, CancellationToken cancellationToken = default)
        {
            schedulers = schedulers.ToArray();
            return await GXHistoryMutation.RunAsync<GXSchedule, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, schedulers,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User1, GXSchedulePolicies.Add, GXSchedulePolicies.Edit);
            DateTime now = DateTime.Now;
            string userId = ServerHelpers.GetUserId(User1);
            List<Guid> list = [];
            Dictionary<GXSchedule, List<string>> updates = [];
            List<GXScheduleGroup>? defaultGroups = null;
            var newGroups = schedulers.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = schedulers.Where(w => w.Id != Guid.Empty).ToList();
            //Map schedule groups to schedule.
            Dictionary<GXSchedule, List<GXScheduleGroup>> scheduleGroups = [];
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IScheduleGroupRepository scheduleGroupRepository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                foreach (var it in updatedGroups)
                {
                    scheduleGroups.Add(it, await scheduleGroupRepository.GetJoinedScheduleGroups(it.Id));
                }
            }
            //Get notified users.
            if (newGroups.Count != 0)
            {
                var first = newGroups.First();
                var users = await GetNotifiedUsers(first.Id, NotificationAction.Edit);
                foreach (var it in newGroups)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedGroups)
            {
                updates[it] = await GetNotifiedUsers(it.Id, NotificationAction.Edit);
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (var triggerId in schedulers.SelectMany(s => s.Triggers ?? []).Select(t => t.Id).Distinct())
            {
                await _authorizationService.EnsureAnyPolicyAsync(User1, GXTriggerPolicies.View);
                var trigger = await connection.SingleOrDefaultAsync<GXTrigger>(
                    GXSelectArgs.Select<GXTrigger>(t => t.Id, t => t.Id == triggerId && t.Removed == null), cancellationToken);
                if (trigger == null) {
                    throw new GXAmiForbiddenException(triggerId.ToString());
                }
                await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, User1,
                    typeof(GXTrigger), triggerId, cancellationToken);
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            foreach (GXSchedule schedule in schedulers)
            {
                //Verify the name.
                if (string.IsNullOrEmpty(schedule.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Verify start date time.
                try
                {
                    if (string.IsNullOrEmpty(schedule.Start))
                    {
                        throw new ArgumentNullException(Properties.Resources.InvalidStartTime);
                    }
                    if (schedule.ScheduleGroups == null || schedule.ScheduleGroups.Count == 0)
                    {
                        if (defaultGroups == null)
                        {
                            ListScheduleGroups request = new() { Filter = new GXScheduleGroup() { Default = true } };
                            defaultGroups = [.. await _scheduleGroupRepository.ListAsync(request, null, cancellationToken)];
                        }
                        schedule.ScheduleGroups = defaultGroups;
                        if (schedule.ScheduleGroups.Count == 0)
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                    }
                    //Schedule times are saved in InvariantCulture.
                    schedule.Start = new Scheduling.GXDateTime(schedule.Start, CultureInfo.InvariantCulture).ToFormatString(CultureInfo.InvariantCulture);
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
                            return await UpsertAsync(schedulers, columns, linkedCts.Token);
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
                    throw new ArgumentException(string.Format("Invalid start time: '{0}'", schedule.Start));
                }
            }
            try
            {
                if (newGroups.Count != 0)
                {
                    foreach (var it in newGroups)
                    {
                        it.CreationTime = now;
                        it.Creator = new GXUser() { Id = ServerHelpers.GetUserId(User1) };
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXSchedule>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        e.ScheduleGroups,
                        e.Devices,
                        e.DeviceGroups,
                        e.ScriptMethods,
                        e.Modules,
                        e.Triggers,
                        e.DeviceAttributeTemplates,
                        e.DeviceObjectTemplates,
                        e.DeviceGroupAttributeTemplates,
                        e.DeviceGroupObjectTemplates,
                    });
                    await connection.InsertAsync(transaction, args, cancellationToken);
                    foreach (var it in newGroups)
                    {
                        list.Add(it.Id);
                        scheduleGroups.Add(it, []);
                    }
                }
                foreach (var schedule in schedulers)
                {
                    if (!newGroups.Contains(schedule))
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXSchedule>(q => q.ConcurrencyStamp!, where => where.Id == schedule.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != schedule.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        schedule.Updated = now;
                        schedule.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(schedule, columns);
                        args.Exclude<GXSchedule>(q => new
                        {
                            q.CreationTime,
                            q.ExecutionTime,
                            q.ScheduleGroups,
                            q.Devices,
                            q.DeviceGroups,
                            q.ScriptMethods,
                            q.Modules,
                            q.Triggers,
                            q.DeviceAttributeTemplates,
                            q.DeviceObjectTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.DeviceGroupObjectTemplates,
                        });
                        if (!User1.IsInRole(GXRoles.Admin) ||
                            schedule.Creator == null ||
                            string.IsNullOrEmpty(schedule.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXSchedule>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);
                    }
                    //Map schedule to schedule groups.
                    {
                        var comparer = new UniqueComparer<GXScheduleGroup, Guid>();
                        List<GXScheduleGroup> removed = [.. scheduleGroups[schedule].Except(schedule.ScheduleGroups ?? [], comparer)];
                        List<GXScheduleGroup> added = [.. (schedule.ScheduleGroups ?? []).Except(scheduleGroups[schedule], comparer)];
                        if (removed.Count != 0)
                        {
                            RemoveSchedulesFromScheduleGroup(connection, transaction, schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            AddScheduleToScheduleGroups(connection, transaction, schedule.Id, added);
                        }
                    }
                    // A null trigger collection leaves existing links untouched;
                    // an explicit empty collection removes this schedule's links.
                    if (schedule.Triggers != null)
                    {
                        var existing = await connection.SelectAsync<GXScheduleTrigger>(transaction,
                            GXSelectArgs.SelectAll<GXScheduleTrigger>(m => m.ScheduleId == schedule.Id), cancellationToken);
                        var selected = schedule.Triggers.Select(t => t.Id).Distinct().ToArray();
                        var removed = existing.Where(m => !selected.Contains(m.TriggerId)).Select(m => m.TriggerId).ToArray();
                        if (removed.Length != 0)
                        {
                            await connection.DeleteAsync(transaction,
                                GXDeleteArgs.Delete<GXScheduleTrigger>(m => m.ScheduleId == schedule.Id && removed.Contains(m.TriggerId)), cancellationToken);
                        }
                        foreach (var triggerId in selected)
                        {
                            var mapping = existing.SingleOrDefault(m => m.TriggerId == triggerId);
                            if (mapping == null)
                            {
                                await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXScheduleTrigger
                                {
                                    ScheduleId = schedule.Id, TriggerId = triggerId, CreationTime = DateTimeOffset.UtcNow
                                }), cancellationToken);
                            }
                            else if (mapping.Removed != null)
                            {
                                mapping.Removed = null;
                                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(mapping, m => m.Removed!,
                                    m => m.ScheduleId == schedule.Id && m.TriggerId == triggerId), cancellationToken);
                            }
                        }
                    }
                    //Map devices to schedule.
                    {
                        List<GXDevice> devices = await GetDevicesByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXDevice, Guid>();
                        List<GXDevice> removed, added;
                        if (schedule.Devices == null)
                        {
                            removed = devices;
                            added = [];
                        }
                        else
                        {
                            removed = [.. devices.Except(schedule.Devices, comparer)];
                            added = [.. schedule.Devices.Except(devices, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDevicesFromSchedule(connection, transaction, schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            AddDevicesToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }
                    //Map device groups to schedule.
                    {
                        List<GXDeviceGroup> devicegroups = await GetDeviceGroupsByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        List<GXDeviceGroup> removed, added;
                        if (schedule.DeviceGroups == null)
                        {
                            removed = devicegroups;
                            added = [];
                        }
                        else
                        {
                            removed = [.. devicegroups.Except(schedule.DeviceGroups, comparer)];
                            added = [.. schedule.DeviceGroups.Except(devicegroups, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDeviceGroupsFromSchedule(connection, transaction, schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            AddDeviceGroupsToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }
                    //Map scripts to schedule.
                    {
                        List<GXScriptMethod> scripts = await GetScriptsByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXScriptMethod, Guid>();
                        List<GXScriptMethod> removed, added;
                        if (schedule.ScriptMethods == null)
                        {
                            removed = scripts;
                            added = [];
                        }
                        else
                        {
                            removed = [.. scripts.Except(schedule.ScriptMethods, comparer)];
                            added = [.. schedule.ScriptMethods.Except(scripts, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveScriptMethodsFromSchedule(connection, transaction, schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            AddScriptMethodsToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }
                    //Map modules to schedule.
                    {
                        List<GXModule> modules = await GetModulesByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXModule, string>();
                        List<GXModule> removed, added;
                        if (schedule.Modules == null)
                        {
                            removed = modules;
                            added = [];
                        }
                        else
                        {
                            removed = [.. modules.Except(schedule.Modules, comparer)];
                            added = [.. schedule.Modules.Except(modules, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveModulesFromSchedule(connection, transaction, schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            AddModulesToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }

                    //Map device attribute templates to schedule.
                    {
                        List<GXAttributeTemplate> attributes = await GetDeviceAttributeTemplatesByScheduleId(connection, transaction,
                            schedule.Id);
                        var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                        List<GXAttributeTemplate> removed, added;
                        if (schedule.DeviceAttributeTemplates == null)
                        {
                            removed = attributes;
                            added = [];
                        }
                        else
                        {
                            removed = [.. attributes.Except(schedule.DeviceAttributeTemplates, comparer)];
                            added = [.. schedule.DeviceAttributeTemplates.Except(attributes, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDeviceAttributeTemplatesFromSchedule(connection, transaction,
                                schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            await AddDeviceAttributeTemplatesToSchedule(connection, transaction,
                                schedule.Id, added);
                        }
                    }

                    //Map device object templates to schedule.
                    {
                        List<GXObjectTemplate> objects = await GetDeviceObjectTemplatesByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXObjectTemplate, Guid>();
                        List<GXObjectTemplate> removed, added;
                        if (schedule.DeviceObjectTemplates == null)
                        {
                            removed = objects;
                            added = [];
                        }
                        else
                        {
                            removed = [.. objects.Except(schedule.DeviceObjectTemplates, comparer)];
                            added = [.. schedule.DeviceObjectTemplates.Except(objects, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDeviceObjectTemplatesFromSchedule(connection, transaction,
                                schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            await AddDeviceObjectTemplatesToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }
                    //Map device group attribute templates to schedule.
                    {
                        List<GXAttributeTemplate> attributes = await GetDeviceGroupAttributeTemplatesByScheduleId(connection, transaction,
                            schedule.Id);
                        var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                        List<GXAttributeTemplate> removed;
                        List<GXAttributeTemplate> added;
                        if (schedule.DeviceGroupAttributeTemplates == null)
                        {
                            removed = attributes;
                            added = [];
                        }
                        else
                        {
                            removed = [.. attributes.Except(schedule.DeviceGroupAttributeTemplates, comparer)];
                            added = [.. schedule.DeviceGroupAttributeTemplates.Except(attributes, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDeviceGroupAttributeTemplatesFromSchedule(connection, transaction,
                                schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            await AddDeviceGroupAttributeTemplatesToSchedule(connection, transaction,
                                schedule.Id, added);
                        }
                    }

                    //Map device group object templates to schedule.
                    {
                        List<GXObjectTemplate> objects = await GetDeviceGroupObjectTemplatesByScheduleId(connection, transaction, schedule.Id);
                        var comparer = new UniqueComparer<GXObjectTemplate, Guid>();
                        List<GXObjectTemplate> removed;
                        List<GXObjectTemplate> added;
                        if (schedule.DeviceGroupObjectTemplates == null)
                        {
                            removed = objects;
                            added = [];
                        }
                        else
                        {
                            removed = [.. objects.Except(schedule.DeviceGroupObjectTemplates, comparer)];
                            added = [.. schedule.DeviceGroupObjectTemplates.Except(objects, comparer)];
                        }
                        if (removed.Count != 0)
                        {
                            RemoveDeviceGroupObjectTemplatesFromSchedule(connection, transaction,
                                schedule.Id, removed);
                        }
                        if (added.Count != 0)
                        {
                            await AddDeviceGroupObjectTemplatesToSchedule(connection, transaction, schedule.Id, added);
                        }
                    }
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            foreach (var it in updates)
            {
                _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                    new GXTarget(GXSchedulePolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
            }
            List<GXLog> logs = [];
            foreach (var it in updates.Keys)
            {
                logs.Add(new GXLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Creator = it.Id,
                    Message = it.CreationTime == now ?
                Properties.Resources.ScheduleCreated :
                Properties.Resources.ScheduleUpdated
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var scheduleLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("schedule");
                await scheduleLogRepository.AddAsync("Update", logs, cancellationToken);
            }
            return [.. list];
        }, cancellationToken);
        }

        /// <summary>
        /// Add devices to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="devices">Devices that are added for the schedule.</param>
        private static void AddDevicesToSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDevice> devices)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDevice> list = [];
            foreach (GXDevice it in devices)
            {
                list.Add(new GXScheduleToDevice()
                {
                    ScheduleId = scheduleId,
                    DeviceId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove devices from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="devices">Devices  that are removed from the schedule.</param>
        private static void RemoveDevicesFromSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDevice> devices)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDevice>();
            foreach (var it in devices)
            {
                args.Where.Or<GXScheduleToDevice>(w => w.DeviceId == it.Id && w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add script methods to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="methods">Script methods that are added for the schedule.</param>
        private static void AddScriptMethodsToSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScriptMethod> methods)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleScript> list = [];
            foreach (GXScriptMethod it in methods)
            {
                list.Add(new GXScheduleScript()
                {
                    ScheduleId = scheduleId,
                    ScriptMethodId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove script methods from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="methods">Script methods that are removed from the schedule.</param>
        private static void RemoveScriptMethodsFromSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScriptMethod> methods)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleScript>();
            foreach (var it in methods)
            {
                args.Where.Or<GXScheduleScript>(w => w.ScriptMethodId == it.Id && w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add devices groups to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Devices groups that are added for the schedule.</param>
        private static void AddDeviceGroupsToSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDeviceGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceGroup> list = [];
            foreach (GXDeviceGroup it in groups)
            {
                list.Add(new GXScheduleToDeviceGroup()
                {
                    ScheduleId = scheduleId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device groups from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Devices groups that are removed from the schedule.</param>
        private static void RemoveDeviceGroupsFromSchedule(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDeviceGroup> groups)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceGroup>();
            foreach (GXDeviceGroup it in groups)
            {
                args.Where.Or<GXScheduleToDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map schedule group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Schedule groups where the schedule is added.</param>
        private static void AddScheduleToScheduleGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScheduleGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleGroupSchedule> list = [];
            foreach (GXScheduleGroup it in groups)
            {
                list.Add(new GXScheduleGroupSchedule()
                {
                    ScheduleId = scheduleId,
                    ScheduleGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Schedule groups where the schedule is removed.</param>
        private static void RemoveSchedulesFromScheduleGroup(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScheduleGroup> groups)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleGroupSchedule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScheduleGroupSchedule>(w => w.ScheduleGroupId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add modules to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="modules">Modules that are added for the schedule.</param>
        private static void AddModulesToSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXModule> modules)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleModule> list = [];
            foreach (GXModule it in modules)
            {
                list.Add(new GXScheduleModule()
                {
                    ScheduleId = scheduleId,
                    ModuleId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove modules from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="modules">Modules that are removed from the schedule.</param>
        private static void RemoveModulesFromSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXModule> modules)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleModule>();
            foreach (GXModule it in modules)
            {
                args.Where.Or<GXScheduleModule>(w => w.ModuleId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <inheritdoc/>
        public async Task RunAsync(Guid id, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXSchedulePolicies.Edit);
            IGXScheduleService scheduleHandler = _serviceProvider.GetRequiredService<IGXScheduleService>();
            GXSchedule schedule = await ReadAsync(id, cancellationToken: cancellationToken) ?? throw new GXAmiForbiddenException(id.ToString());
            //Schedule creator is the user who runs the schedule.
            schedule.Creator = new GXUser() { Id = ServerHelpers.GetUserId(User1) };
            await scheduleHandler.RunAsync(User1, schedule, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<GXScheduleModule?> GetModuleSettingsAsync(
            GXScheduleModule? settings, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXSchedulePolicies.View);
            if (settings == null)
            {
                throw new GXAmiForbiddenException();
            }
            GXSelectArgs args = GXSelectArgs.SelectAll<GXScheduleModule>(w => w.Removed == null
            && w.ScheduleId == settings.ScheduleId &&
            w.ModuleId == settings.ModuleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            var tmp = await connection.SingleOrDefaultAsync<GXScheduleModule>(args);
            if (tmp != null && tmp.ScheduleId == Guid.Empty)
            {
                tmp = null;
            }
            return tmp;
        }

        /// <inheritdoc/>
        public async Task UpdateModuleSettingsAsync(
            GXScheduleModule? settings, CancellationToken cancellationToken)
        {
            await GXHistoryMutation.RunIdsAsync<GXSchedule, Guid>(_dbFactory, User, _serviceProvider, settings == null ? Array.Empty<Guid>() : new[] { settings.ScheduleId },
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXSchedulePolicies.Edit);
            if (settings == null)
            {
                throw new GXAmiForbiddenException();
            }
            GXSelectArgs m = GXSelectArgs.Select<GXScheduleModule>(q => new { q.ScheduleId, q.ConcurrencyStamp },
                w => w.Removed == null && w.ScheduleId == settings.ScheduleId &&
                w.ModuleId == settings.ModuleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXScheduleModule? item = connection.SingleOrDefault<GXScheduleModule>(m);
            if (!string.IsNullOrEmpty(item?.ConcurrencyStamp) && item.ConcurrencyStamp != settings.ConcurrencyStamp)
            {
                throw new ArgumentException(Properties.Resources.ContentEdited);
            }
            if (item == null || item.ScheduleId == Guid.Empty)
            {
                settings.CreationTime = DateTime.Now;
                settings.ConcurrencyStamp = Guid.NewGuid().ToString();
                GXInsertArgs args = GXInsertArgs.Insert(settings);
                await connection.InsertAsync(args, cancellationToken);
            }
            else
            {
                settings.Updated = DateTime.Now;
                settings.ConcurrencyStamp = Guid.NewGuid().ToString();
                GXUpdateArgs args = GXUpdateArgs.Update(settings,
                    u => new { u.Updated, u.ConcurrencyStamp, u.Settings });
                args.Where.And<GXScheduleModule>(w => w.Removed == null &&
                w.ScheduleId == settings.ScheduleId && w.ModuleId == settings.ModuleId);
                await connection.UpdateAsync(args, cancellationToken);
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Add device objects to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">ObjectTemplatess that are added for the schedule.</param>
        private static async Task AddDeviceObjectTemplatesToSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXObjectTemplate> objects)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceObjectTemplate> list = [];
            foreach (GXObjectTemplate it in objects)
            {
                list.Add(new GXScheduleToDeviceObjectTemplate()
                {
                    ScheduleId = scheduleId,
                    ObjectTemplateId = it.Id,
                    CreationTime = now
                });
            }
            await connection.InsertAsync(transaction,
                GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device objects from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">ObjectTemplatess that are removed from the schedule.</param>
        private static void RemoveDeviceObjectTemplatesFromSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXObjectTemplate> objects)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceObjectTemplate>();
            foreach (var it in objects)
            {
                args.Where.Or<GXScheduleToDeviceObjectTemplate>(w =>
                w.ObjectTemplateId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add device group objects to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">Object templates that are added for the schedule.</param>
        private static async Task AddDeviceGroupObjectTemplatesToSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXObjectTemplate> objects)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceGroupObjectTemplate> list = [];
            foreach (GXObjectTemplate it in objects)
            {
                list.Add(new GXScheduleToDeviceGroupObjectTemplate()
                {
                    ScheduleId = scheduleId,
                    ObjectTemplateId = it.Id,
                    CreationTime = now
                });
            }
            await connection.InsertAsync(transaction,
                GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device group objects from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">ObjectTemplatess that are removed from the schedule.</param>
        private static void RemoveDeviceGroupObjectTemplatesFromSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXObjectTemplate> objects)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceGroupObjectTemplate>();
            foreach (var it in objects)
            {
                args.Where.Or<GXScheduleToDeviceGroupObjectTemplate>(w =>
                w.ObjectTemplateId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add device attributes to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Device attribute templates that are added for the schedule.</param>
        private static async Task AddDeviceAttributeTemplatesToSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXAttributeTemplate> attributes)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceAttributeTemplate> list = [];
            foreach (GXAttributeTemplate it in attributes)
            {
                list.Add(new GXScheduleToDeviceAttributeTemplate()
                {
                    ScheduleId = scheduleId,
                    AttributeTemplateId = it.Id,
                    CreationTime = now
                });
            }
            await connection.InsertAsync(transaction,
                GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device attributes from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Device attribute templatess that are removed from the schedule.</param>
        private static void RemoveDeviceAttributeTemplatesFromSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXAttributeTemplate> attributes)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceAttributeTemplate>();
            foreach (var it in attributes)
            {
                args.Where.Or<GXScheduleToDeviceAttributeTemplate>(w =>
                w.AttributeTemplateId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add device group attribute templates to schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Device group attribute templates that are added for the schedule.</param>
        private static async Task AddDeviceGroupAttributeTemplatesToSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXAttributeTemplate> attributes)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceGroupAttributeTemplate> list = [];
            foreach (GXAttributeTemplate it in attributes)
            {
                list.Add(new GXScheduleToDeviceGroupAttributeTemplate()
                {
                    ScheduleId = scheduleId,
                    AttributeTemplateId = it.Id,
                    CreationTime = now
                });
            }
            await connection.InsertAsync(transaction,
                GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device group attributes from schedule.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Device group attribute templatess that are removed from the schedule.</param>
        private static void RemoveDeviceGroupAttributeTemplatesFromSchedule(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleId,
            IEnumerable<GXAttributeTemplate> attributes)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceGroupAttributeTemplate>();
            foreach (var it in attributes)
            {
                args.Where.Or<GXScheduleToDeviceGroupAttributeTemplate>(w =>
                w.AttributeTemplateId == it.Id &&
                w.ScheduleId == scheduleId);
            }
            connection.Delete(transaction, args);
        }
    }
}
