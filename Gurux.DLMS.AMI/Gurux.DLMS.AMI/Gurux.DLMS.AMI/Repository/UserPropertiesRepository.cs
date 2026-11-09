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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserPropertiesRepository : IUserPropertiesRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly NotificationCache _notificationCache;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserPropertiesRepository(
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            // Operation policies can be granted directly without assigning a role.
            if (User.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException();
            }
            _dbFactory = dbFactory;
            _notificationCache = notificationCache;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> settings, CancellationToken cancellationToken = default)
        {
            settings = settings.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXUserProperty, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, settings,
        async () =>
        {
            try
            {
                if (!User.IsInRole(GXRoles.Admin) &&
                    !User.IsInRole(GXRoles.Authenticated))
                {
                    throw new UnauthorizedAccessException();
                }
                ArgumentNullException.ThrowIfNull(settings);
                var ids = settings.Distinct().ToArray();
                var user = new GXUser { Id = ServerHelpers.GetUserId(User) };
                var query = GXSelectArgs.SelectAll<GXUserProperty>(
                    q => q.User == user && q.Module == null && ids.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                var rows = await connection.SelectAsync<GXUserProperty>(transaction, query, cancellationToken);
                foreach (var row in rows)
                {
                    await GXPropertyStores.User.DeleteAsync(connection, transaction, user, row, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                foreach (var row in rows)
                {
                    _notificationCache.Add(user.Id, new GXTarget(GXUserPropertiesPolicies.Delete, row.Id));
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
                        await DeleteAsync(settings, linkedCts.Token);
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
        public async Task<IEnumerable<GXUserProperty>> ListAsync(
            ListUserProperties? request,
            ListUserPropertiesResponse? response,
            CancellationToken cancellationToken = default)
        {
            GXUser user = new() { Id = ServerHelpers.GetUserId(User) };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserProperty>(
                q => q.User == user && q.Module == null);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXUserProperty>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXUserProperty>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXUserProperty>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserProperty>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXUserProperty>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXUserProperty>(q => q.CreationTime!);
            }
            GXUserProperty[] properties = [.. (await connection.SelectAsync<GXUserProperty>(arg, cancellationToken))];
            response?.Properties = properties;
            return properties;
        }

        /// <inheritdoc />
        public async Task<GXUserProperty?> ReadAsync(string name, Expression<Func<GXUserProperty, object>>? columns = null, CancellationToken cancellationToken = default, string? userId = null)
        {
            try
            {

                string currentUserId = ServerHelpers.GetUserId(User);
                if (userId != null && userId != currentUserId && !User.IsInRole(GXRoles.Admin))
                {
                    throw new UnauthorizedAccessException();
                }
                GXUser user = new() { Id = userId ?? currentUserId };
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserProperty>(
                q => q.User == user && q.Module == null && q.Name == name);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXUserProperty? ret = await connection.SingleOrDefaultAsync<GXUserProperty>(arg);
                ret?.User = user;
                return ret;

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
                        return await ReadAsync(name, columns, linkedCts.Token, userId);
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
        public async Task UpsertAsync(
            IEnumerable<GXUserProperty> settings,
            Expression<Func<GXUserProperty, object>>? columns, CancellationToken cancellationToken = default)
        {
            settings = settings.ToArray();
            await GXHistoryMutation.RunAsync<GXUserProperty>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, settings,
        async () =>
        {
            try
            {

                ArgumentNullException.ThrowIfNull(settings);
                var currentUserId = ServerHelpers.GetUserId(User);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                var notifications = new List<(string UserId, string Policy, Guid Id, string? Version)>();
                foreach (var it in settings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ArgumentNullException.ThrowIfNull(it);
                    GXPropertyStores.User.ValidateName(it.Name);
                    if (it.Module != null)
                    {
                        throw new UnauthorizedAccessException("Module user data must be written through the module interface.");
                    }
                    if (it.Name == "Ami.Toaster.MaxCount" && !string.IsNullOrWhiteSpace(it.Value)
                        && (!int.TryParse(it.Value, out var maxCount) || maxCount < 1))
                    {
                        throw new ArgumentException("The maximum notification count must be positive.");
                    }
                    var ownerId = it.User?.Id ?? currentUserId;
                    if (ownerId != currentUserId && !User.IsInRole(GXRoles.Admin))
                    {
                        throw new UnauthorizedAccessException();
                    }
                    var owner = new GXUser { Id = ownerId };
                    if (await connection.SingleOrDefaultAsync<GXUser>(transaction,
                        GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == ownerId && u.Removed == null), cancellationToken) == null)
                    {
                        throw new ArgumentException("The user is unavailable.");
                    }
                    var previousVersion = it.ConcurrencyStamp;
                    var added = await GXPropertyStores.User.UpsertAsync(connection, transaction, owner, it, columns, cancellationToken);
                    notifications.Add((ownerId, added ? GXUserPropertiesPolicies.Add : GXUserPropertiesPolicies.Edit, it.Id,
                        added || it.ConcurrencyStamp != previousVersion ? it.ConcurrencyStamp : null));
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var notice in notifications)
                {
                    _notificationCache.Add(notice.UserId, new GXTarget(notice.Policy, notice.Id) { ConcurrencyStamp = notice.Version });
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
                        await UpsertAsync(settings, columns, linkedCts.Token);
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

        internal static IGXAmiModuleUserSettings ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IGXAmiModuleUserSettings
        {
            private GXModule Module => new() { Id = scope.ModuleId };
            private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

            private static void Validate(string userId, string? name = null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(userId);
                if (name != null && (string.IsNullOrWhiteSpace(name) || name.Length > 64))
                {
                    throw new ArgumentException("A setting name of at most 64 characters is required.");
                }
            }

            private Task<GXUserProperty?> FindAsync(string userId, string name, CancellationToken token)
            {
                var user = new GXUser { Id = userId };
                var module = Module;
                return scope.Connection.SingleOrDefaultAsync<GXUserProperty>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXUserProperty>(s => s.User == user && s.Module == module && s.Name == name), token);
            }

            public async Task<T?> ReadAsync<T>(string userId, string name, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(name);
                Validate(userId, name);
                var row = await FindAsync(userId, name, cancellationToken);
                return row?.Value == null ? default : JsonSerializer.Deserialize<T>(row.Value, Json);
            }

            public async Task<IReadOnlyList<AmiModuleUserSetting<T>>> ListAsync<T>(string userId, CancellationToken cancellationToken = default)
            {
                Validate(userId);
                var user = new GXUser { Id = userId };
                var module = Module;
                var rows = await scope.Connection.SelectAsync<GXUserProperty>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXUserProperty>(s => s.User == user && s.Module == module), cancellationToken);
                return [.. rows.Select(r => new AmiModuleUserSetting<T>(r.Name ?? throw new InvalidOperationException("Stored module user setting has no name."), JsonSerializer.Deserialize<T>(r.Value!, Json)!, r.ConcurrencyStamp!))];
            }

            public async Task UpsertAsync<T>(string userId, string name, T value, bool exclusiveToUser = false, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(name);
                ArgumentNullException.ThrowIfNull(value);
                Validate(userId, name);
                var user = new GXUser { Id = userId };
                var module = Module;
                if (await scope.Connection.SingleOrDefaultAsync<GXUser>(scope.Transaction, GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == userId && u.Removed == null), cancellationToken) == null)
                {
                    throw new ArgumentException("The user is unavailable.");
                }
                if (await scope.Connection.SingleOrDefaultAsync<GXModule>(scope.Transaction, GXSelectArgs.Select<GXModule>(m => m.Id, m => m.Id == scope.ModuleId), cancellationToken) == null)
                {
                    throw new InvalidOperationException("The module is unavailable.");
                }
                // An exclusive key's primary key excludes the user. The database rejects competing
                // inserts even when two host processes race; a later owner cannot overwrite it.
                if (exclusiveToUser)
                {
                    var matches = await scope.Connection.SelectAsync<GXUserProperty>(scope.Transaction, GXSelectArgs.SelectAll<GXUserProperty>(s => s.Module == module && s.Name == name), cancellationToken);
                    foreach (var match in matches)
                    {
                        var owned = await FindAsync(userId, name, cancellationToken);
                        if (owned == null || owned.Id != match.Id)
                        {
                            throw new InvalidOperationException("This module setting belongs to another user.");
                        }
                    }
                }
                var existing = await FindAsync(userId, name, cancellationToken);
                var row = existing ?? new GXUserProperty { User = user, Module = module, Name = name };
                // The ORM read does not hydrate these navigation references without explicit joins.
                row.User = user;
                row.Module = module;
                var beforeJson = existing == null ? null : GXHistorySnapshot.Serialize(row);
                row.Value = JsonSerializer.Serialize(value, Json);
                // Preserve the module API's established exclusive/nonexclusive identity scheme.
                var key = JsonSerializer.Serialize(new[] { scope.ModuleId, exclusiveToUser ? null : userId, name });
                var insertId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
                var added = await GXPropertyStores.User.SaveAsync(scope.Connection, scope.Transaction,
                    user, row, existing, cancellationToken: cancellationToken, insertId: insertId);
                await GXHistoryModulePropertyCapture.RecordAsync(scope, beforeJson, row, row.Id, cancellationToken);
                await NotifyAsync(userId, added ? GXUserPropertiesPolicies.Add : GXUserPropertiesPolicies.Edit, row.Id, cancellationToken);
            }

            public Task DeleteAsync(string userId, string name, CancellationToken cancellationToken = default)
                => DeleteCoreAsync(userId, name, null, cancellationToken);

            public Task DeleteIfUnchangedAsync(string userId, string name, string version, CancellationToken cancellationToken = default)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(version);
                return DeleteCoreAsync(userId, name, version, cancellationToken);
            }

            private async Task DeleteCoreAsync(string userId, string name, string? version, CancellationToken token)
            {
                ArgumentNullException.ThrowIfNull(name);
                Validate(userId, name);
                var row = await FindAsync(userId, name, token);
                if (row == null || (version != null && row.ConcurrencyStamp != version))
                {
                    return;
                }
                row.User = new GXUser { Id = userId };
                row.Module = Module;
                var beforeJson = GXHistorySnapshot.Serialize(row);
                var removed = await GXPropertyStores.User.TryDeleteAsync(scope.Connection, scope.Transaction,
                    row.User, row, token);
                if (removed)
                {
                    await GXHistoryModulePropertyCapture.RecordAsync(scope, beforeJson, null, row.Id, token);
                    await NotifyAsync(userId, GXUserPropertiesPolicies.Delete, row.Id, token);
                }
            }

            private Task NotifyAsync(string userId, string policy, Guid id, CancellationToken token)
                => GXModuleRepositoryNotifications.QueueAsync(scope, policy, id.ToString(), token, [userId]);
        }
    }
}
