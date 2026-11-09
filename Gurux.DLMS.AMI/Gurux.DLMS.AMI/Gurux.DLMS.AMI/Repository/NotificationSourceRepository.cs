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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Reads registered notification sources and updates their server-wide delivery settings.</summary>
public sealed class NotificationSourceRepository : INotificationSourceRepository
{
    private readonly ClaimsPrincipal user;
    private readonly IAuthorizationService authorization;
    private readonly IDbConnectionFactory factory;
    private readonly NotificationCache cache;

    /// <summary>Creates a repository for the current authenticated user.</summary>
    public NotificationSourceRepository(IHttpContextAccessor accessor, IAuthorizationService authorizationService,
        IDbConnectionFactory dbFactory, NotificationCache notificationCache)
    {
        user = accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        if (user.Identity?.IsAuthenticated != true) {
            throw new UnauthorizedAccessException();
        }
        authorization = authorizationService;
        factory = dbFactory;
        cache = notificationCache;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GXNotificationSource>> ListAsync(ListNotificationSources? request = null,
        ListNotificationSourcesResponse? response = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await authorization.EnsureAnyPolicyAsync(user, GXConfigurationPolicies.View);
        if (request?.Index < 0 || request?.Count < 0) {
            throw new ArgumentException("Paging values cannot be negative.", nameof(request));
        }
        var query = GXSelectArgs.SelectAll<GXNotificationSource>();
        var included = request?.Included?.Distinct().ToArray();
        var excluded = request?.Exclude?.Distinct().ToArray();
        if (included?.Length > 0) {
            query.Where.And<GXNotificationSource>(s => included.Contains(s.Id));
        }
        if (excluded?.Length > 0) {
            query.Where.And<GXNotificationSource>(s => !excluded.Contains(s.Id));
        }
        // This DTO deliberately has no ORM Filter attributes. Apply nullable settings
        // explicitly so false is a criterion and null means no enabled filter.
        if (request?.Filter is { } filter)
        {
            if (filter.Id != 0) {
                query.Where.And<GXNotificationSource>(s => s.Id == filter.Id);
            }
            if (filter.Enabled.HasValue) {
                query.Where.And<GXNotificationSource>(s => s.Enabled == filter.Enabled);
            }
            if (!string.IsNullOrEmpty(filter.ModuleId)) {
                query.Where.And<GXNotificationSource>(s => s.ModuleId == filter.ModuleId);
            }
            if (!string.IsNullOrEmpty(filter.Source)) {
                query.Where.And<GXNotificationSource>(s => s.Source.Contains(filter.Source));
            }
        }
        var order = string.IsNullOrEmpty(request?.OrderBy) ? nameof(GXNotificationSource.Source) : request.OrderBy;
        if (order is not (nameof(GXNotificationSource.Id) or nameof(GXNotificationSource.Source) or
            nameof(GXNotificationSource.Enabled) or nameof(GXNotificationSource.ModuleId)))
            {
            throw new ArgumentException("Unsupported notification source ordering column.", nameof(request));
        }
        query.OrderBy.Add<GXNotificationSource>(order);
        if (order != nameof(GXNotificationSource.Id)) {
            query.OrderBy.Add<GXNotificationSource>(s => s.Id);
        }
        query.Descending = request?.Descending == true;
        await using var connection = await factory.GetHistoryConnectionAsync(cancellationToken);
        if (response != null)
        {
            var count = GXSelectArgs.Select<GXNotificationSource>(s => GXSql.Count(1));
            count.Where.Append(query.Where);
            response.Count = await connection.SingleOrDefaultAsync<long>(count, cancellationToken);
        }
        if (request?.Count > 0) { query.Index = request.Index; query.Count = request.Count; }
        var selected = await connection.SelectAsync<GXNotificationSource>(query, cancellationToken);
        if (response != null) {
            response.NotificationSources = selected;
        }
        return selected;
    }

    /// <inheritdoc />
    public async Task SetEnabledAsync(IEnumerable<int> ids, bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await authorization.EnsureAnyPolicyAsync(user, GXConfigurationPolicies.Edit);
        ArgumentNullException.ThrowIfNull(ids);
        var selected = ids.Distinct().ToArray();
        if (selected.Length == 0 || selected.Any(id => id <= 0))
            {
            throw new ArgumentException("At least one positive notification source identifier is required.", nameof(ids));
        }
        // Source snapshots contain only scalar Id/Enabled values; read them directly
        // so history capture does not impose an additional configuration.view grant.
        await GXHistoryMutation.RunIdsAsync<GXNotificationSource, int>(factory, user, null, selected, async () =>
        {
            await using var connection = await factory.GetHistoryConnectionAsync(cancellationToken);
            // Validate the entire batch before changing any setting. Only Enabled is mutable.
            foreach (var batch in selected.Chunk(500))
            {
                var existing = await connection.SelectAsync<GXNotificationSource>(
                    GXSelectArgs.Select<GXNotificationSource>(s => s.Id, s => batch.Contains(s.Id)), cancellationToken);
                if (existing.Count != batch.Length) {
                    throw new GXAmiNotFoundException("Notification source not found.");
                }
            }
            foreach (var batch in selected.Chunk(500))
                {
                await connection.UpdateAsync(GXUpdateArgs.Update(new GXNotificationSource { Enabled = enabled },
                    s => s.Enabled!, s => batch.Contains(s.Id)), cancellationToken);
            }
            var sources = await connection.SelectAsync<GXNotificationSource>(GXSelectArgs.SelectAll<GXNotificationSource>(), cancellationToken);
            // ConfigureSources joins the history transaction's after-commit queue.
            cache.ConfigureSources(sources);
        }, cancellationToken);
    }
}
