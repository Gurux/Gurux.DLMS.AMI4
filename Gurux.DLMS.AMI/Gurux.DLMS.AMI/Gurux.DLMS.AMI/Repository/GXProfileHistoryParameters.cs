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

using System.Data;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Profile read coverage persisted in the shared GXObject.Properties collection.</summary>
internal static class GXProfileHistoryParameters
{
    internal const string Prefix = "Ami.ProfileHistory.";

    public static bool IsManaged(GXObjectProperty parameter)
        => parameter.Module == null && parameter.Name?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    internal sealed class ReadInterval
    {
        public Guid Id { get; set; }
        public Guid AttributeId { get; set; }
        public Guid ObjectId { get; set; }
        public Guid TaskId { get; set; }
        public DateTimeOffset? ClaimStart { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public bool Completed { get; set; }
        public int RowCount { get; set; }
        public DateTimeOffset? CommittedAt { get; set; }
        public DateTimeOffset? AvailableFrom { get; set; }
    }

    private static ReadInterval Decode(GXObjectProperty parameter)
    {
        var interval = JsonSerializer.Deserialize<ReadInterval>(parameter.Value ?? "null")
            ?? throw new InvalidOperationException("Profile history parameter is invalid.");
        if (parameter.Id != interval.Id || parameter.Name != Prefix + interval.Id.ToString("N"))
            {
            throw new InvalidOperationException("Profile history parameter identity is invalid.");
        }
        return interval;
    }

    private static async Task<GXObject> LoadObjectAsync(GXDbConnection db, IDbTransaction? transaction,
        Guid objectId, CancellationToken token)
    {
        var owner = new GXObject { Id = objectId };
        owner.Properties = await GXPropertyStores.Object.ListAsync(db, transaction, owner, token);
        return owner;
    }

    private static async Task<GXObject?> LoadAttributeObjectAsync(GXDbConnection db, IDbTransaction? transaction,
        Guid attributeId, CancellationToken token)
    {
        var query = GXSelectArgs.Select<GXObject>(o => o.Id);
        query.Joins.AddInnerJoin<GXObject, GXAttribute>(o => o.Id, a => a.Object!);
        query.Where.And<GXAttribute>(a => a.Id == attributeId);
        var owner = await db.SingleOrDefaultAsync<GXObject>(transaction, query, token);
        return owner == null ? null : await LoadObjectAsync(db, transaction, owner.Id, token);
    }

    internal static async Task<List<ReadInterval>> ListAsync(GXDbConnection db, IDbTransaction? transaction,
        Guid attributeId, CancellationToken token)
    {
        var owner = await LoadAttributeObjectAsync(db, transaction, attributeId, token);
        return (owner?.Properties ?? []).Where(IsManaged).Select(Decode)
            .Where(s => s.AttributeId == attributeId && s.ObjectId == owner!.Id).ToList();
    }

    internal static async Task<ReadInterval?> FindAsync(GXDbConnection db, IDbTransaction transaction, Guid id, CancellationToken token)
    {
        // Resolve the owner first; all subsequent reads and writes use its shared Properties collection.
        string name = Prefix + id.ToString("N");
        var reference = await db.SingleOrDefaultAsync<GXObjectProperty>(transaction,
            GXSelectArgs.SelectAll<GXObjectProperty>(p => p.Id == id && p.Name == name && p.Module == null), token);
        if (reference == null) {
            return null;
        }
        var interval = Decode(reference);
        var owner = await LoadObjectAsync(db, transaction, interval.ObjectId, token);
        var property = owner.Properties!.SingleOrDefault(p => p.Id == id && IsManaged(p));
        if (property == null)
            {
            throw new UnauthorizedAccessException("Profile history property belongs to another object.");
        }
        return Decode(property);
    }

    internal static async Task SaveAsync(GXDbConnection db, IDbTransaction transaction, ReadInterval interval, bool insert, CancellationToken token)
    {
        var owner = await LoadObjectAsync(db, transaction, interval.ObjectId, token);
        var existing = owner.Properties!.SingleOrDefault(p => p.Id == interval.Id && IsManaged(p));
        if (!insert && existing == null)
            {
            throw new InvalidOperationException("The profile interval changed before it could be committed.");
        }
        var property = new GXObjectProperty
        {
            Id = interval.Id, Name = Prefix + interval.Id.ToString("N"),
            Value = JsonSerializer.Serialize(interval), ConcurrencyStamp = existing?.ConcurrencyStamp
        };
        await GXPropertyStores.Object.SaveAsync(db, transaction, owner, property, existing,
            cancellationToken: token, insertId: interval.Id);
        if (existing == null) {
            owner.Properties!.Add(property);
        }
    }

    internal static async Task ClearAsync(GXDbConnection db, IDbTransaction transaction, Guid attributeId, CancellationToken token)
    {
        var owner = await LoadAttributeObjectAsync(db, transaction, attributeId, token);
        if (owner == null) {
            return;
        }
        var properties = owner.Properties!.Where(IsManaged).Where(p => Decode(p).AttributeId == attributeId).ToArray();
        foreach (var property in properties)
        {
            await GXPropertyStores.Object.DeleteAsync(db, transaction, owner, property, token);
            owner.Properties!.Remove(property);
        }
    }
}
