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
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

using Gurux.DLMS.AMI.Hubs;

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;
namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class DeviceTraceRepository : IDeviceTraceRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ILogRepository _systemLogRepository;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTraceRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IDeviceRepository deviceRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            NotificationCache notificationCache,
            ILogRepository systemLogRepository)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _deviceRepository = deviceRepository;
            _userRepository = userRepository;
            _notificationCache = notificationCache;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository.ForSource("system");
            _authorizationService = authorizationService;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXDeviceTrace>> ListAsync(
            ListDeviceTrace? request,
            ListDeviceTraceResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTracePolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the traces.
                arg = GXSelectArgs.SelectAll<GXDeviceTrace>();
                arg.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(j => j.Device!, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceTracesByUser(s => "*", userId);
            }
            arg.Columns.Add<GXDevice>(c => new { c.Id, c.Name });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXDeviceTrace>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXDeviceTrace>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            //Trace is shown as ascending order;
            GXDateTimeFilters.Apply<GXDeviceTrace>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTrace>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
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
                arg.OrderBy.Add<GXDeviceTrace>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXDeviceTrace>(q => q.CreationTime!);
            }
            //Traces are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Traces!);
            GXDeviceTrace[] traces = [.. (await connection.SelectAsync<GXDeviceTrace>(arg, cancellationToken))];
            if (response != null)
            {
                response.Traces = traces;
                if (response.Count == 0)
                {
                    response.Count = traces.Length;
                }
            }
            return traces;
        }

        /// <inheritdoc/>
        public async Task<GXDeviceTrace> ReadAsync(Guid id, Expression<Func<GXDeviceTrace, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTracePolicies.View);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTrace>(where => where.Id == id);
                //Get device.
                arg.Columns.Add<GXDevice>();
                arg.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(x => x.Device!, y => y.Id);
                //Traces are ignored from the device 
                //so there is no reference relation that is causing problems with JSON parser.
                arg.Columns.Exclude<GXDevice>(e => e.Traces!);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXDeviceTrace? deviceTrace = (await connection.SingleOrDefaultAsync<GXDeviceTrace>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                return deviceTrace;

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
        public async Task AddAsync(string type, IEnumerable<GXDeviceTrace> deviceTraces, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTracePolicies.Add);
            foreach (var it in deviceTraces)
            {
                it.CreationTime = DateTime.Now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.DeviceLog, type, cancellationToken);
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            connection.Insert(GXInsertArgs.InsertRange(deviceTraces));
            var users = await GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDevice),
                deviceTraces.Select(s => s.Device!.Id).ToArray(), NotificationAction.Add, GXDeviceTracePolicies.View, connection, notifications: _notificationCache);
            if (users != null && users.Count != 0)
            {
                _notificationCache.Add(users, new GXTarget(GXDeviceTracePolicies.Add));
            }
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDevice), id, action, GXDeviceTracePolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<Guid>? devices, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTracePolicies.Clear);
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDevicesByUser(s => "*", id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXDevice>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTrace>(y => y.Id, x => x.Device!);
            if (devices != null && devices.Any())
            {
                arg.Where.And<GXDevice>(w => devices.Contains(w.Id));
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXDevice> errors = await connection.SelectAsync<GXDevice>(arg);
            var list = await GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDevice),
                errors.Select(d => d.Id).ToArray(), NotificationAction.Clear, GXDeviceTracePolicies.View, connection, notifications: _notificationCache);
            if (admin && (devices == null || !devices.Any()))
            {
                //Admin clears all device errors.
                connection.Truncate<GXDeviceTrace>();
            }
            else if (errors.Count != 0)
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXDeviceTrace>(w => errors.Contains(w.Device!));
                await connection.DeleteAsync(args, cancellationToken);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.DeviceTrace, cancellationToken);
            if (devices?.Any() == true)
            {
                //Notify users whose device errors are cleared.
                foreach (var it in errors)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Clear, connection),
                                    new GXTarget(GXDeviceTracePolicies.Clear, it.Id, it.Name));
                }
            }
            else
            {
                //Notify users that all device errors are cleared.
                _notificationCache.Add(list, new GXTarget(GXDeviceTracePolicies.Clear));
            }
            await _systemLogRepository.AddAsync(TargetType.DeviceTrace, [new GXLog(TraceLevel.Info) { Message = Properties.Resources.Clear }], cancellationToken);
        }
    }
}
