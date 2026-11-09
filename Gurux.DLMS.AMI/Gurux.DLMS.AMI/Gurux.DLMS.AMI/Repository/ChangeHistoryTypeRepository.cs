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

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Manages persistent history type identities and their recording settings.</summary>
public sealed class ChangeHistoryTypeRepository : IChangeHistoryTypeRepository
{
    private readonly IDbConnectionFactory factory;
    private readonly IAuthorizationService authorization;
    private readonly ClaimsPrincipal user;

    /// <summary>Creates a repository for the authenticated caller.</summary>
    public ChangeHistoryTypeRepository(IDbConnectionFactory factory, IAuthorizationService authorization,
        IHttpContextAccessor accessor)
    {
        this.factory = factory;
        this.authorization = authorization;
        user = accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException();
        }
    }

    internal static async Task EnsureDefaultsAsync(GXDbConnection db, IDbTransaction transaction,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        foreach (var type in DefaultTypes())
        {
            await GXHistoryStore.ResolveTypeAsync(db, transaction, type, token);
        }
    }

    private static GXChangeHistoryType[] DefaultTypes() =>
        new[] { "Schedule", "Device", "User", "Gateway", "Script", "Agent" }
            .Select(name => GXHistoryEntities.Get(name).HistoryType).ToArray();

    /// <inheritdoc />
    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        cancellationToken.ThrowIfCancellationRequested();
        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureDefaultsAsync(db, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        ArgumentNullException.ThrowIfNull(ids);
        cancellationToken.ThrowIfCancellationRequested();
        var selected = ids.Distinct().ToArray();
        var defaults = DefaultTypes().ToDictionary(t => t.Id);
        if (selected.Length == 0 || selected.Any(id => !defaults.ContainsKey(id)))
        {
            throw new ValidationException("Select existing built-in history type GUIDs to reset.");
        }

        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var rows = await db.SelectAsync<GXChangeHistoryType>(transaction,
            GXSelectArgs.Select<GXChangeHistoryType>(t => t.Id, t => selected.Contains(t.Id)), cancellationToken);
        if (rows.Count != selected.Length)
        {
            throw new ValidationException("Only existing built-in history types can be reset. Ensure defaults to add missing types.");
        }

        foreach (var id in selected)
        {
            await db.UpdateAsync(transaction,
                        GXUpdateArgs.Update(defaults[id], t => new { t.Name, t.RecordHistory }), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GXChangeHistoryType>> ListAsync(ListChangeHistoryTypes? request = null,
        ListChangeHistoryTypesResponse? response = null, CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.View);
        if (request?.Index < 0 || request?.Count < 0)
        {
            throw new ValidationException("Paging values cannot be negative.");
        }

        var query = GXSelectArgs.Select<GXChangeHistoryType>(t => new { t.Id, t.Name, t.RecordHistory });
        // Navigation collections are not query filters or part of the default response.
        if (request?.Filter is { } filter)
        {
            query.Where.FilterBy(new GXChangeHistoryType { Id = filter.Id, Name = filter.Name, RecordHistory = filter.RecordHistory });
        }

        if (request?.Included?.Any() == true)
        {
            query.Where.And<GXChangeHistoryType>(t => request.Included.Contains(t.Id));
        }

        if (request?.Exclude?.Any() == true)
        {
            query.Where.And<GXChangeHistoryType>(t => !request.Exclude.Contains(t.Id));
        }

        if (request?.OrderBy is { Length: > 0 } order && order is not (nameof(GXChangeHistoryType.Id) or nameof(GXChangeHistoryType.Name)))
        {
            throw new ValidationException("Unsupported history type ordering column.");
        }

        if (request?.OrderBy == nameof(GXChangeHistoryType.Id))
        {
            query.OrderBy.Add<GXChangeHistoryType>(t => t.Id);
        }
        else
        {
            query.OrderBy.Add<GXChangeHistoryType>(t => t.Name!);
        }

        if (request?.OrderBy != nameof(GXChangeHistoryType.Id))
        {
            query.OrderBy.Add<GXChangeHistoryType>(t => t.Id);
        }

        query.Descending = request?.Descending == true;
        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        if (response != null)
        {
            var count = GXSelectArgs.Select<GXChangeHistoryType>(t => GXSql.Count(1));
            count.Where.Append(query.Where);
            response.Count = await db.SingleOrDefaultAsync<long>(count, cancellationToken);
        }
        query.Index = request?.Index ?? 0;
        query.Count = request?.Count ?? 0;
        var items = await db.SelectAsync<GXChangeHistoryType>(query, cancellationToken);
        if (response != null)
        {
            response.Items = items;
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<GXChangeHistoryType> ReadAsync(Guid id,
        Expression<Func<GXChangeHistoryType, object>>? columns = null, CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.View);
        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        var query = columns == null
            ? GXSelectArgs.Select<GXChangeHistoryType>(t => new { t.Id, t.Name, t.RecordHistory }, t => t.Id == id)
            : GXSelectArgs.Select(columns, t => t.Id == id);
        return await db.SingleOrDefaultAsync<GXChangeHistoryType>(query, cancellationToken)
            ?? throw new GXAmiNotFoundException("History type was not found.");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXChangeHistoryType> types,
        Expression<Func<GXChangeHistoryType, object>>? columns = null, CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        ArgumentNullException.ThrowIfNull(types);
        var items = types.ToArray();
        if (items.Any(t => t == null || t.Id == Guid.Empty))
        {
            throw new ValidationException("Stable history type GUIDs are required.");
        }

        if (items.Select(t => t.Id).Distinct().Count() != items.Length)
        {
            throw new ValidationException("History type GUIDs must be unique.");
        }

        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        foreach (var item in items)
        {
            var stored = await db.SingleOrDefaultAsync<GXChangeHistoryType>(tx,
                GXSelectArgs.Select<GXChangeHistoryType>(t => new { t.Id, t.Name, t.RecordHistory }, t => t.Id == item.Id), cancellationToken);
            bool name = stored == null || columns == null || ServerHelpers.Contains(columns, nameof(GXChangeHistoryType.Name));
            bool recording = stored == null || columns == null || ServerHelpers.Contains(columns, nameof(GXChangeHistoryType.RecordHistory));
            if (name && (string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 128))
            {
                throw new ValidationException("A history type name of at most 128 characters is required.");
            }

            var row = new GXChangeHistoryType
            {
                Id = item.Id,
                Name = name ? item.Name : stored!.Name,
                RecordHistory = recording ? item.RecordHistory : stored!.RecordHistory
            };
            if (stored == null)
            {
                await db.InsertAsync(tx, GXInsertArgs.Insert(row), cancellationToken);
            }
            else if (name || recording)
            {
                await db.UpdateAsync(tx, GXUpdateArgs.Update(row, t => new { t.Name, t.RecordHistory }), cancellationToken);
            }
        }
        await tx.CommitAsync(cancellationToken);
        return items.Select(t => t.Id).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<Guid> types, CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        ArgumentNullException.ThrowIfNull(types);
        var ids = types.Distinct().ToArray();
        if (ids.Any(id => id == Guid.Empty))
        {
            throw new ValidationException("Stable history type GUIDs are required.");
        }

        if (ids.Length == 0)
        {
            return;
        }

        await using var db = await factory.GetHistoryConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(cancellationToken);
        await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXChangeHistoryType>(t => ids.Contains(t.Id)), cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private void EnsureAdministrator()
    {
        if (!user.IsInRole(GXRoles.Admin))
        {
            throw new UnauthorizedAccessException("Only administrators can change history type settings.");
        }
    }
}
