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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public partial class NotificationRepository : INotificationRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly NotificationCache _notificationCache;
        private readonly INotificationTypeRepository _notificationTypeRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRoleRepository _roleRepository;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationRepository(
            IDbConnectionFactory dbFactory,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            INotificationTypeRepository notificationTypeRepository,
            IRoleRepository roleRepository,
            NotificationCache notificationCache,
            IAuthorizationService authorizationService)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            User = httpContextAccessor.HttpContext.User;
            _notificationCache = notificationCache;
            _notificationTypeRepository = notificationTypeRepository;
            _roleRepository = roleRepository;
            _authorizationService = authorizationService;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> notifications,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            notifications = notifications.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXNotification, Guid>(_dbFactory, User, _serviceProvider, notifications,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationPolicies.Delete);

            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXNotification>(a => a.Id, q => notifications.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXNotification> list = connection.Select<GXNotification>(arg);
                foreach (var item in list)
                {
                    await ReadAsync(item.Id, null, cancellationToken);
                }

                DateTime now = DateTime.Now;
                foreach (GXNotification it in list)
                {
                    it.Removed = now;
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXNotification>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    string? userId = ServerHelpers.GetUserId(User);
                    _notificationCache.Add(userId, new GXTarget(GXNotificationPolicies.Delete, it.Id));
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
                        await DeleteAsync(notifications, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXNotification>> ListAsync(
            ListNotifications? request,
            ListNotificationResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationPolicies.View);
            if (User?.Identity?.IsAuthenticated != true)
            {
                return [];
            }
            var owner = new GXUser { Id = ServerHelpers.GetUserId(User) };
            var arg = GXSelectArgs.SelectAll<GXNotification>(n => n.Creator == owner);
            arg.Joins.AddInnerJoin<GXNotification, GXUser>(n => n.Creator!, u => u.Id!);
            if (request?.Filter == null)
                {
                arg.Where.And<GXNotification>(n => n.Removed == null);
            }
            //Return notification type with all queries since it's a small table and usually needed for display.
            arg.Columns.Add<GXNotificationType>(c => new { c.Id, c.Name, c.Code });
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationType>(j => j.Type!, j => j.Id);
            if (request?.Filter != null)
            {
                if (!string.IsNullOrEmpty(request.Filter.Creator?.Id))
                {
                    request.Filter.Creator = new GXUser() { Id = request.Filter.Creator.Id };
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXNotification>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXNotification>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXNotification>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXNotification>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXNotification>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            var recipient = User;
            await GXNotificationTypeAccess.RestrictAsync(connection, arg, _authorizationService, recipient, cancellationToken);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXNotification>(q => GXSql.Count(1));
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
            if (request?.Select != null && request.Select.Contains(TargetType.User))
            {
                arg.Joins.AddInnerJoin<GXNotification, GXUser>(j => j.Creator!, j => j.Id!);
                arg.Columns.Add<GXUser>(c => c.Id!);
            }
            GXNotification[] notifications = [.. (await connection.SelectAsync<GXNotification>(arg))];
            await GXNotificationSourceStorage.LoadAsync(connection, notifications, cancellationToken);
            if (response != null)
            {
                response.Notifications = notifications;
                if (response.Count == 0)
                {
                    response.Count = notifications.Length;
                }
            }
            return notifications;
        }

        /// <inheritdoc />
        public async Task<GXNotification> ReadAsync(Guid id, Expression<Func<GXNotification, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationPolicies.View);
            try
            {

                var userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXQuery.GetNotificationsByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXNotification, GXNotificationType>(x => x.Type!, y => y.Id);
                arg.Columns.Add<GXNotificationType>(c => new { c.Id, c.Name });
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXNotification? notification = await connection.SingleOrDefaultAsync<GXNotification>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXNotification, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXNotification>(w => w.Id == id);
                notification.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                var recipient = User;
                if (notification.Type == null || !await GXNotificationTypeAccess.AllowsAsync(connection, _authorizationService, recipient, notification.Type.Id, cancellationToken))
                    {
                    throw new UnauthorizedAccessException();
                }
                await GXNotificationSourceStorage.LoadAsync(connection, [notification], cancellationToken);
                return notification;

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
            IEnumerable<GXNotification> notifications,
            Expression<Func<GXNotification, object>>? columns, CancellationToken cancellationToken = default)
        {
            notifications = notifications.ToArray();
            return await GXHistoryMutation.RunAsync<GXNotification, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, notifications,
        async () =>
        {
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            DateTime now = DateTime.Now;
            List<Guid> list = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            notifications = [.. notifications];
            foreach (var notification in notifications)
            {
                if (notification.Id == Guid.Empty || columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Sources)))
                    {
                    notification.Sources = GXNotificationSourceStorage.Validate(notification.Sources);
                }
                await _authorizationService.EnsureAnyPolicyAsync(User,
                    notification.Id == Guid.Empty ? GXNotificationPolicies.Add : GXNotificationPolicies.Edit);
                GXNotification? previous = null;
                if (notification.Id != Guid.Empty)
                {
                    previous = await ReadAsync(notification.Id, null, cancellationToken);
                }
                if (!string.IsNullOrWhiteSpace(notification.Creator?.Id) && notification.Creator.Id != creator.Id)
                    {
                    throw new UnauthorizedAccessException();
                }
                var recipient = User;
                var type = notification.Type ?? previous?.Type;
                if (type == null || !await GXNotificationTypeAccess.AllowsAsync(connection, _authorizationService, recipient, type.Id, cancellationToken))
                    {
                    throw new UnauthorizedAccessException();
                }
                if (notification.Id == Guid.Empty)
                    {
                    notification.Creator = new GXUser { Id = ServerHelpers.GetUserId(recipient) };
                }
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXNotification notification in notifications)
                {
                    if (string.IsNullOrEmpty(notification.Name) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (notification.Id == Guid.Empty)
                    {
                        notification.CreationTime = now;
                        notification.Creator ??= creator;
                        GXInsertArgs args = GXInsertArgs.Insert(notification);
                        args.Exclude<GXNotification>(q => new
                        {
                            q.Updated,
                            q.Removed
                        });
                        connection.Insert(transaction, args);
                        list.Add(notification.Id);
                        _notificationCache.Add(notification.Creator.Id!, new GXTarget(GXNotificationPolicies.Add, notification.Id, notification.Name));
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXNotification>(q => q.ConcurrencyStamp!, where => where.Id == notification.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != notification.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        notification.ConcurrencyStamp = Guid.NewGuid().ToString();
                        notification.Updated = now;
                        // Sources live in their own table; never pass that member to SQL UPDATE.
                        var storedColumns = columns;
                        if (columns != null && ServerHelpers.Contains(columns, nameof(GXNotification.Sources)))
                        {
                            var parameter = columns.Parameters[0];
                            var members = typeof(GXNotification).GetProperties()
                                .Where(p => p.Name != nameof(GXNotification.Sources) &&
                                    (p.Name == nameof(GXNotification.Updated) || ServerHelpers.Contains(columns, p.Name)))
                                .Select(p => Expression.Convert(Expression.Property(parameter, p), typeof(object)));
                            storedColumns = Expression.Lambda<Func<GXNotification, object>>(
                                Expression.NewArrayInit(typeof(object), members), parameter);
                        }
                        GXUpdateArgs args = GXUpdateArgs.Update(notification, storedColumns);
                        args.Exclude<GXNotification>(q => new
                        {
                            q.CreationTime,
                            q.Creator,
                        });
                        connection.Update(transaction, args);
                        _notificationCache.Add(creator.Id, new GXTarget(GXNotificationPolicies.Edit, notification.Id, notification.Name));
                    }
                    if (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Sources)) || list.Contains(notification.Id))
                        {
                        await GXNotificationSourceStorage.ReplaceAsync(connection, transaction, notification.Id, notification.Sources, cancellationToken);
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
                        return await UpsertAsync(notifications, columns, linkedCts.Token);
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
            return [.. list];
        }, cancellationToken);
        }

        // Lifecycle operations share this repository's persistence rules and notification queue.
        internal static IModuleNotificationRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleNotificationRepository
        {
            public async Task RemoveAsync(GXNotificationType type, CancellationToken token)
            {
                var notificationType = new GXNotificationType { Id = type.Id };
                var query = GXSelectArgs.SelectAll<GXNotification>(n => n.Type == notificationType);
                query.Joins.AddLeftJoin<GXNotification, GXUser>(n => n.Creator!, u => u.Id!);
                query.Columns.Add<GXUser>(u => u.Id!);
                foreach (var notification in await scope.Connection.SelectAsync<GXNotification>(scope.Transaction, query, token))
                {
                    await scope.Connection.DeleteAsync(scope.Transaction, GXDeleteArgs.DeleteById<GXNotification>(notification.Id), token);
                    if (notification.Creator?.Id is string owner)
                    {
                        await GXModuleRepositoryNotifications.QueueAsync(scope, GXNotificationPolicies.Delete, notification.Id.ToString(), token, [owner]);
                    }
                }
            }

            public async Task EnsureAdministratorsAsync(GXNotificationType type, CancellationToken token)
            {
                if (type.Removed != null)
                {
                    return;
                }
                var admins = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
                admins.Joins.AddInnerJoin<GXUser, GXUserRole>(u => u.Id!, r => r.UserId!);
                admins.Joins.AddInnerJoin<GXUserRole, GXRole>(u => u.RoleId!, r => r.Id);
                admins.Where.And<GXRole>(r => r.NormalizedName == "ADMIN" && r.Removed == null);
                foreach (var admin in await scope.Connection.SelectAsync<GXUser>(scope.Transaction, admins, token))
                {
                    // Both foreign keys must be captured at the same level for the ORM.
                    var notificationType = new GXNotificationType { Id = type.Id };
                    var owner = new GXUser { Id = admin.Id };
                    if (await scope.Connection.SingleOrDefaultAsync<GXNotification>(scope.Transaction, GXSelectArgs.Select<GXNotification>(n => n.Id, n => n.Type == notificationType && n.Creator == owner), token) != null)
                    {
                        continue;
                    }
                    var notification = new GXNotification(scope.ModuleId)
                    {
                        Type = type,
                        Creator = owner,
                        Sources = [],
                        TraceLevel = TraceLevel.Verbose,
                        CreationTime = DateTimeOffset.UtcNow
                    };
                    Validator.ValidateObject(notification, new ValidationContext(notification), true);
                    await scope.Connection.InsertAsync(scope.Transaction, GXInsertArgs.Insert(notification), token);
                    await GXModuleRepositoryNotifications.QueueAsync(scope, GXNotificationPolicies.Add, notification.Id.ToString(), token, [admin.Id!]);
                }
            }
        }

        private static readonly SemaphoreSlim DefaultsGate = new(1, 1);

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> EnsureDefaultsAsync(CancellationToken cancellationToken = default)
            {
            return await GXHistoryMutation.RunAllAsync<GXNotification, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider,
        async () =>
        {
            return await ApplyNotificationDefaultsAsync(null, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            ids = ids.ToArray();
            return await GXHistoryMutation.RunIdsAsync<GXNotification, Guid, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, ids,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(ids);
            var selected = ids.Distinct().ToArray();
            if (selected.Length == 0 || selected.Contains(Guid.Empty))
                {
                throw new ArgumentException("Select existing automatic notification defaults to restore.", nameof(ids));
            }
            return await ApplyNotificationDefaultsAsync(selected, cancellationToken);
        }, cancellationToken);
        }

        private async Task<IEnumerable<Guid>> ApplyNotificationDefaultsAsync(Guid[]? ids, CancellationToken token)
        {
            bool reset = ids != null;
            token.ThrowIfCancellationRequested();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationPolicies.Edit);
            await DefaultsGate.WaitAsync(token);
            try
            {
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(token);
                var recipient = User;
                var ownerId = ServerHelpers.GetUserId(recipient);
                var owner = new GXUser { Id = ownerId };
                if (await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!,
                    u => u.Id == ownerId && u.Removed == null), token) == null)
                    {
                    throw new ArgumentException("The notification owner is unavailable.", nameof(User));
                }

                var types = await connection.SelectAsync<GXNotificationType>(GXSelectArgs.SelectAll<GXNotificationType>(
                    t => t.CreationMode == NotificationCreationMode.Automatic && t.Enabled == true && t.Removed == null), token);
                var allowed = new List<GXNotificationType>();
                foreach (var type in types)
                    {
                    if (await GXNotificationTypeAccess.AllowsAsync(connection, _authorizationService, recipient, type.Id, token))
                        {
                        allowed.Add(type);
                    }
                }
                var sources = allowed.Any(t => string.Equals(t.Code, "UI", StringComparison.OrdinalIgnoreCase))
                    ? (await GXNotificationPermissions.ReadAsync(connection, ownerId, token)).Select(p => p.Name!).ToList() : [];

                var changes = new List<(GXNotification Notification, bool Added)>();
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
                try
                {
                    var query = GXSelectArgs.Select<GXNotification>(n => new { n.Id, n.Type, n.CreationTime },
                        n => n.Creator == owner && n.Removed == null);
                    query.Joins.AddInnerJoin<GXNotification, GXNotificationType>(n => n.Type!, t => t.Id);
                    query.Columns.Add<GXNotificationType>(t => t.Id);
                    var existing = await connection.SelectAsync<GXNotification>(transaction, query, token);
                    IEnumerable<(GXNotificationType Type, GXNotification? Previous)> targets;
                    if (ids != null)
                    {
                        var selected = existing.Where(n => ids.Contains(n.Id)).ToArray();
                        if (selected.Length != ids.Length || selected.Any(n => !allowed.Any(t => t.Id == n.Type?.Id)))
                            {
                            throw new ArgumentException("Only the selected user's available automatic notification defaults can be restored.", nameof(ids));
                        }
                        targets = selected.Select(n => (allowed.Single(t => t.Id == n.Type!.Id), (GXNotification?)n)).ToArray();
                    }
                    else
                    {
                        targets = allowed.OrderBy(t => t.Id).Select(type => (type, existing.Where(n => n.Type?.Id == type.Id)
                            .OrderBy(n => n.CreationTime).ThenBy(n => n.Id).FirstOrDefault())).ToArray();
                    }
                    foreach (var (type, previous) in targets)
                    {
                        if (previous != null && !reset)
                            {
                            continue;
                        }
                        var value = new GXNotification(type.Name)
                        {
                            Id = previous?.Id ?? Guid.Empty,
                            Creator = owner,
                            Type = new GXNotificationType { Id = type.Id },
                            Sources = string.Equals(type.Code, "UI", StringComparison.OrdinalIgnoreCase) ? sources
                                : string.Equals(type.Code, "TOASTER", StringComparison.OrdinalIgnoreCase) && recipient.IsInRole(GXRoles.Admin)
                                    ? [GXSystemLogPolicies.Add] : [],
                            TraceLevel = TraceLevel.Error,
                            ConcurrencyStamp = Guid.NewGuid().ToString()
                        };
                        if (previous == null)
                        {
                            value.CreationTime = DateTimeOffset.UtcNow;
                            await connection.InsertAsync(transaction, GXInsertArgs.Insert(value), token);
                        }
                        else
                        {
                            value.Updated = DateTimeOffset.UtcNow;
                            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(value, n => new
                            {
                                n.Name,
                                n.Description,
                                n.Enabled,
                                n.ScriptMethod,
                                n.MinimumDeliveryTime,
                                n.TraceLevel,
                                n.Updated,
                                n.ConcurrencyStamp
                            }), token);
                            await connection.DeleteAsync(transaction,
                                GXDeleteArgs.Delete<GXNotificationProperty>(p => p.Notification == value), token);
                        }
                        await GXNotificationSourceStorage.ReplaceAsync(connection, transaction, value.Id, value.Sources, token);
                        changes.Add((value, previous == null));
                    }
                    await transaction.CommitAsync(token);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
                foreach (var change in changes)
                    {
                    _notificationCache.Add(ownerId, new GXTarget(change.Added ? GXNotificationPolicies.Add : GXNotificationPolicies.Edit,
                        change.Notification.Id, change.Notification.Name));
                }
                return changes.Select(c => c.Notification.Id).ToArray();
            }
            finally { DefaultsGate.Release(); }
        }
    }
}
