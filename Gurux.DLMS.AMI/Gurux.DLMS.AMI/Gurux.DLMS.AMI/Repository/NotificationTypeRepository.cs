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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Data;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public partial class NotificationTypeRepository : INotificationTypeRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ClaimsPrincipal User;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache? _notifications;

        /// <summary>        
        /// Update blocked items in constructor.
        /// </summary>
        public NotificationTypeRepository(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            NotificationCache? notifications = null)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _authorizationService = authorizationService;
            _dbFactory = dbFactory;
            _notifications = notifications;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXNotificationType>> ListAsync(ListNotificationType? request,
            ListNotificationTypeResponse? response = null,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationTypePolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXNotificationType>();
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXNotificationType>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included?.Any() == true)
            {
                arg.Where.And<GXNotificationType>(w => request.Included.Contains(w.Id));
            }
            arg.Where.FilterBy(request?.Filter);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXNotificationType>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXNotificationType>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXNotificationType>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXNotificationType>(q => GXSql.Count(1));
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
            GXNotificationType[] types = [.. (await connection.SelectAsync<GXNotificationType>(arg))];
            foreach (var type in types)
            {
                type.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXNotificationType), type.Id, cancellationToken);
            }
            if (response != null)
            {
                response.NotificationTypes = types;
                if (response.Count == 0)
                {
                    response.Count = types.Length;
                }
            }
            return types;
        }

        /// <inheritdoc/>
        public async Task<GXNotificationType> ReadAsync(Guid id, Expression<Func<GXNotificationType, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationTypePolicies.View);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXNotificationType>(w => w.Id == id && w.Removed == null);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var type = await connection.SingleOrDefaultAsync<GXNotificationType>(arg) ?? throw new GXAmiNotFoundException(Properties.Resources.IpAccessControl + " " + Properties.Resources.Id + " " + id.ToString());
                type.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXNotificationType), type.Id, cancellationToken);
                return type;

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
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXNotificationType> list,
            Expression<Func<GXNotificationType, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            list = list.ToArray();
            return await GXHistoryMutation.RunAsync<GXNotificationType, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, list,
        async () =>
        {
            try
            {

                var items = list.ToArray();
                if (items.Any(type => type.Id == Guid.Empty))
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationTypePolicies.Add);
                }
                if (items.Any(type => type.Id != Guid.Empty))
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationTypePolicies.Edit);
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
                {
                    foreach (var type in items)
                    {
                        if ((columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Name))) &&
                            (string.IsNullOrWhiteSpace(type.Name) || type.Name.Length > 128))
                        {
                            throw new ArgumentException(Properties.Resources.InvalidName);
                        }
                        if (type.Id == Guid.Empty)
                        {
                            type.Creator = new GXUser { Id = ServerHelpers.GetUserId(User) };
                            type.CreationTime = DateTimeOffset.UtcNow;
                            type.ConcurrencyStamp = Guid.NewGuid().ToString();
                            await connection.InsertAsync(transaction, GXInsertArgs.Insert(type), cancellationToken);
                        }
                        else
                        {
                            var stored = await connection.SingleOrDefaultAsync<GXNotificationType>(transaction,
                                GXSelectArgs.SelectAll<GXNotificationType>(t => t.Id == type.Id && t.Removed == null), cancellationToken)
                                ?? throw new GXAmiNotFoundException("Notification type not found.");
                            if (!string.Equals(stored.ConcurrencyStamp, type.ConcurrencyStamp, StringComparison.Ordinal))
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            if (columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Name)))
                            {
                                stored.Name = type.Name;
                            }
                            if (columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Enabled)))
                            {
                                stored.Enabled = type.Enabled;
                            }
                            if (columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Code)))
                            {
                                stored.Code = type.Code;
                            }
                            if (columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Removed)))
                            {
                                stored.Removed = type.Removed;
                            }
                            stored.Updated = DateTimeOffset.UtcNow;
                            stored.ConcurrencyStamp = Guid.NewGuid().ToString();
                            var update = GXUpdateArgs.Update(stored);
                            update.Exclude<GXNotificationType>(t => new { t.CreationTime, t.Creator });
                            await connection.UpdateAsync(transaction, update, cancellationToken);
                            type.Updated = stored.Updated;
                            type.ConcurrencyStamp = stored.ConcurrencyStamp;
                        }
                        if (type.Policies != null && (columns == null || ServerHelpers.Contains(columns, nameof(GXNotificationType.Policies))))
                        {
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXNotificationType), type.Id,
                                type.Policies, cancellationToken, transaction);
                        }
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                if (_notifications != null)
                {
                    foreach (var type in items)
                    {
                        var recipients = GXSelectArgs.Select<GXUser>(u => u.Id!);
                        recipients.Joins.AddInnerJoin<GXNotification, GXUser>(n => n.Creator!, u => u.Id!);
                        recipients.Where.And<GXNotification>(n => n.Type == type && n.Removed == null);
                        var users = await connection.SelectAsync<GXUser>(recipients, cancellationToken);
                        _notifications.Add(users.Select(u => u.Id!).Distinct(), new GXTarget(GXNotificationPolicies.Edit, type.Id));
                    }
                }
                return [.. items.Select(type => type.Id)];

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
                        return await UpsertAsync(list, columns, linkedCts.Token);
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

        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> list, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default)
        {
            list = list.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXNotificationType, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, list,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXNotificationTypePolicies.Delete);
                GXDeleteArgs del = GXDeleteArgs.Delete<GXNotificationType>(w => list.Contains(w.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                await connection.DeleteAsync(del, cancellationToken);

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
                        await DeleteAsync(list, delete, notify, linkedCts.Token);
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

        /// <inheritdoc/>
        public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
            {
            await GXHistoryMutation.RunAllAsync<GXNotificationType>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
        async () =>
        {
            await ApplyDefaultsAsync(null, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            ids = ids.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXNotificationType, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ids,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(ids);
            var selected = ids.Distinct().ToArray();
            if (selected.Length == 0 || selected.Contains(Guid.Empty))
            {
                throw new ArgumentException("Existing notification type identifiers are required.", nameof(ids));
            }
            await ApplyDefaultsAsync(selected, cancellationToken);
        }, cancellationToken);
        }

        private async Task ApplyDefaultsAsync(Guid[]? ids, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User,
                ids == null ? GXNotificationTypePolicies.Add : GXNotificationTypePolicies.Edit);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            if (connection.Connection.State != System.Data.ConnectionState.Open)
            {
                await connection.Connection.OpenAsync(cancellationToken);
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                var rows = (await connection.SelectAsync<GXNotificationType>(transaction,
                    GXSelectArgs.SelectAll<GXNotificationType>(), cancellationToken)).ToArray();
                var defaults = new[]
                {
                    new GXNotificationType { Name = "UI notifications", Description = Properties.Resources.UINotificationDescription, Code = "UI", Enabled = true, CreationMode = NotificationCreationMode.Automatic },
                    new GXNotificationType { Name = "Toaster notifications", Description = Properties.Resources.ToasterNotificationDescription, Code = "TOASTER", Enabled = true, CreationMode = NotificationCreationMode.Automatic },
                    new GXNotificationType { Name = "Badge", Description = Properties.Resources.BadgeNotificationDescription, Code = "BADGE", Enabled = true, CreationMode = NotificationCreationMode.Automatic }
                };
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        var row = rows.SingleOrDefault(r => r.Id == id);
                        if (row == null || !defaults.Any(d => string.Equals(d.Code, row.Code, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new ArgumentException($"Notification type '{id}' has no matching built-in default.", nameof(ids));
                        }
                    }
                }
                var user = new GXUser { Id = ServerHelpers.GetUserId(User) };
                foreach (var item in defaults)
                {
                    var matches = rows.Where(r => string.Equals(r.Code, item.Code, StringComparison.OrdinalIgnoreCase));
                    if (ids == null)
                    {
                        if (matches.Any())
                        {
                            continue;
                        }
                        item.Creator = user;
                        item.CreationTime = DateTimeOffset.Now;
                        item.ConcurrencyStamp = Guid.NewGuid().ToString();
                        await connection.InsertAsync(transaction, GXInsertArgs.Insert(item), cancellationToken);
                    }
                    else
                    {
                        foreach (var row in matches.Where(r => ids.Contains(r.Id)))
                        {
                            row.Name = item.Name;
                            row.Description = item.Description;
                            row.Enabled = item.Enabled;
                            row.Code = item.Code;
                            row.CreationMode = item.CreationMode;
                            row.Removed = null;
                            row.Updated = DateTimeOffset.Now;
                            row.ConcurrencyStamp = Guid.NewGuid().ToString();
                            var update = GXUpdateArgs.Update(row);
                            update.Exclude<GXNotificationType>(r => new { r.CreationTime, r.Creator });
                            await connection.UpdateAsync(transaction, update, cancellationToken);
                        }
                    }
                }
                var subscriptions = await GXAdminToasterDefaults.EnsureAsync(connection, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                foreach (var subscription in subscriptions)
                    {
                    _notifications?.Add(subscription.Owner, new GXTarget(subscription.Added ? GXNotificationPolicies.Add : GXNotificationPolicies.Edit, subscription.Id));
                }
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        // Lifecycle operations share this repository's persistence rules and notification queue.
        internal static IModuleNotificationTypeRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleNotificationTypeRepository
        {
            public async Task RemoveAsync(CancellationToken token)
            {
                var id = await scope.Settings.ReadNotificationTypeIdAsync(token);
                if (id == null)
                {
                    return;
                }

                var type = await scope.Connection.SingleOrDefaultAsync<GXNotificationType>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXNotificationType>(t => t.Id == id.Value), token);
                if (type != null)
                {
                    if (type.Code != scope.ModuleId.ToUpperInvariant())
                    {
                        throw new InvalidOperationException("The notification channel does not match this module's ownership record.");
                    }

                    await scope.UserNotifications.RemoveAsync(type, token);
                    await scope.Connection.DeleteAsync(scope.Transaction, GXDeleteArgs.DeleteById<GXNotificationType>(type.Id), token);
                    await GXModuleRepositoryNotifications.QueueAsync(scope, GXNotificationTypePolicies.Delete, type.Id.ToString(), token);
                }
                await scope.Settings.SetNotificationTypeIdAsync(null, token);
            }

            public async Task<GXNotificationType> EnsureAsync(CancellationToken token)
            {
                var normalized = scope.ModuleId.ToUpperInvariant();
                var existing = await scope.Connection.SingleOrDefaultAsync<GXNotificationType>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXNotificationType>(t => t.Code == normalized), token);
                if (existing != null)
                {
                    return existing;
                }
                // Module lifecycle resources run without a request. Attribute generated
                // channels to the persisted module owner, never an ambient caller.
                var creatorArgs = GXSelectArgs.Select<GXUser>(u => u.Id!);
                creatorArgs.Joins.AddInnerJoin<GXUser, GXModule>(u => u.Id!, m => m.Creator!);
                creatorArgs.Where.And<GXModule>(m => m.Id == scope.ModuleId);
                var creator = await scope.Connection.SingleOrDefaultAsync<GXUser>(scope.Transaction, creatorArgs, token)
                    ?? throw new InvalidOperationException("The module creator is unavailable.");
                var type = new GXNotificationType
                {
                    Creator = creator,
                    Name = scope.ModuleId,
                    Code = normalized,
                    Enabled = true,
                    CreationTime = DateTimeOffset.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                Validator.ValidateObject(type, new ValidationContext(type), true);
                await scope.Connection.InsertAsync(scope.Transaction, GXInsertArgs.Insert(type), token);
                await scope.Settings.SetNotificationTypeIdAsync(type.Id, token);
                await GXModuleRepositoryNotifications.QueueAsync(scope, GXNotificationTypePolicies.Add, type.Id.ToString(), token);
                return type;
            }
        }
    }
}
