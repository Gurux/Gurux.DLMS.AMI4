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
using System.Linq.Expressions;
using System.Data;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class RoleRepository : IRoleRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoleRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            GXLanguageService languageService,
            ILocalizationRepository localizationRepository,
            NotificationCache notificationCache)
        {
            _ = languageService;
            _ = localizationRepository;
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

        private async Task<List<string>> GetAffectedUsersAsync(GXDbConnection connection,
            IEnumerable<string> roleIds, CancellationToken cancellationToken = default)
        {
            string[] ids = [.. roleIds.Distinct()];
            var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            if (ids.Length == 0)
            {
                return [.. users.Distinct()];
            }

            var members = await connection.SelectAsync<GXUserRole>(
                            GXSelectArgs.Select<GXUserRole>(r => r.UserId!, r => ids.Contains(r.RoleId!)), cancellationToken);
            return [.. users.Concat(members.Where(r => r.UserId != null).Select(r => r.UserId!)).Distinct()];
        }
        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<string> rolers, CancellationToken cancellationToken = default)
        {
            rolers = rolers.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXRole, string>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, rolers,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.Delete);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                await LockRoleDefaultsAsync(connection, transaction, cancellationToken);
                var ids = rolers.Distinct().ToArray();
                var active = await connection.SelectAsync<GXRole>(transaction,
                    GXSelectArgs.SelectAll<GXRole>(r => r.Removed == null), cancellationToken);
                var list = active.Where(r => ids.Contains(r.Id)).ToList();
                if (list.Any(r => r.Default == true))
                {
                    throw new ArgumentException("The default role cannot be removed. Select another default role first.");
                }
                if (active.Count - list.Count < 1)
                {
                    throw new ArgumentException("At least one active role must remain.");
                }
                DateTime now = DateTime.Now;
                foreach (GXRole it in list)
                {
                    it.Removed = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    connection.Update(transaction, GXUpdateArgs.Update(it, q => new { q.Removed, q.ConcurrencyStamp }));
                }
                transaction.Commit();
                if (!GXHistoryScope.Defer(Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll))
                {
                    Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll();
                }
                var users = await GetAffectedUsersAsync(connection, list.Select(r => r.Id), cancellationToken);
                _notificationCache.Add(users, new GXTarget(GXRolePolicies.Delete));
                _notificationCache.Add(users, new GXTarget(GXRolePolicies.Edit));
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
                        await DeleteAsync(rolers, linkedCts.Token);
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
        public async Task<IEnumerable<GXRole>> ListAsync(
            ListRoles? request,
            ListRolesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.View);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Removed == null);
            arg.Columns.Add<GXPermission>();
            arg.Joins.AddLeftJoin<GXRole, GXRolePermission>(j => j.Id, j => j.RoleId!);
            arg.Joins.AddLeftJoin<GXRolePermission, GXPermission>(j => j.PermissionId!, j => j.Id);
            arg.OrderBy.Add<GXRole>(o => o.Name!);
            if (!isAdmin)
            {
                //Return roles that user can access.
                string? userId = ServerHelpers.GetUserId(User);
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId!);
                arg.Where.And<GXUserRole>(where => where.UserId == userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXRole>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXRole>(q => q.Id);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXRole>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXRole>(q => GXSql.Count(1));
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
            GXRole[] roles = [.. (await connection.SelectAsync<GXRole>(arg))];
            if (response != null)
            {
                response.Roles = roles;
                if (response.Count == 0)
                {
                    response.Count = roles.Length;
                }
            }
            return roles;
        }

        /// <inheritdoc />
        public async Task<GXRole> ReadAsync(string id, Expression<Func<GXRole, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.View);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Id == id && w.Removed == null);
                arg.Columns.Add<GXPermission>();
                arg.Joins.AddLeftJoin<GXRole, GXRolePermission>(j => j.Id, j => j.RoleId!);
                arg.Joins.AddLeftJoin<GXRolePermission, GXPermission>(j => j.PermissionId!, j => j.Id);
                arg.OrderBy.Add<GXRole>(o => o.Name!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXRole? role = await connection.SingleOrDefaultAsync<GXRole>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                return role;

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
        public async Task<GXRole> ReadByNameAsync(string name)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.View);
            name = name.ToUpperInvariant();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.NormalizedName == name && w.Removed == null);
            arg.Columns.Add<GXPermission>();
            arg.Joins.AddLeftJoin<GXRole, GXRolePermission>(j => j.Id, j => j.RoleId!);
            arg.Joins.AddLeftJoin<GXRolePermission, GXPermission>(j => j.PermissionId!, j => j.Id);
            arg.OrderBy.Add<GXRole>(o => o.Name!);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXRole? role = await connection.SingleOrDefaultAsync<GXRole>(arg) ?? throw new GXAmiForbiddenException(name);
            return role;
        }

        private static async Task<List<GXPermission>> GetPermissionsByRoleIdAsync(GXDbConnection connection, IDbTransaction transaction, string roleId, CancellationToken cancellationToken = default)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPermission>();
            arg.Joins.AddInnerJoin<GXPermission, GXRolePermission>(j => j.Id, j => j.PermissionId!);
            arg.Where.And<GXRolePermission>(w => w.RoleId == roleId);
            arg.Distinct = true;
            return await connection.SelectAsync<GXPermission>(transaction, arg, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> UpsertAsync(IEnumerable<GXRole> roles, CancellationToken cancellationToken = default)
        {
            roles = roles.ToArray();
            return await GXHistoryMutation.RunAsync<GXRole, IEnumerable<string>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, roles,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(roles);
            roles = roles.ToArray();
            if (roles.Any(r => string.IsNullOrEmpty(r.Id)))
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.Add);
            }
            if (roles.Any(r => !string.IsNullOrEmpty(r.Id)))
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.Edit);
            }
            try
            {
                DateTime now = DateTime.Now;
                List<string> list = [];
                List<GXRole> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                await LockRoleDefaultsAsync(connection, transaction, cancellationToken);
                foreach (GXRole role in roles)
                {
                    if (string.IsNullOrWhiteSpace(role.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    role.Name = role.Name.Trim();
                    role.NormalizedName = role.Name.ToUpperInvariant();
                    var arg = GXSelectArgs.SelectAll<GXRole>(w => w.NormalizedName == role.NormalizedName);
                    GXRole? tmp = await connection.SingleOrDefaultAsync<GXRole>(transaction, arg, cancellationToken);
                    if (tmp != null && tmp.Id != role.Id)
                    {
                        throw new ArgumentException("A role with this name already exists.");
                    }
                    if (string.IsNullOrEmpty(role.Id))
                    {
                        role.SystemDefined = false;
                        role.Removed = null;
                        GXInsertArgs args = GXInsertArgs.Insert(role);
                        args.Exclude<GXRole>(e => e.Permissions!);
                        connection.Insert(transaction, args);
                        list.Add(role.Id);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.SelectAll<GXRole>(where => where.Id == role.Id);
                        GXRole? old = connection.SingleOrDefault<GXRole>(transaction, m);
                        if (old == null || !string.IsNullOrEmpty(old.ConcurrencyStamp) && old.ConcurrencyStamp != role.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        if (old.Removed != null)
                        {
                            throw new ArgumentException("Removed roles cannot be edited.");
                        }
                        if (old.SystemDefined == true && old.Name != role.Name)
                        {
                            throw new ArgumentException("System role names cannot be changed.");
                        }
                        if (old.Name != role.Name)
                        {
                            var references = GXSelectArgs.Select<GXPolicyRequirement>(r => GXSql.Count(1),
                                r => r.Type == Gurux.DLMS.AMI.Shared.Enums.PolicyRequirementType.Role && r.Value == old.Name);
                            if (await connection.SingleOrDefaultAsync<long>(transaction, references, cancellationToken) != 0)
                            {
                                throw new ArgumentException("This role is referenced by an access policy. Update the policy requirements before renaming the role.");
                            }
                        }
                        role.Default ??= old.Default;
                        //If content of the role has changed.
                        if ((old.Default != role.Default && role.Default != null) ||
                            old.Name != role.Name ||
                            old.Description != role.Description)
                        {
                            role.ConcurrencyStamp = Guid.NewGuid().ToString();
                            GXUpdateArgs args = GXUpdateArgs.Update(role, u => new { u.Name, u.NormalizedName, u.Description, u.Default, u.ConcurrencyStamp });
                            connection.Update(transaction, args);
                            updates.Add(role);
                        }
                    }
                    if (role.Permissions != null)
                    {
                        var existing = await GetPermissionsByRoleIdAsync(connection, transaction, role.Id, cancellationToken);
                        var desired = new List<GXPermission>();
                        foreach (var permission in role.Permissions)
                        {
                            GXPermission? catalog = null;
                            if (permission.Id != Guid.Empty)
                            {
                                catalog = await connection.SingleOrDefaultAsync<GXPermission>(transaction,
                                GXSelectArgs.SelectAll<GXPermission>(p => p.Id == permission.Id));
                            }

                            if (catalog == null && !string.IsNullOrWhiteSpace(permission.Name))
                            {
                                string name = permission.Name.Trim();
                                catalog = await connection.SingleOrDefaultAsync<GXPermission>(transaction,
                                    GXSelectArgs.SelectAll<GXPermission>(p => p.Name == name));
                            }
                            if (catalog == null)
                            {
                                throw new ArgumentException($"Unknown permission '{permission.Name ?? permission.Id.ToString()}'.");
                            }

                            if (!desired.Any(p => p.Id == catalog.Id))
                            {
                                desired.Add(catalog);
                            }
                        }
                        foreach (var permission in existing.Where(p => !desired.Any(d => d.Id == p.Id)))
                        {
                            connection.Delete(transaction, GXDeleteArgs.Delete<GXRolePermission>(p => p.RoleId == role.Id && p.PermissionId == permission.Id));
                        }

                        foreach (var permission in desired.Where(p => !existing.Any(e => e.Id == p.Id)))
                        {
                            connection.Insert(transaction, GXInsertArgs.Insert(new GXRolePermission { RoleId = role.Id, PermissionId = permission.Id }));
                        }

                        if (!existing.Select(p => p.Id).ToHashSet().SetEquals(desired.Select(p => p.Id)))
                        {
                            updates.Add(role);
                        }

                        role.Permissions = desired;
                    }
                }
                await ValidateDefaultRoleAsync(connection, transaction, cancellationToken);
                transaction.Commit();
                if (!GXHistoryScope.Defer(Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll))
                {
                    Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll();
                }
                if (updates.Count != 0)
                {
                    var users = await GetAffectedUsersAsync(connection, updates.Select(r => r.Id), cancellationToken);
                    _notificationCache.Add(users, new GXTarget(GXRolePolicies.Edit));
                }
                return [.. list];

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
                        return await UpsertAsync(roles, linkedCts.Token);
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

        internal static async Task EnsureRoleDefaultsAsync(GXDbConnection connection, IDbTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            await LockRoleDefaultsAsync(connection, transaction, cancellationToken);
            var existing = await connection.SelectAsync<GXRole>(transaction, GXSelectArgs.SelectAll<GXRole>(), cancellationToken);
            foreach (string name in GXRoles.SystemRoles)
            {
                string normalized = name.ToUpperInvariant();
                if (existing.Any(r => string.Equals(r.NormalizedName, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var role = GXRoleDefaults.Create(name);
                role.Id = Guid.NewGuid().ToString();
                role.ConcurrencyStamp = Guid.NewGuid().ToString();
                if (role.Default == true && existing.Any(r => r.Default == true && r.Removed == null))
                {
                    // Preserve an administrator's existing choice when adding a missing user role.
                    role.Default = false;
                }
                var insert = GXInsertArgs.Insert(role);
                insert.Exclude<GXRole>(r => r.Permissions!);
                await connection.InsertAsync(transaction, insert, cancellationToken);
                await ApplyRolePermissionsAsync(connection, transaction, role, cancellationToken);
                existing.Add(role);
            }
            await NormalizeConflictingDefaultsAsync(connection, transaction, existing, cancellationToken);
            await ValidateDefaultRoleAsync(connection, transaction, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAllAsync<GXRole>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.Add);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await EnsureRoleDefaultsAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (!GXHistoryScope.Defer(GXUserPrincipalCache.InvalidateAll))
            {
                GXUserPrincipalCache.InvalidateAll();
            }
            var users = await GetAffectedUsersAsync(connection, [], cancellationToken);
            _notificationCache.Add(users, new GXTarget(GXRolePolicies.Add));
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ResetToDefaultsAsync(IEnumerable<string> roleIds, CancellationToken cancellationToken = default)
        {
            roleIds = roleIds.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXRole, string>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, roleIds,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXRolePolicies.Edit);
            ArgumentNullException.ThrowIfNull(roleIds);
            var ids = roleIds.Distinct().ToArray();
            if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Select at least one built-in role.", nameof(roleIds));
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await LockRoleDefaultsAsync(connection, transaction, cancellationToken);
            var selected = await connection.SelectAsync<GXRole>(transaction,
                GXSelectArgs.SelectAll<GXRole>(r => ids.Contains(r.Id)), cancellationToken);
            var names = GXRoles.SystemRoles.ToDictionary(n => n.ToUpperInvariant(), StringComparer.Ordinal);
            if (selected.Count != ids.Length || selected.Any(r => r.NormalizedName == null || !names.ContainsKey(r.NormalizedName)))
            {
                throw new ArgumentException("Only known built-in roles can be reset.", nameof(roleIds));
            }
            foreach (var role in selected)
            {
                var defaults = GXRoleDefaults.Create(names[role.NormalizedName!]);
                role.Name = defaults.Name;
                role.SystemDefined = defaults.SystemDefined;
                role.Default = defaults.Default;
                role.Description = defaults.Description;
                role.Removed = null;
                role.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(role, r => new {
                    r.Name, r.SystemDefined, r.Default, r.Description, r.Removed, r.ConcurrencyStamp }), cancellationToken);
                await ApplyRolePermissionsAsync(connection, transaction, role, cancellationToken);
            }
            await ValidateDefaultRoleAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (!GXHistoryScope.Defer(GXUserPrincipalCache.InvalidateAll))
            {
                GXUserPrincipalCache.InvalidateAll();
            }
            var users = await GetAffectedUsersAsync(connection, ids, cancellationToken);
            _notificationCache.Add(users, new GXTarget(GXRolePolicies.Edit));
        }, cancellationToken);
        }

        private static async Task ApplyRolePermissionsAsync(GXDbConnection connection, IDbTransaction transaction,
            GXRole role, CancellationToken cancellationToken)
        {
            var links = new List<GXRolePermission>();
            foreach (var name in GXRoleDefaults.PermissionNames(role.Name!))
            {
                var permission = await connection.SingleOrDefaultAsync<GXPermission>(transaction,
                    GXSelectArgs.Select<GXPermission>(p => p.Id, p => p.Name == name), cancellationToken);
                if (permission == null)
                {
                    permission = new GXPermission(name) { Id = Guid.NewGuid(), SystemDefined = true, Default = false };
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(permission), cancellationToken);
                }
                links.Add(new GXRolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXRolePermission>(p => p.RoleId == role.Id), cancellationToken);
            if (links.Count != 0)
            {
                await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(links), cancellationToken);
            }
        }

        private static async Task ValidateDefaultRoleAsync(GXDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var query = GXSelectArgs.Select<GXRole>(r => GXSql.Count(1), r => r.Default == true && r.Removed == null);
            var count = await connection.SingleOrDefaultAsync<long>(transaction, query, cancellationToken);
            if (count == 0)
            {
                throw new ArgumentException("At least one active role must be the default role.");
            }
            if (count > 1)
            {
                throw new ArgumentException("Only one active role can be the default role.");
            }
        }

        private static async Task NormalizeConflictingDefaultsAsync(GXDbConnection connection, IDbTransaction transaction,
            List<GXRole> roles, CancellationToken cancellationToken)
        {
            if (roles.Count(r => r.Default == true && r.Removed == null) == 1)
            {
                return;
            }
            var user = roles.FirstOrDefault(r => r.Removed == null &&
                string.Equals(r.NormalizedName, GXRoles.Authenticated.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                // A fallback administrator role would grant administrative access to new registrations.
                throw new ArgumentException("Select exactly one default role. The built-in user role is unavailable for automatic repair.");
            }
            foreach (var role in roles.Where(r => r.Removed == null && (r.Default == true || r.Id == user?.Id)))
            {
                bool desired = role.Id == user?.Id;
                if (role.Default != desired)
                {
                    // Repair legacy multiple-default configurations without changing grants or reviving removed roles.
                    role.Default = desired;
                    role.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(transaction,
                        GXUpdateArgs.Update(role, r => new { r.Default, r.ConcurrencyStamp }), cancellationToken);
                }
            }
        }

        private static async Task LockRoleDefaultsAsync(GXDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            if (connection.DatabaseType == Gurux.Service.Orm.Common.Enums.DatabaseType.MSSQL)
            {
                // Serialize the complete role set, including missing rows, until commit or rollback.
                // Default-role writes are rare, and this also protects concurrent server instances.
                await using var command = ((System.Data.Common.DbConnection)transaction.Connection!).CreateCommand();
                command.Transaction = (System.Data.Common.DbTransaction)transaction;
                command.CommandTimeout = connection.CommandTimeout > 0 ? connection.CommandTimeout : 30;
                command.CommandText = "SELECT COUNT_BIG(*) FROM [GXRole] WITH (UPDLOCK, HOLDLOCK)";
                await command.ExecuteScalarAsync(cancellationToken);
            }
        }
    }
}
