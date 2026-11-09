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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Insert-only history persistence in the caller's local transaction.</summary>
internal static class GXHistoryStore
{
    internal static async Task<GXChangeHistoryType> ResolveTypeAsync(GXDbConnection db, IDbTransaction? tx,
        GXChangeHistoryType type, CancellationToken token)
    {
        if (type.Id == Guid.Empty)
        {
            throw new ValidationException("A stable history type GUID is required.");
        }

        var stored = await db.SingleOrDefaultAsync<GXChangeHistoryType>(tx,
            GXSelectArgs.Select<GXChangeHistoryType>(t => new { t.Id, t.Name, t.RecordHistory }, t => t.Id == type.Id), token);
        if (stored != null)
        {
            return stored;
        }

        if (string.IsNullOrWhiteSpace(type.Name))
        {
            throw new ValidationException("A history type name is required.");
        }

        var created = new GXChangeHistoryType { Id = type.Id, Name = type.Name, RecordHistory = false };
        await db.InsertAsync(tx, GXInsertArgs.Insert(created), token);
        return created;
    }

    internal static async Task<long> GetRevisionAsync(GXDbConnection db, IDbTransaction? tx,
        GXChangeHistoryType type, string id, CancellationToken token)
    {
        var row = await db.SingleOrDefaultAsync<GXChangeHistoryRevision>(tx,
            GXSelectArgs.SelectAll<GXChangeHistoryRevision>(r => r.EntityType == type && r.EntityId == id), token);
        return row?.Revision ?? 0;
    }

    internal static async Task<long> AdvanceAsync(GXDbConnection db, IDbTransaction tx,
        GXChangeHistoryType type, string id, long? expectedRevision, CancellationToken token)
    {
        type = await ResolveTypeAsync(db, tx, type, token);
        var row = await db.SingleOrDefaultAsync<GXChangeHistoryRevision>(tx,
            GXSelectArgs.SelectAll<GXChangeHistoryRevision>(r => r.EntityType == type && r.EntityId == id), token);
        var previous = row?.Revision ?? 0;
        if (expectedRevision.HasValue && previous != expectedRevision.Value)
        {
            throw new InvalidOperationException("The entity has changed. Reload its current revision before restoring.");
        }

        var next = checked(previous + 1);
        if (row == null)
        {
            await db.InsertAsync(tx, GXInsertArgs.Insert(new GXChangeHistoryRevision
            { Id = Guid.NewGuid(), EntityType = type, EntityId = id, Revision = next }), token);
        }
        else
        {
            row.Revision = next;
            var update = GXUpdateArgs.Update(row, r => r.Revision);
            update.Where.And<GXChangeHistoryRevision>(r => r.Id == row.Id && r.Revision == previous);
            if (await db.UpdateAsync(tx, update, token) != 1)
            {
                throw new InvalidOperationException("The entity revision changed concurrently.");
            }
        }
        return next;
    }

    internal static async Task<Guid> AppendAsync(GXDbConnection db, IDbTransaction tx,
        GXChangeHistoryItem item, ClaimsPrincipal actor, Guid changeSetId, CancellationToken token,
        long? assignedRevision = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        string? actorId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? actor.FindFirst("sub")?.Value;
        if (actor.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("An authenticated user is required to record history.");
        }

        if (await db.SingleOrDefaultAsync<GXUser>(tx,
                    GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == actorId && u.Removed == null), token) == null)
        {
            throw new UnauthorizedAccessException("The history actor is no longer available.");
        }

        if (item.EntityType == null || item.EntityType.Id == Guid.Empty ||
                    string.IsNullOrWhiteSpace(item.EntityId) || item.EntityId.Length > 512)
        {
            throw new ValidationException("A registered entity type and canonical entity identifier are required.");
        }

        if (item.SchemaVersion is not (null or 1))
        {
            throw new ValidationException("Unsupported history snapshot schema version.");
        }

        if (item.Operation == null || !Enum.IsDefined(item.Operation.Value))
        {
            throw new ValidationException("A valid history operation is required.");
        }

        ValidateJson(item.BeforeJson);
        ValidateJson(item.AfterJson);
        if (item.Operation == ChangeOperation.Create && (item.BeforeJson != null || item.AfterJson == null) ||
            item.Operation == ChangeOperation.Update && (item.BeforeJson == null || item.AfterJson == null) ||
            item.Operation == ChangeOperation.Delete && (item.BeforeJson == null || item.AfterJson != null))
        {
            throw new ValidationException("Snapshots do not match the recorded history operation.");
        }

        if (item.Id != Guid.Empty && await db.SingleOrDefaultAsync<GXChangeHistoryItem>(tx,
                    GXSelectArgs.Select<GXChangeHistoryItem>(h => h.Id, h => h.Id == item.Id), token) != null)
        {
            throw new InvalidOperationException("History items cannot be overwritten.");
        }

        item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
        item.ChangedBy = new GXUser { Id = actorId };
        item.ChangedAt = DateTimeOffset.UtcNow;
        item.ChangeSetId = changeSetId;
        item.SchemaVersion = 1;
        item.Source ??= ChangeSource.User;
        item.Revision = assignedRevision ?? await AdvanceAsync(db, tx, item.EntityType, item.EntityId, null, token);
        item.EntityType = await ResolveTypeAsync(db, tx, item.EntityType!, token);
        if (!item.EntityType.RecordHistory) { item.Id = Guid.Empty; return Guid.Empty; }
        await db.InsertAsync(tx, GXInsertArgs.Insert(item), token);
        return item.Id;
    }

    private static void ValidateJson(string? json)
    {
        if (json == null)
        {
            return;
        }

        try
        {
            using var value = JsonDocument.Parse(json);
            if (value.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationException("A history snapshot must be a JSON object.");
            }
        }
        catch (JsonException error)
        {
            throw new ValidationException("A history snapshot is not valid JSON.", error);
        }
    }
}
