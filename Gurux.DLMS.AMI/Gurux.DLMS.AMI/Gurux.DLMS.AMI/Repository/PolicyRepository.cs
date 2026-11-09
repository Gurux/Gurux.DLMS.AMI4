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
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public partial class PolicyRepository : IPolicyRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache _notificationCache;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PolicyRepository(IAuthorizationService authorizationService,
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
            _authorizationService = authorizationService;
            _notificationCache = notificationCache;
        }

        private static async Task<List<string>> GetNotifiedUsers(Guid id, NotificationAction action, CancellationToken cancellationToken = default)
        {
            _ = id;
            _ = action;
            _ = cancellationToken;
            return [];
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> policies, CancellationToken cancellationToken = default)
        {
            policies = policies.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXPolicy, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, policies,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await GXPolicyStore.DeleteAsync(connection, policies, cancellationToken);
            await NotifyAuthorizationChangedAsync(GXPolicyPolicies.Delete, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXPolicy>> ListAsync(
            ListPolicys? request,
            ListPolicysResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            return await GXPolicyListQuery.ReadAsync(connection, request, response, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GXPolicy> ReadAsync(Guid id, Expression<Func<GXPolicy, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.View);
            try
            {
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXPolicy>(w => w.Id == id && w.Removed == null);
                arg.Columns.Add<GXPolicyRequirement>();
                arg.Joins.AddLeftJoin<GXPolicy, GXPolicyRequirement>(j => j.Id, j => j.Policy!);
                arg.OrderBy.Add<GXPolicy>(o => o.Name);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXPolicy? policy = await connection.SingleOrDefaultAsync<GXPolicy>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                return policy;

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
        public async Task<GXPolicy> ReadByNameAsync(string name)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.View);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            string normalizedName = name.ToUpperInvariant();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPolicy>(w => w.NormalizedName == normalizedName && w.Removed == null);
            arg.Columns.Add<GXPolicyRequirement>();
            arg.Joins.AddLeftJoin<GXPolicy, GXPolicyRequirement>(j => j.Id, j => j.Policy!);
            arg.OrderBy.Add<GXPolicy>(o => o.Name);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXPolicy? policy = await connection.SingleOrDefaultAsync<GXPolicy>(arg) ?? throw new GXAmiForbiddenException(name);
            return policy;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXPolicy> policys, CancellationToken cancellationToken = default)
        {
            policys = policys.ToArray();
            return await GXHistoryMutation.RunAsync<GXPolicy, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, policys,
        async () =>
        {
            var values = policys.ToArray();
            if (values.Any(p => p.Id == Guid.Empty))
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.Add);
            }

            if (values.Any(p => p.Id != Guid.Empty))
            {
                await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.Edit);
            }

            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var ids = await GXPolicyStore.SaveAsync(connection, values, cancellationToken);
            await NotifyAuthorizationChangedAsync(GXPolicyPolicies.Edit, cancellationToken);
            return ids;
        }, cancellationToken);
        }

        private async Task NotifyAuthorizationChangedAsync(string policy, CancellationToken cancellationToken)
        {
            // Policy requirements can affect any signed-in user. Publish only the invalidation marker.
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var users = await connection.SelectAsync<GXUser>(GXSelectArgs.Select<GXUser>(user => user.Id!), cancellationToken);
            _notificationCache.Add(users.Select(user => user.Id!), new GXTarget(policy));
        }

        internal static string[] GetSystemPolicyNames()
        {
            return [.. typeof(GXDevicePolicies).Assembly.GetTypes()
                .Where(t => t.Namespace == typeof(GXDevicePolicies).Namespace && t.Name.EndsWith("Policies", StringComparison.Ordinal))
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .Concat(typeof(Gurux.DLMS.AMI.Client.Shared.KeyManagement.GXKeyPermissions).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!)).Distinct(StringComparer.Ordinal)];
        }

        private static List<GXPolicy> Policies()
        {
            List<GXPolicy> policies =
            [
                GXUpdatesDefaults.CreatePolicy(),
                .. CreatePolicies(typeof(GXSystemLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXWorkflowLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXScheduleLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXScriptLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserErrorPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXModuleLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAgentLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDevicePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXSchedulePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXScheduleGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAgentPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXRawNotificationPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAgentGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXBlockPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXBlockGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXMenuPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXViewPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(Gurux.DLMS.AMI.Client.Shared.KeyManagement.GXKeyPermissions), GXRoles.Admin),
                .. CreatePolicies(typeof(GXMenuGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXComponentViewPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXComponentViewGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXConfigurationPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceTemplatePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceTemplateGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXLocalizationPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXModulePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXModuleGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserPropertiesPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDevicePropertiesPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAgentPropertiesPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXScriptPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXManufacturerPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXManufacturerGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXKeyManagementPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXKeyManagementGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXKeyManagementLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXScriptGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXTaskPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXTokenPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXTriggerPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXTriggerGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXWorkflowPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXWorkflowGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXWorkflowOperationPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceTracePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXDeviceActionPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserActionPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXValuePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXRolePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXObjectPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXObjectTemplatePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAttributeTemplatePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAttributePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXOptionPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXGatewayPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXGatewayGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXGatewayLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXSubtotalPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXSubtotalGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXReportPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXReportGroupPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXNotificationPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXNotificationTypePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAppearancePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXLocalizedResourcePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXAnnouncementPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXFavoritePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXLanguagePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXPerformancePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXUserStampPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXLogPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXLogTypePolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXPolicyPolicies), GXRoles.Admin),
                .. CreatePolicies(typeof(GXChangeHistoryPolicies), GXRoles.Admin),

                .. CreatePolicies(typeof(GXViewGroupPolicies), GXRoles.Admin),

            ];
            return policies;
        }

        /// <summary>
        /// Creates policies and their requirements from a static policy definition class.
        /// </summary>
        /// <param name="type">Type of the static class containing policy constants.</param>
        /// <param name="roles">Allowed roles.</param>
        /// <returns>Created policies.</returns>
        private static List<GXPolicy> CreatePolicies(Type type, params string[]? roles)
        {
            List<GXPolicy> policies = [];
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.FlattenHierarchy))
            {
                // Only const string fields.
                if (!field.IsLiteral ||
                    field.IsInitOnly ||
                    field.FieldType != typeof(string))
                {
                    continue;
                }

                string? name = field.GetRawConstantValue() as string;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string? description = field
                    .GetCustomAttribute<DescriptionAttribute>()?
                    .Description;

                GXPolicy policy = new(name)
                {
                    Description = description,
                    SystemDefined = true,
                    RequireAuthenticatedUser = true,
                };
                foreach (var role in roles ?? [])
                {
                    policy.Requirements!.Add(new GXPolicyRequirement
                    {
                        Policy = policy,
                        Type = PolicyRequirementType.Role,
                        Value = role
                    });
                }
                policy.Requirements!.Add(new GXPolicyRequirement
                {
                    Policy = policy,
                    Type = PolicyRequirementType.Permission,
                    Value = name
                });
                policies.Add(policy);
            }
            return policies;
        }

        internal static Task EnsureViewPoliciesAsync(GXDbConnection connection, System.Data.IDbTransaction transaction)
            => EnsurePolicyDefaultsAsync(connection, transaction);

        /// <summary>Add missing defaults during bootstrap.</summary>
        public static async Task InitializeDefaultPolicies(IDbConnectionFactory dbFactory, IGXAmiPolicyRegistry registry)
        {
            _ = registry;
            await using var connection = await dbFactory.GetHistoryConnectionAsync();
            bool oracle = connection.DatabaseType == Gurux.Service.Orm.Common.Enums.DatabaseType.Oracle;
            await using var transaction = await connection.BeginTransactionAsync(oracle
                ? System.Data.IsolationLevel.ReadCommitted : System.Data.IsolationLevel.Serializable);
            if (oracle)
            {
                //Oracle snapshot isolation can fail while a new catalog's indexes grow.
                //Lock the complete bootstrap set before reading to serialize all five tables instead.
                await using var command = connection.Connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = connection.CommandTimeout;
                command.CommandText = "LOCK TABLE GXROLE, GXROLEPERMISSION, GXPOLICY, GXPOLICYREQUIREMENT, GXPERMISSION IN EXCLUSIVE MODE";
                await command.ExecuteNonQueryAsync();
            }
            await RoleRepository.EnsureRoleDefaultsAsync(connection, transaction);
            await EnsurePolicyDefaultsAsync(connection, transaction);
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction);
            await transaction.CommitAsync();
            if (!GXHistoryScope.Defer(Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll))
            {
                Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll();
            }
        }

        internal static async Task EnsurePolicyDefaultsAsync(GXDbConnection connection,
            System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            foreach (var policy in Policies())
            {
                var normalized = policy.NormalizedName;
                if (await connection.SingleOrDefaultAsync<GXPolicy>(transaction,
                    GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.NormalizedName == normalized), cancellationToken) != null)
                {
                    continue;
                }
                policy.Id = Guid.NewGuid();
                var insert = GXInsertArgs.Insert(policy);
                insert.Exclude<GXPolicy>(p => p.Requirements!);
                await connection.InsertAsync(transaction, insert, cancellationToken);
                foreach (var requirement in policy.Requirements ?? [])
                {
                    requirement.Id = Guid.NewGuid();
                    requirement.Policy = policy;
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(requirement), cancellationToken);
                }
            }
        }

        internal static async Task EnsureNotificationSourcesAsync(GXDbConnection connection,
            System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var names = Policies().Select(p => p.Name!).Distinct(StringComparer.Ordinal).ToArray();
            foreach (var name in names.Concat(names.Select(n => n.Split('.')[0])).Distinct(StringComparer.Ordinal))
                {
                await GXNotificationSourceStorage.RegisterAsync(connection, transaction, name, null, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAllAsync<GXPolicy>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.Add);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await EnsurePolicyDefaultsAsync(connection, transaction, cancellationToken);
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (!GXHistoryScope.Defer(Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll))
            {
                Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll();
            }
            await NotifyAuthorizationChangedAsync(GXPolicyPolicies.Add, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ResetToDefaultsAsync(IEnumerable<Guid> policyIds, CancellationToken cancellationToken = default)
        {
            policyIds = policyIds.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXPolicy, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, policyIds,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXPolicyPolicies.Edit);
            ArgumentNullException.ThrowIfNull(policyIds);
            var ids = policyIds.Distinct().ToArray();
            if (ids.Length == 0 || ids.Contains(Guid.Empty))
            {
                throw new ArgumentException("Select at least one built-in policy.", nameof(policyIds));
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var selected = await connection.SelectAsync<GXPolicy>(transaction,
                GXSelectArgs.SelectAll<GXPolicy>(p => ids.Contains(p.Id)), cancellationToken);
            var defaults = Policies().ToDictionary(p => p.NormalizedName, StringComparer.Ordinal);
            if (selected.Count != ids.Length || selected.Any(p => !defaults.ContainsKey(p.NormalizedName)))
            {
                throw new ArgumentException("Only known built-in policies can be reset.", nameof(policyIds));
            }
            foreach (var old in selected)
            {
                var policy = defaults[old.NormalizedName];
                policy.Id = old.Id;
                policy.Removed = null;
                policy.Updated = DateTimeOffset.UtcNow;
                policy.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(policy, p => new {
                    p.Name, p.NormalizedName, p.DisplayName, p.Description, p.SystemDefined, p.RequireAuthenticatedUser,
                    p.RequirementOperator, p.Removed, p.Updated, p.ConcurrencyStamp }), cancellationToken);
                await connection.DeleteAsync(transaction,
                    GXDeleteArgs.Delete<GXPolicyRequirement>(r => r.Policy == old), cancellationToken);
                foreach (var requirement in policy.Requirements ?? [])
                {
                    requirement.Id = Guid.NewGuid();
                    requirement.Policy = policy;
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(requirement), cancellationToken);
                }
            }
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (!GXHistoryScope.Defer(Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll))
            {
                Gurux.DLMS.AMI.Server.Midlewares.GXUserPrincipalCache.InvalidateAll();
            }
            await NotifyAuthorizationChangedAsync(GXPolicyPolicies.Edit, cancellationToken);
        }, cancellationToken);
        }
        // Lifecycle operations share this repository's persistence rules and notification queue.
        internal static IModulePolicyRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModulePolicyRepository
        {
            public async Task<GXPolicy?> EnsureCaptureAsync(string name, CancellationToken token)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                if (!name.StartsWith(scope.ModuleId + ".", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("The policy must belong to this module.");
                }
                var normalized = name.ToUpperInvariant();
                await GXNotificationSourceStorage.RegisterAsync(scope.Connection, scope.Transaction, scope.ModuleId, scope.ModuleId, token);
                await GXNotificationSourceStorage.RegisterAsync(scope.Connection, scope.Transaction, name, scope.ModuleId, token);
                var policy = await scope.Connection.SingleOrDefaultAsync<GXPolicy>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXPolicy>(p => p.NormalizedName == normalized), token);
                if (policy?.Removed != null)
                {
                    return null;
                }
                if (policy != null)
                {
                    return policy;
                }
                policy = new GXPolicy(name)
                {
                    RequireAuthenticatedUser = true,
                    Requirements = [new GXPolicyRequirement { Type = PolicyRequirementType.Role, Value = "admin" }]
                };
                await GXPolicyStore.SaveAsync(scope.Connection, [policy], token, scope.Transaction);
                await GXModuleRepositoryNotifications.QueueAsync(scope, GXPolicyPolicies.Add, policy.Id.ToString(), token);
                return policy;
            }
        }
    }
}



