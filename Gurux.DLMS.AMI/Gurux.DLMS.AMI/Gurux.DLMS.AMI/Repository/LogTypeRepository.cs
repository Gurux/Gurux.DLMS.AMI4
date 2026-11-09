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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public partial class LogTypeRepository : ILogTypeRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LogTypeRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private async Task<List<string>> GetNotifiedUsers(Guid id, NotificationAction action)
        {
            _ = id;
            GXSelectArgs args = GXQuery.GetUsersByPermission(action, GXLogTypePolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = connection.Select<GXUser>(args);
            return [.. users.Select(s => s.Id!)];
        }
        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXLogType, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            groups = groups.Distinct().ToArray();
            GXSelectArgs arg = GXSelectArgs.Select<GXLogType>(s => new { s.Id, s.PublicId, s.Name, s.NormalizedName }, w => groups.Contains(w.PublicId));
            var exists = await connection.SelectAsync<GXLogType>(arg, cancellationToken);
            if (exists.Count != groups.Count() || exists.Count == 0)
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            foreach (var type in exists)
            {
                var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), type.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXLogType it in exists)
                {
                    if (it.Name?.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase) == true ||
                        it.NormalizedName?.StartsWith("BUILTIN:", StringComparison.Ordinal) == true)
                    {
                        throw new InvalidOperationException("Built-in log types cannot be deleted.");
                    }

                    if (!groups.Contains(it.PublicId))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
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
                        await DeleteAsync(groups, delete, notify, linkedCts.Token);
                        return;
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
            if (notify)
            {
                var users = await GetNotifiedUsers(Guid.Empty, NotificationAction.Remove);
                foreach (var type in exists)
                {
                    _notificationCache.Add(users, new GXTarget(GXLogTypePolicies.Delete, type.PublicId));
                }
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXLogType>> ListAsync(
            ListLogTypes? request,
            ListLogTypesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLogType>();
            if (request != null)
            {
                //If Log groups are filtered by user.

                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXLogType>(w => !request.Exclude.Contains(w.PublicId));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXLogType>(w => request.Included.Contains(w.PublicId));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXLogType>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXLogType>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXLogType>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (!(request?.AllUsers == true && User.IsInRole(GXRoles.Admin)))
            {
                var candidates = await connection.SelectAsync<GXLogType>(arg);
                var allowedIds = new List<long>();
                foreach (var candidate in candidates)
                {
                    candidate.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), candidate.Id, cancellationToken);
                    if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, candidate.Policies, cancellationToken))
                    {
                        allowedIds.Add(candidate.Id);
                    }
                }
                if (allowedIds.Count == 0)
                {
                    if (response != null)
                    {
                        response.Count = 0;
                        response.LogTypes = [];
                    }
                    return [];
                }
                arg.Where.And<GXLogType>(w => allowedIds.Contains(w.Id));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXLogType>(q => GXSql.Count(1));
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
            GXLogType[] types = [.. (await connection.SelectAsync<GXLogType>(arg))];
            foreach (var resource in types)
            {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), resource.Id, cancellationToken);
            }

            if (response != null)
            {
                response.LogTypes = types;
                if (response.Count == 0)
                {
                    response.Count = types.Length;
                }
            }
            return types;
        }

        /// <inheritdoc />
        public async Task<GXLogType> ReadAsync(Guid id, Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.View);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXLogType>(w => w.PublicId == id);
                arg.Columns.Add<GXLogType>();
                arg.Columns.Exclude<GXLogType>(e => e.Logs!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXLogType>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), group.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, group.Policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXLogType, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXLogType>(w => w.PublicId == id);
                group.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return group;

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
            IEnumerable<GXLogType> LogGroups,
            Expression<Func<GXLogType, object>>? columns, CancellationToken cancellationToken = default)
        {
            LogGroups = LogGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXLogType, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, LogGroups,
        async () =>
        {
            try
            {

                LogGroups = [.. LogGroups];
                if (LogGroups.Any(g => g.Id == 0))
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.Add);
                }

                if (LogGroups.Any(g => g.Id != 0))
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.Edit);
                }

                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXLogType, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXLogType it in LogGroups)
                {
                    if (it.Id != 0)
                    {
                        var existingType = await connection.SingleOrDefaultAsync<GXLogType>(GXSelectArgs.Select<GXLogType>(t => new { t.PublicId, t.Name, t.NormalizedName }, t => t.Id == it.Id), cancellationToken) ?? throw new ArgumentException("Unknown log type.");
                        var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), it.Id, cancellationToken);
                        if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, policies, cancellationToken))
                        {
                            throw new UnauthorizedAccessException();
                        }

                        if (it.PublicId != Guid.Empty && it.PublicId != existingType.PublicId)
                        {
                            throw new ArgumentException("The log type identifiers do not match.");
                        }

                        it.PublicId = existingType.PublicId;
                        if (existingType.NormalizedName?.StartsWith("BUILTIN:", StringComparison.Ordinal) == true && it.Name != existingType.Name)
                        {
                            throw new ArgumentException("Built-in log type names are permanent identifiers.");
                        }

                        if (it.Name?.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase) == true &&
                                                    existingType.NormalizedName?.StartsWith("BUILTIN:", StringComparison.Ordinal) != true)
                        {
                            throw new ArgumentException("The builtin log type namespace is reserved.");
                        }
                    }
                    else if (it.Name?.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        throw new ArgumentException("The builtin log type namespace is reserved.");
                    }

                    await GXResourcePolicyAccess.ValidateAsync(connection, it.Policies, cancellationToken);
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    it.NormalizedName = it.Name.ToUpperInvariant();

                    if (it.Id == 0)
                    {
                        if (it.PublicId == Guid.Empty)
                        {
                            it.PublicId = Guid.NewGuid();
                        }

                        it.Creator = creator;
                        it.CreationTime = now;
                        GXInsertArgs args = GXInsertArgs.Insert(it);
                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Insert(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXLogType), it.Id, it.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.PublicId, NotificationAction.Add),
                            new GXTarget(GXLogTypePolicies.Add, it.PublicId, it.Name));
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXLogType>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXLogType>(q => q.PublicId);
                        args.Exclude<GXLogType>(q => new
                        {
                            q.CreationTime,
                            q.Logs
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXLogType>(q => q.Creator!);
                        }

                        using (var policyTransaction = connection.BeginTransaction())
                        {
                            connection.Update(policyTransaction, args);
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXLogType), it.Id, it.Policies,
                                cancellationToken, policyTransaction);
                            policyTransaction.Commit();
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.PublicId, NotificationAction.Edit),
                            new GXTarget(GXLogTypePolicies.Edit, it.PublicId, it.Name));
                    }
                }
                return [.. LogGroups.Select(s => s.PublicId)];

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
                        return await UpsertAsync(LogGroups, columns, linkedCts.Token);
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

        // Lifecycle operations share this repository's persistence rules and notification queue.
        internal static IModuleLogTypeRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleLogTypeRepository
        {
            public Task<GXLogType?> ReadAsync(CancellationToken token)
            {
                var normalized = scope.ModuleId.ToUpperInvariant();
                return scope.Connection.SingleOrDefaultAsync<GXLogType>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXLogType>(t => t.NormalizedName == normalized), token);
            }

            public async Task<GXLogType> EnsureAsync(GXPolicy policy, CancellationToken token)
            {
                var existing = await ReadAsync(token);
                if (existing != null)
                {
                    return existing;
                }
                var type = new GXLogType(scope.ModuleId)
                {
                    PublicId = Guid.NewGuid(),
                    Level = 4,
                    NormalizedName = scope.ModuleId.ToUpperInvariant(),
                    CreationTime = DateTimeOffset.UtcNow
                };
                Validator.ValidateObject(type, new ValidationContext(type), true);
                await scope.Connection.InsertAsync(scope.Transaction, GXInsertArgs.Insert(type), token);
                await GXResourcePolicyStorage.SaveAsync(scope.Connection, typeof(GXLogType), type.Id, [policy.Name!], token, scope.Transaction);
                await GXModuleRepositoryNotifications.QueueAsync(scope, GXLogTypePolicies.Add, type.PublicId.ToString(), token);
                return type;
            }
        }
    }
}



