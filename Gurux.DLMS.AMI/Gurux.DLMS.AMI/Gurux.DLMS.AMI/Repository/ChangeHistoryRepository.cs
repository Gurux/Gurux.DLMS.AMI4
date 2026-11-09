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
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Reads user-change history, restores saved configuration and removes expired history.</summary>
public sealed class ChangeHistoryRepository : IChangeHistoryRepository
{
    private readonly ClaimsPrincipal user;
    private readonly IDbConnectionFactory factory;
    private readonly IServiceProvider services;
    private readonly IAuthorizationService authorization;

    /// <summary>Creates a repository for the current authenticated user.</summary>
    public ChangeHistoryRepository(IAuthorizationService authorizationService, IDbConnectionFactory dbFactory,
        IHttpContextAccessor httpContextAccessor, IUserRepository userRepository,
        IServiceProvider serviceProvider, NotificationCache notificationCache)
    {
        user = httpContextAccessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException();
        }

        factory = dbFactory;
        services = serviceProvider;
        authorization = authorizationService;
    }

    private bool IsAdmin => user.IsInRole(GXRoles.Admin);
    private string UserId => ServerHelpers.GetUserId(user);

    /// <inheritdoc />
    public async Task<IEnumerable<GXChangeHistoryItem>> ListAsync(ListChangeHistoryItems? request = null,
        ListChangeHistoryItemsResponse? response = null, CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.View);
        if (request?.AllUsers == true && !IsAdmin)
        {
            throw new UnauthorizedAccessException();
        }

        if (request?.Index < 0 || request?.Count < 0)
        {
            throw new ValidationException("Paging values cannot be negative.");
        }

        var query = GXSelectArgs.Select<GXChangeHistoryItem>(h => new
        {
            h.Id,
            h.EntityType,
            h.EntityId,
            h.Operation,
            h.ChangedAt,
            h.ChangedBy,
            h.ChangeSetId,
            h.Source,
            h.SourceId,
            h.Revision,
            h.SchemaVersion,
            h.RestoredFromHistoryItemId,
            h.Description
        });
        if (request?.AllUsers != true)
        {
            var owner = new GXUser { Id = UserId };
            query.Where.And<GXChangeHistoryItem>(h => h.ChangedBy == owner);
        }
        AddActorProjection(query);
        query.Where.FilterBy(request?.Filter);
        if (request?.Included?.Any() == true)
        {
            query.Where.And<GXChangeHistoryItem>(h => request.Included.Contains(h.Id));
        }

        if (request?.Exclude?.Any() == true)
        {
            query.Where.And<GXChangeHistoryItem>(h => !request.Exclude.Contains(h.Id));
        }

        GXDateTimeFilters.Apply<GXChangeHistoryItem>(query, request?.DateFilters);
        if (!string.IsNullOrEmpty(request?.OrderBy))
        {
            if (!AllowedOrder.Contains(request.OrderBy))
            {
                throw new ValidationException("Unsupported history ordering column.");
            }

            Expression<Func<GXChangeHistoryItem, object>> order = request.OrderBy switch
            {
                "Id" => h => h.Id,
                "EntityType" => h => h.EntityType!,
                "EntityId" => h => h.EntityId!,
                "Operation" => h => h.Operation!,
                "ChangedAt" => h => h.ChangedAt!,
                "Revision" => h => h.Revision!,
                "ChangeSetId" => h => h.ChangeSetId!,
                _ => throw new ValidationException("Unsupported history ordering column.")
            };
            query.OrderBy.Add(order);
            query.Descending = request.Descending;
        }
        else
        {
            query.OrderBy.Add<GXChangeHistoryItem>(h => h.ChangedAt!);
            query.Descending = true;
        }
        query.OrderBy.Add<GXChangeHistoryItem>(h => h.Id);
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        var selected = await db.SelectAsync<GXChangeHistoryItem>(query, cancellationToken);
        var visible = new List<GXChangeHistoryItem>();
        foreach (var item in selected)
        {
            if (!await CanReadTargetAsync(item, cancellationToken))
            {
                continue;
            }

            item.ChangedBy = item.ChangedBy?.Id == null ? null : new GXUser { Id = item.ChangedBy.Id, UserName = item.ChangedBy.UserName, GivenName = item.ChangedBy.GivenName, Surname = item.ChangedBy.Surname };
            visible.Add(item);
        }
        long count = visible.Count;
        // Authorization precedes both counting and paging, so inaccessible rows cannot leak through Count.
        IEnumerable<GXChangeHistoryItem> page = visible.Skip((int)Math.Min(request?.Index ?? 0, int.MaxValue));
        if (request?.Count > 0)
        {
            page = page.Take((int)Math.Min(request.Count, int.MaxValue));
        }

        var items = page.ToArray();
        if (response != null)
        { response.Items = items; response.Count = count; }
        return items;
    }

    /// <inheritdoc />
    public async Task<GXChangeHistoryItem> ReadAsync(Guid id,
        Expression<Func<GXChangeHistoryItem, object>>? columns = null, CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.View);
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        var item = await ReadScopedAsync(db, id, cancellationToken);
        if (!await CanReadTargetAsync(item, cancellationToken))
        {
            throw new GXAmiNotFoundException("History item is not accessible.");
        }

        item.ChangedBy = item.ChangedBy?.Id == null ? null : new GXUser { Id = item.ChangedBy.Id, UserName = item.ChangedBy.UserName, GivenName = item.ChangedBy.GivenName, Surname = item.ChangedBy.Surname };
        if (columns == null)
        {
            return item;
        }

        var projection = GXSelectArgs.Select(columns, h => h.Id == id);
        if (ServerHelpers.Contains(columns, nameof(GXChangeHistoryItem.ChangedBy)))
        {
            AddActorProjection(projection);
        }
        else if (ServerHelpers.Contains(columns, nameof(GXChangeHistoryItem.EntityType)))
        {
            AddTypeProjection(projection);
        }

        var projected = await db.SingleOrDefaultAsync<GXChangeHistoryItem>(projection, cancellationToken)
            ?? throw new GXAmiNotFoundException("History item was not found.");
        if (projected.ChangedBy != null)
        {
            projected.ChangedBy = new GXUser { Id = projected.ChangedBy.Id, UserName = projected.ChangedBy.UserName, GivenName = projected.ChangedBy.GivenName, Surname = projected.ChangedBy.Surname };
        }

        return projected;
    }

    /// <inheritdoc />
    public Task<Guid> AppendAsync(GXChangeHistoryItem history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        return GXHistoryScope.RunAsync(factory, async db =>
        {
            var adapter = GXHistoryEntities.Get(history.EntityType ?? throw new ValidationException("A history type is required."));
            var id = adapter.ParseId(history.EntityId ?? "");
            await adapter.ReadAsync(services, id, cancellationToken);
            history.EntityType = adapter.HistoryType;
            history.EntityId = GXHistoryMutation.IdString(id);
            history.BeforeJson = Clean(history.BeforeJson, adapter, id);
            history.AfterJson = Clean(history.AfterJson, adapter, id);
            await using var tx = await db.BeginTransactionAsync(cancellationToken);
            var result = await GXHistoryStore.AppendAsync(db, tx, history, user, GXHistoryScope.ChangeSetId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> RestoreAsync(Guid historyItemId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.Restore);
        if (expectedRevision < 0)
        {
            throw new ValidationException("An expected current revision is required.");
        }

        return await GXHistoryScope.RunAsync(factory, async db =>
        {
            var source = await ReadScopedAsync(db, historyItemId, cancellationToken);
            if (source.AfterJson == null)
            {
                throw new ValidationException("This history item has no saved state to restore.");
            }

            if (source.SchemaVersion != 1)
            {
                throw new ValidationException("Unsupported history snapshot schema version.");
            }

            var adapter = GXHistoryEntities.Get(source.EntityType!);
            var id = adapter.ParseId(source.EntityId!);
            var current = await adapter.ReadAsync(services, id, cancellationToken)
                ?? throw new GXAmiNotFoundException("The target entity no longer exists.");
            var restored = GXHistorySnapshot.Deserialize(source.AfterJson, adapter.EntityType);
            if (GXHistoryMutation.IdString(adapter.GetId(restored)) != source.EntityId)
            {
                throw new ValidationException("The snapshot identifier does not match its history target.");
            }

            await using var tx = await db.BeginTransactionAsync(cancellationToken);
            var revision = await GXHistoryStore.AdvanceAsync(db, tx, adapter.HistoryType, source.EntityId!, expectedRevision, cancellationToken);
            var before = GXHistorySnapshot.Serialize(current);
            using (GXHistoryMutation.Suppress(adapter.EntityType, id))
                await adapter.WriteAsync(services, restored, cancellationToken);
            var after = await GXHistoryMutation.SnapshotAsync(adapter, db, services, id, cancellationToken)
                ?? throw new InvalidOperationException("Restoration removed the target unexpectedly.");
            var item = new GXChangeHistoryItem
            {
                EntityType = adapter.HistoryType,
                EntityId = source.EntityId,
                Operation = ChangeOperation.Update,
                BeforeJson = before,
                AfterJson = after,
                RestoredFromHistoryItemId = source.Id
            };
            var result = await GXHistoryStore.AppendAsync(db, tx, item, user, GXHistoryScope.ChangeSetId, cancellationToken, revision);
            await tx.CommitAsync(cancellationToken);
            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsAdmin)
        {
            throw new UnauthorizedAccessException("Only administrators can remove expired history.");
        }

        await authorization.EnsureAnyPolicyAsync(user, GXChangeHistoryPolicies.Delete);
        cutoff = cutoff.ToUniversalTime();
        const int batchSize = 500;
        long deleted = 0;
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var transaction = await db.BeginTransactionAsync(cancellationToken);
            var query = GXSelectArgs.Select<GXChangeHistoryItem>(h => h.Id, h => h.ChangedAt < cutoff);
            query.OrderBy.Add<GXChangeHistoryItem>(h => h.Id);
            query.Count = batchSize;
            var ids = (await db.SelectAsync<GXChangeHistoryItem>(transaction, query, cancellationToken))
                .Select(h => h.Id).ToArray();
            if (ids.Length == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return deleted;
            }
            // Recheck age in the delete predicate and count only rows actually deleted by this operation.
            var count = await db.DeleteAsync(transaction,
                GXDeleteArgs.Delete<GXChangeHistoryItem>(h => ids.Contains(h.Id) && h.ChangedAt < cutoff), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            deleted = checked(deleted + count);
        }
    }

    /// <summary>Reads the current configuration revision independently of the selected historical revision.</summary>
    public async Task<long> GetCurrentRevisionAsync(Guid historyItemId, CancellationToken cancellationToken = default)
    {
        var item = await ReadAsync(historyItemId, cancellationToken: cancellationToken);
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        return await GXHistoryStore.GetRevisionAsync(db, null, item.EntityType!, item.EntityId!, cancellationToken);
    }

    private async Task<GXChangeHistoryItem> ReadScopedAsync(GXDbConnection db, Guid id, CancellationToken token)
    {
        var query = GXSelectArgs.SelectAll<GXChangeHistoryItem>(h => h.Id == id);
        if (!IsAdmin)
        {
            var owner = new GXUser { Id = UserId };
            query.Where.And<GXChangeHistoryItem>(h => h.ChangedBy == owner);
        }
        AddActorProjection(query);
        return await db.SingleOrDefaultAsync<GXChangeHistoryItem>(query, token)
            ?? throw new GXAmiNotFoundException("History item was not found.");
    }

    private static void AddActorProjection(GXSelectArgs query)
    {
        AddTypeProjection(query);
        query.Joins.AddLeftJoin<GXChangeHistoryItem, GXUser>(h => h.ChangedBy!, u => u.Id!);
        query.Columns.Add<GXUser>(u => new { u.Id, u.UserName, u.GivenName, u.Surname });
    }

    private static void AddTypeProjection(GXSelectArgs query)
    {
        query.Joins.AddLeftJoin<GXChangeHistoryItem, GXChangeHistoryType>(h => h.EntityType!, t => t.Id);
        query.Columns.Add<GXChangeHistoryType>(t => new { t.Id, t.Name, t.RecordHistory });
    }

    private async Task<bool> CanReadTargetAsync(GXChangeHistoryItem item, CancellationToken token)
    {
        var adapter = GXHistoryEntities.Get(item.EntityType!);
        var id = adapter.ParseId(item.EntityId!);
        // A deleted target cannot supply instance authorization. Only administrators, or the actor's
        // own deletion record, may use the remaining explicit resource-view permission as a fallback.
        if (IsAdmin || item.Operation == ChangeOperation.Delete && item.ChangedBy?.Id == UserId)
        {
            await using var db = await factory.GetHistoryConnectionAsync(token);
            if (await adapter.ReadStoredAsync(db, id, token) == null)
            {
                return GXHistoryViewPolicies.Values.TryGetValue(adapter.EntityType.Name, out var policy) &&
                                (await authorization.AuthorizeAsync(user, null, policy)).Succeeded;
            }
        }
        try
        { return await adapter.ReadAsync(services, id, token) != null; }
        catch (UnauthorizedAccessException) { return false; }
        catch (GXAmiForbiddenException) { return false; }
        catch (GXAmiNotFoundException) { return false; }
    }

    private static string? Clean(string? json, IGXHistoryEntity adapter, object id)
    {
        if (json == null)
        {
            return null;
        }

        var entity = GXHistorySnapshot.Deserialize(json, adapter.EntityType);
        if (GXHistoryMutation.IdString(adapter.GetId(entity)) != GXHistoryMutation.IdString(id))
        {
            throw new ValidationException("The snapshot identifier does not match its history target.");
        }

        return GXHistorySnapshot.Serialize(entity);
    }

    private static readonly HashSet<string> AllowedOrder = new(StringComparer.Ordinal)
    { "Id", "EntityType", "EntityId", "Operation", "ChangedAt", "Revision", "ChangeSetId" };
}
