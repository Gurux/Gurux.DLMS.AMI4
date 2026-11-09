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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class AgentPropertiesRepository : IAgentPropertiesRepository
    {
        private readonly ClaimsPrincipal _user;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>Creates a repository scoped to the current authenticated user.</summary>
        public AgentPropertiesRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor, NotificationCache notificationCache,
            IAuthorizationService authorizationService)
        {
            _user = httpContextAccessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
            // Operation policies can be granted directly without assigning a role.
            if (_user.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException();
            }
            _dbFactory = dbFactory;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private GXSelectArgs Owners(Guid? id = null)
        {
            if (!_user.IsInRole(GXRoles.Admin))
            {
                var users = GXRepositoryUsers.Query(typeof(GXAgent), id);
                var userId = ServerHelpers.GetUserId(_user);
                users.Where.And<GXUser>(u => u.Id == userId);
                users.Columns.Clear();
                users.Columns.Add<GXUser>(u => GXSql.One);
                return users;
            }
            var query = GXSelectArgs.Select<GXAgent>(o => GXSql.One, o => o.Removed == null);
            if (id != null)
            {
                query.Where.And<GXAgent>(o => o.Id == id);
            }
            return query;
        }

        private GXSelectArgs PropertiesQuery()
        {
            var owners = Owners();
            var query = GXSelectArgs.SelectAll<GXAgentProperty>(p =>
                GXSql.Exists<GXAgentProperty, GXAgent>(p => p.Agent!, o => o.Id, owners));
            query.Joins.AddInnerJoin<GXAgentProperty, GXAgent>(p => p.Agent!, o => o.Id);
            query.Columns.Add<GXAgent>(o => o.Id);
            query.Joins.AddLeftJoin<GXAgentProperty, GXModule>(p => p.Module!, m => m.Id);
            query.Columns.Add<GXModule>(m => m.Id);
            return query;
        }

        private async Task EnsureOwnerAsync(GXDbConnection connection, IDbTransaction? transaction,
            Guid id, CancellationToken token)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("A valid agent identifier is required.");
            }
            if (await connection.SingleOrDefaultAsync<int>(transaction, Owners(id), token) != 1)
            {
                throw new UnauthorizedAccessException("The agent is unavailable or inaccessible.");
            }
        }

        private static void ValidateName(string? name)
            => GXPropertyStores.Agent.ValidateName(name);

        private static GXSelectArgs Find(GXAgent owner, GXModule? module, string name)
            => GXSelectArgs.SelectAll<GXAgentProperty>(p => p.Agent == owner && p.Module == module && p.Name == name);

        /// <inheritdoc />
        public async Task<GXAgentProperty?> ReadAsync(string agentId, string name, string? moduleId = null,
            Expression<Func<GXAgentProperty, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            if (!Guid.TryParse(agentId, out var id) || id == Guid.Empty)
            {
                throw new ArgumentException("A valid agent identifier is required.", nameof(agentId));
            }
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXAgentPropertiesPolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await EnsureOwnerAsync(connection, null, id, cancellationToken);
            var owner = new GXAgent { Id = id };
            var module = moduleId == null ? null : new GXModule { Id = moduleId };
            var query = Find(owner, module, name);
            if (columns != null)
            {
                query.Columns.Clear();
                query.Columns.Add(columns);
            }
            var property = await connection.SingleOrDefaultAsync<GXAgentProperty>(query, cancellationToken);
            if (property != null)
            {
                property.Agent = owner;
                property.Module = module;
            }
            return property;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXAgentProperty>> ListAsync(ListAgentProperties? request = null,
            ListAgentPropertiesResponse? response = null, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXAgentPropertiesPolicies.View);
            if (request?.Index < 0 || request?.Count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request));
            }
            var query = PropertiesQuery();
            if (request != null)
            {
                query.Where.FilterBy(request.Filter);
                if (request.Included?.Any() == true)
                {
                    query.Where.And<GXAgentProperty>(p => request.Included.Contains(p.Id));
                }
                if (request.Exclude?.Any() == true)
                {
                    query.Where.And<GXAgentProperty>(p => !request.Exclude.Contains(p.Id));
                }
            }
            GXDateTimeFilters.Apply<GXAgentProperty>(query, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            if (response != null)
            {
                var count = GXSelectArgs.Select<GXAgentProperty>(p => GXSql.Count(1));
                count.Joins.Append(query.Joins);
                count.Where.Append(query.Where);
                response.Count = await connection.SingleOrDefaultAsync<long>(count, cancellationToken);
            }
            if (request?.Count > 0)
            {
                query.Index = request.Index;
                query.Count = request.Count;
            }
            if (!string.IsNullOrEmpty(request?.OrderBy))
            {
                query.OrderBy.Add<GXAgentProperty>(request.OrderBy);
            }
            else
            {
                query.OrderBy.Add<GXAgentProperty>(p => p.CreationTime!);
            }

            query.Descending = request?.Descending == true;
            var properties = (await connection.SelectAsync<GXAgentProperty>(query, cancellationToken)).ToArray();
            response?.Properties = properties;
            return properties;
        }

        /// <inheritdoc />
        public async Task UpsertAsync(IEnumerable<GXAgentProperty> properties,
            Expression<Func<GXAgentProperty, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            properties = properties.ToArray();
            await GXHistoryMutation.RunAsync<GXAgentProperty>(_dbFactory, _user, new HttpContextAccessor().HttpContext?.RequestServices, properties,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(properties);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var notifications = new List<(List<string> Users, string Policy, Guid Id, string? Version)>();
            foreach (var property in properties)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ArgumentNullException.ThrowIfNull(property);
                ValidateName(property.Name);
                var id = property.Agent?.Id ?? Guid.Empty;
                await EnsureOwnerAsync(connection, transaction, id, cancellationToken);
                var owner = new GXAgent { Id = id };
                var existing = await GXPropertyStores.Agent.FindAsync(connection, transaction,
                    owner, property.Module, property.Name!, cancellationToken);
                var added = existing == null;
                var policy = added ? GXAgentPropertiesPolicies.Add : GXAgentPropertiesPolicies.Edit;
                await _authorizationService.EnsureAnyPolicyAsync(_user, policy);
                await GXPropertyStores.Agent.SaveAsync(connection, transaction, owner, property,
                    existing, columns, cancellationToken);
                var users = await GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAgent), id,
                    added ? NotificationAction.Add : NotificationAction.Edit, GXAgentPropertiesPolicies.View,
                    connection, transaction, notifications: _notificationCache);
                notifications.Add((users, policy, property.Id,
                    added || property.ConcurrencyStamp != existing!.ConcurrencyStamp ? property.ConcurrencyStamp : null));
            }
            await transaction.CommitAsync(cancellationToken);
            foreach (var notice in notifications)
            {
                _notificationCache.Add(notice.Users, new GXTarget(notice.Policy, notice.Id) { ConcurrencyStamp = notice.Version });
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            ids = ids.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXAgentProperty, Guid>(_dbFactory, _user, new HttpContextAccessor().HttpContext?.RequestServices, ids,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(ids);
            var targets = ids.Distinct().ToArray();
            if (targets.Length == 0)
            {
                return;
            }
            targets.ValidateIds();
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXAgentPropertiesPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var query = PropertiesQuery();
            query.Where.And<GXAgentProperty>(p => targets.Contains(p.Id));
            var rows = await connection.SelectAsync<GXAgentProperty>(transaction, query, cancellationToken);
            var notifications = new List<(List<string> Users, Guid Id)>();
            foreach (var row in rows)
            {
                var users = await GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXAgent), row.Agent!.Id,
                    NotificationAction.Remove, GXAgentPropertiesPolicies.View, connection, transaction,
                    notifications: _notificationCache);
                await GXPropertyStores.Agent.DeleteAsync(connection, transaction, row.Agent!, row, cancellationToken);
                notifications.Add((users, row.Id));
            }
            await transaction.CommitAsync(cancellationToken);
            foreach (var (Users, Id) in notifications)
            {
                _notificationCache.Add(Users, new GXTarget(GXAgentPropertiesPolicies.Delete, Id));
            }
        }, cancellationToken);
        }
    }
}
