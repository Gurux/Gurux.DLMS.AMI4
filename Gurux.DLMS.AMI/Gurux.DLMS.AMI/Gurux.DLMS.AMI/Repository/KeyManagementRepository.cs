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
using Gurux.DLMS.AMI.Client.Pages.Admin;
using Gurux.DLMS.AMI.Client.Pages.KeyManagement;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class KeyManagementRepository : IKeyManagementRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IKeyManagementGroupRepository _deviceKeyGroupRepository;
        private readonly IGXAmiCryptoService _cryproService;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IKeyManagementGroupRepository deviceKeyGroupRepository,
            IGXAmiCryptoService cryproService,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _deviceKeyGroupRepository = deviceKeyGroupRepository;
            _cryproService = cryproService;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXKeyManagement), id, action, GXKeyManagementLogPolicies.View, connection, transaction);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid keyId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByKeyManagement(s => s.Id!,
                ServerHelpers.GetUserId(User), keyId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.HasClaim("Permission", GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? keyIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByKeyManagements(s => s.Id!,
                ServerHelpers.GetUserId(User), keyIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.HasClaim("Permission", GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> keys,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            keys = keys.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXKeyManagement, Guid>(_dbFactory, User, _serviceProvider, keys,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.Delete);
                if (keys != null && keys.Any())
                {
                    if (!User.HasClaim("Permission", GXRoles.Admin) &&
                        !User.HasClaim("Permission", GXKeyManagementPolicies.Delete))
                    {
                        throw new UnauthorizedAccessException();
                    }
                    await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                    GXSelectArgs arg = GXSelectArgs.Select<GXKeyManagement>(a => a.Id, q => keys.Contains(q.Id));
                    List<GXKeyManagement> list = await connection.SelectAsync<GXKeyManagement>(arg);
                    DateTime now = DateTime.Now;
                    Dictionary<GXKeyManagement, List<string>> updates = [];

                    foreach (var entity in list)
                    {
                        updates[entity] = await GetNotifiedUsers(entity.Id, NotificationAction.Remove, connection);
                    }

                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                    foreach (GXKeyManagement key in list)
                    {
                        await GXLogStore.DeleteOwnedAsync(connection, transaction, "key-management", [key.Id], cancellationToken);
                        key.Removed = now;

                        if (delete && (await connection.SelectAsync<GXKeyManagementKey>(transaction,
                            GXSelectArgs.Select<GXKeyManagementKey>(k => k.Id, k => k.KeyManagement == key), cancellationToken)).Count == 0)
                        {
                            await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXKeyManagement>(key.Id), cancellationToken);
                        }
                        else
                        {
                            connection.Update(transaction, GXUpdateArgs.Update(key, q => q.Removed!));
                        }

                    }
                    await transaction.CommitAsync(cancellationToken);
                    foreach (var it in updates)
                    {
                        var users = it.Value;
                        if (users == null)
                        {
                            break;
                        }
                        GXKeyManagement tmp = new() { Id = it.Key.Id };
                        _notificationCache.Add(users, new GXTarget(GXKeyManagementPolicies.Delete, tmp.Id));
                    }

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
                        await DeleteAsync(keys, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXKeyManagement>> ListAsync(
            ListKeyManagements? request,
            ListKeyManagementsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User.HasClaim("Permission", GXRoles.Admin))
            {
                //Admin can see all the keys.
                arg = GXSelectArgs.SelectAll<GXKeyManagement>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetKeyManagementsByUser(s => "*", userId, null);
            }
            bool selectItemsBySystemTitle = false;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Device != null && request.Filter.Device.Id != Guid.Empty)
                {
                    //If user want to filter devices by ID.
                    Guid id = request.Filter.Device.Id;
                    arg.Joins.AddInnerJoin<GXKeyManagement, GXDevice>(j => j.Device!, j => j.Id);
                    arg.Where.And<GXDevice>(w => w.Id == id);
                    request.Filter.Device = null;
                }
                if (request.Filter.SystemTitle?.Length > 0)
                {
                    selectItemsBySystemTitle = true;
                    //Return key management items where system title is set ot it's null.
                    var st = request.Filter.SystemTitle;
                    arg.Where.And<GXKeyManagement>(w => w.SystemTitle == st || w.SystemTitle == null);
                    request.Filter.SystemTitle = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXKeyManagement>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXKeyManagement>(w => request.Included.Contains(w.Id));
                }
            }
            if (request?.Select != null && request.Select.Contains(TargetType.Device))
            {
                //Get device basic information.
                arg.Joins.AddLeftJoin<GXKeyManagement, GXDevice>(j => j.Device!, j => j.Id);
                arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
                arg.Columns.Exclude<GXDevice>(e => e.Keys!);
            }

            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXKeyManagement>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXKeyManagement>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXKeyManagement>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXKeyManagement>(q => q.CreationTime!);
                arg.Descending = true;
            }
            GXKeyManagement[] list = [.. (await connection.SelectAsync<GXKeyManagement>(arg))];

            //Ignore default key management items if user has ask
            //key management by system title and it's set.
            if (selectItemsBySystemTitle)
            {
                var tmp = list.Where(w => w.SystemTitle?.Length > 0).ToArray();
                if (tmp != null && tmp.Length > 0)
                {
                    list = tmp;
                }
            }

            if (request?.Select != null && (request.Select.Contains("KeyManagementKey") || request.Select.Contains("key-management-key")))
            {
                foreach (GXKeyManagement it in list)
                {
                    arg = GXSelectArgs.SelectAll<GXKeyManagementKey>(w => w.KeyManagement == it);
                    it.Keys = [.. (await connection.SelectAsync<GXKeyManagementKey>(arg))];
                    //Decrypt keys.
                    foreach (var key in it.Keys)
                    {
                        if (key.Key?.Length > 0)
                        {
                            key.Key = null;
                        }
                    }
                }
            }
            if (request?.Select != null && request.Select.Contains(TargetType.Object))
            {
                foreach (GXKeyManagement it in list)
                {
                    if (it.Device != null)
                    {
                        //Get objects.
                        GXDevice dev = it.Device;
                        arg = GXSelectArgs.SelectAll<GXObject>(w => w.Device == dev && w.Removed == null);
                        arg.Columns.Exclude<GXObject>(e => new
                        {
                            e.CreationTime,
                            e.Updated,
                            e.Removed
                        });
                        if (request?.Select != null && request.Select.Contains(TargetType.ObjectTemplate))
                        {
                            arg.Columns.Add<GXObjectTemplate>();
                            arg.Columns.Exclude<GXObjectTemplate>(e => new
                            {
                                e.CreationTime,
                                e.Updated,
                                e.Removed
                            });
                            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template!, j => j.Id);
                            arg.Where.And<GXObjectTemplate>(w => w.Removed == null);
                        }
                        if (request?.Select != null && request.Select.Contains(TargetType.Attribute))
                        {
                            arg.Columns.Add<GXAttribute>();
                            arg.Columns.Exclude<GXAttribute>(e => new
                            {
                                e.Object,
                                e.CreationTime,
                                e.Updated,
                                e.Removed
                            });
                            arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
                            arg.Where.And<GXAttribute>(w => w.Removed == null);
                            if (request?.Select != null && request.Select.Contains("AttributeTemplate"))
                            {
                                arg.Columns.Add<GXAttributeTemplate>();
                                arg.Columns.Exclude<GXAttributeTemplate>(e => new
                                {
                                    e.ObjectTemplate,
                                    e.CreationTime,
                                    e.Updated,
                                    e.Removed
                                });
                                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template!, j => j.Id);
                                arg.Where.And<GXAttributeTemplate>(w => w.Removed == null);
                            }
                        }
                        it.Device.Objects = [.. (await connection.SelectAsync<GXObject>(arg))];
                    }
                }
            }

            if (response != null)
            {
                response.KeyManagements = list;
                if (response.Count == 0)
                {
                    response.Count = list.Length;
                }
            }
            return list;
        }

        /// <inheritdoc />
        public async Task<GXKeyManagement> ReadAsync(Guid id, Expression<Func<GXKeyManagement, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.View);
                GXSelectArgs arg;
                if (User.HasClaim("Permission", GXRoles.Admin))
                {
                    //Admin can see all the keys.
                    arg = GXSelectArgs.SelectAll<GXKeyManagement>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetKeyManagementsByUser(s => "*", userId, id);
                }
                arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
                arg.Columns.Exclude<GXDevice>(s => s.Keys!);
                arg.Joins.AddLeftJoin<GXKeyManagement, GXDevice>(x => x.Device!, y => y.Id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXKeyManagement? key = await connection.SingleOrDefaultAsync<GXKeyManagement>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                //Get key management groups with own query.
                arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(w => w.Removed == null);
                arg.Joins.AddLeftJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(j => j.Id, j => j.KeyManagementGroupId);
                arg.Where.And<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == key.Id);
                key.KeyManagementGroups = await connection.SelectAsync<GXKeyManagementGroup>(arg);

                //Get key management keys with own query.
                arg = GXSelectArgs.SelectAll<GXKeyManagementKey>(w => w.KeyManagement == key && w.Removed == null);
                key.Keys = await connection.SelectAsync<GXKeyManagementKey>(arg);
                if (key.Keys != null)
                {
                    //Decrypt keys.
                    foreach (var it in key.Keys)
                    {
                        if (it.Key?.Length > 0)
                        {
                            it.Key = null;
                        }
                    }
                }
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXKeyManagement, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXKeyManagement>(w => w.Id == id);
                key.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                if (columns == null || ServerHelpers.Contains(columns, nameof(GXKeyManagement.Properties)))
                {
                    key.Properties = await GXPropertyStores.KeyManagement.ListAsync(connection, null, key, cancellationToken);
                }
                return key;

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
            IEnumerable<GXKeyManagement> keys,
            Expression<Func<GXKeyManagement, object>>? columns, CancellationToken cancellationToken = default)
        {
            keys = keys.ToArray();
            return await GXHistoryMutation.RunAsync<GXKeyManagement, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, keys,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXKeyManagementPolicies.Add, GXKeyManagementPolicies.Edit);
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                foreach (GXKeyManagement key in keys)
                {
                    if (key.Device != null && key.Device.Id == Guid.Empty)
                    {
                        key.Device = null;
                    }
                    if (key.SystemTitle?.Length is not > 0)
                    {
                        key.SystemTitle = null;
                    }
                    else
                    {
                        if (key.SystemTitle.Length != 8)
                        {
                            throw new ArgumentException(string.Format("Invalid system title '{0}'.", key.SystemTitle));
                        }
                    }
                    //Verify key management key system title if it's given.
                    //If system title is not given key management is used as a default value.
                    if (key.SystemTitle?.Length > 0 && key.SystemTitle.Length != 8)
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (key.KeyManagementGroups == null || key.KeyManagementGroups.Count == 0)
                    {
                        //Get default key groups.
                        ListKeyManagementGroups request = new()
                        {
                            Filter = new GXKeyManagementGroup() { Default = true }
                        };
                        key.KeyManagementGroups = [.. await _deviceKeyGroupRepository.ListAsync(request, null, CancellationToken.None)];
                        //It's OK if key management doesn't belong to any group.
                    }
                    var added = key.Id == Guid.Empty;
                    List<GXKeyManagementGroup>? existingGroups = null;
                    if (!added)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var groupsRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementGroupRepository>();
                        existingGroups = await groupsRepository.GetJoinedKeyManagementGroups(key.Id);
                    }
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                    List<string> notifiedUsers;
                    if (added)
                    {
                        key.CreationTime = now;
                        key.Creator = new GXUser()
                        {
                            Id = ServerHelpers.GetUserId(User)
                        };
                        GXInsertArgs args = GXInsertArgs.Insert(key);
                        args.Exclude<GXKeyManagement>(q => new { q.Updated, q.KeyManagementGroups, q.ConcurrencyStamp, q.Keys, q.Properties });
                        connection.Insert(transaction, args);
                        AddKeyManagementToKeyManagementGroups(connection, key.Id, key.KeyManagementGroups, transaction);
                        await AddKeyManagementKeysToKeyManagement(connection, key, key.Keys, transaction);
                        if (key.Properties != null)
                        {
                            await GXPropertyStores.KeyManagement.ReplaceAsync(connection, transaction, key, key.Properties, cancellationToken);
                        }
                        notifiedUsers = await GetNotifiedUsers(key.Id, NotificationAction.Add, connection, transaction);

                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXKeyManagement>(q => q.ConcurrencyStamp!, where => where.Id == key.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != key.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }

                        var routing = await connection.SingleOrDefaultAsync<GXKeyManagement>(transaction, GXSelectArgs.SelectAll<GXKeyManagement>(g => g.Id == key.Id), cancellationToken);
                        if (routing != null && (routing.ProviderId != key.ProviderId || routing.ProviderConnectionId != key.ProviderConnectionId))
                        {
                            throw new InvalidOperationException("Create a new key management to change provider routing.");
                        }

                        key.Updated = now;
                        key.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(key, columns);
                        args.Exclude<GXKeyManagement>(q => new { q.CreationTime, q.KeyManagementGroups, q.Keys, q.Properties });
                        if (!User.HasClaim("Permission", GXRoles.Admin) || key.Creator == null || string.IsNullOrEmpty(key.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXKeyManagement>(q => q.Creator!);
                        }

                        connection.Update(transaction, args);
                        //Map key groups to key.
                        var groups = existingGroups!;

                        {
                            var comparer = new UniqueComparer<GXKeyManagementGroup, Guid>();
                            List<GXKeyManagementGroup> removedKeyManagementGroups = [.. groups.Except(key.KeyManagementGroups, comparer)];
                            List<GXKeyManagementGroup> addedKeyManagementGroups = [.. key.KeyManagementGroups.Except(groups, comparer)];
                            if (removedKeyManagementGroups.Count != 0)
                            {
                                RemoveKeyManagementsFromKeyManagementGroup(connection, key.Id, removedKeyManagementGroups, transaction);
                            }

                            if (addedKeyManagementGroups.Count != 0)
                            {
                                AddKeyManagementToKeyManagementGroups(connection, key.Id, addedKeyManagementGroups, transaction);
                            }
                        }

                        var existing = await connection.SelectAsync<GXKeyManagementKey>(transaction, GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.KeyManagement == key), cancellationToken);
                        if (key.Keys != null)
                        {
                            foreach (var stored in existing.Where(k => k.Removed == null))
                            {
                                var incoming = key.Keys.SingleOrDefault(k => k.Id == stored.Id) ?? throw new InvalidOperationException("Use lifecycle operations; keys cannot be removed through a group save.");
                                GXKeyUpdateGuard.EnsureUnchanged(stored, incoming);
                            }

                            if (key.Keys.Any(k => k.Id != Guid.Empty && !existing.Any(e => e.Id == k.Id)))
                            {
                                throw new InvalidOperationException("Key identities cannot be moved between groups.");
                            }

                            await AddKeyManagementKeysToKeyManagement(connection, key, [.. key.Keys.Where(k => k.Id == Guid.Empty)], transaction);
                        }

                        if (key.Properties != null)
                        {
                            await GXPropertyStores.KeyManagement.ReplaceAsync(connection, transaction, key, key.Properties, cancellationToken);
                        }
                        notifiedUsers = await GetNotifiedUsers(key.Id, NotificationAction.Edit, connection, transaction);

                    }
                    await transaction.CommitAsync(cancellationToken);
                    _notificationCache.Add(notifiedUsers, new GXTarget(added ? GXKeyManagementPolicies.Add : GXKeyManagementPolicies.Edit, key.Id, key.Name) { ConcurrencyStamp = key.ConcurrencyStamp });
                }
                List<GXLog> logs = [];
                foreach (var it in keys)
                {
                    logs.Add(new GXLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Creator = it.Id,
                        Message = it.CreationTime == now ? Properties.Resources.KeyManagementInstalled :
                        Properties.Resources.KeyManagementUpdated
                    });
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    var deviceKeyLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("key-management");
                    await deviceKeyLogRepository.AddAsync("Update", logs, cancellationToken);
                }
                return [.. keys.Select(s => s.Id)];

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
                        return await UpsertAsync(keys, columns, linkedCts.Token);
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

        /// <summary>
        /// Map key group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="keyId">KeyManagement ID.</param>
        /// <param name="groups">Group IDs of the key groups where the key is added.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void AddKeyManagementToKeyManagementGroups(GXDbConnection connection,
            Guid keyId, IEnumerable<GXKeyManagementGroup> groups, System.Data.IDbTransaction transaction)
        {
            DateTime now = DateTime.Now;
            List<GXKeyManagementGroupKeyManagement> list = [];
            foreach (GXKeyManagementGroup it in groups)
            {
                list.Add(new GXKeyManagementGroupKeyManagement()
                {
                    KeyManagementId = keyId,
                    KeyManagementGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key group and key.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="keyId">KeyManagement ID.</param>
        /// <param name="groups">Group IDs of the key groups where the key is removed.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void RemoveKeyManagementsFromKeyManagementGroup(GXDbConnection connection,
            Guid keyId, IEnumerable<GXKeyManagementGroup> groups, System.Data.IDbTransaction transaction)
        {
            foreach (var it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == keyId && w.KeyManagementGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map keys to key management.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="keyManagement">Key management.</param>
        /// <param name="keys">Added keys.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private async Task AddKeyManagementKeysToKeyManagement(GXDbConnection connection,
            GXKeyManagement keyManagement, IEnumerable<GXKeyManagementKey>? keys, System.Data.IDbTransaction transaction)
        {
            if (keys != null && keys.Any())
            {
                DateTime now = DateTime.Now;
                foreach (GXKeyManagementKey it in keys)
                {
                    if (it.Id != Guid.Empty)
                    {
                        throw new InvalidOperationException("Registration requires a new key identity.");
                    }

                    it.KeyManagement = keyManagement;
                    if (it.State == null)
                    {
                        GXKeyPolicy.InitializeLegacy(it);
                    }

                    it.State = KeyState.PreActive;
                    it.ActivationDate = it.DeactivationDate = it.CompromiseDate = it.DestroyDate = null;
                    if (GXDeviceKeyImportScope.IsInstalled(it))
                    {
                        // These values describe the credentials currently installed on the meter.
                        // Ordinary imports and rotations still start in PreActive.
                        GXKeyPolicy.ValidateTransition(it.State.Value, KeyState.Active);
                        it.State = KeyState.Active;
                        it.ActivationDate = DateTimeOffset.UtcNow;
                    }
                    GXKeyPolicy.ValidateRegistration(it);
                    string? provider = it.ProviderId ?? keyManagement.ProviderId;
                    if (string.IsNullOrEmpty(provider) || provider == "local")
                    {
                        GXKeyPolicy.ValidateMaterial(it);
                        it.Key = await _cryproService.EncryptAsync(it.Key!);
                    }

                    it.Id = Guid.NewGuid();
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    it.CreationTime = now;
                    it.KeyManagement = keyManagement;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXKeyManagementKey>(e => new { e.Updated, e.Removed });
                    connection.Insert(transaction, args);
                }
            }
        }

        /// <summary>
        /// Remove map between key management group and key management.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="keys">Removed keys</param>
        private static void RemoveKeyManagementKeysFromKeyManagement(GXDbConnection connection,
            IEnumerable<GXKeyManagementKey> keys)
        {
            DateTime now = DateTime.Now;
            foreach (GXKeyManagementKey it in keys)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(keys, c => c.Removed!);
            connection.UpdateAsync(args);
        }
    }
}
