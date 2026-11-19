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
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Stores receipts and send intents in the normal AMI database.</summary>
public sealed class GXScriptCommunityStore(IGXDatabase database) : IGXScriptCommunityStore
{
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Reads durable submission receipts.</summary>
    public async Task<List<GXScriptCommunityRecord>> ReadAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureTable(db);
            return await db.SelectAsync<GXScriptCommunityRecord>(GXSelectArgs.SelectAll<GXScriptCommunityRecord>(), token);
        }
        finally
        {
            _gate.Release();
        }
    }
    /// <summary>Persists a new or updated private receipt.</summary>
    public async Task SaveAsync(GXScriptCommunityRecord record, bool insert, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureTable(db);
            if (insert)
            {
                await db.InsertAsync(GXInsertArgs.Insert(record), token);
            }
            else
            {
                await db.UpdateAsync(GXUpdateArgs.Update(record), token);
            }
        }
        finally
        {
            _gate.Release();
        }
    }
    /// <summary>Creates or upgrades the local community-submission table to include the content-type discriminator.</summary>
    /// <param name="db">The open database connection used to read or persist update state.</param>
    private static void EnsureTable(GXDbConnection db)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<GXScriptCommunityRecord>())
        {
            schema.CreateTable<GXScriptCommunityRecord>(false, false);
        }
        else if (!schema.GetColumns<GXScriptCommunityRecord>().Contains(nameof(GXScriptCommunityRecord.ContentType), StringComparer.OrdinalIgnoreCase))
        {
            schema.UpdateTable(typeof(GXScriptCommunityRecord), false, false);
        }
    }
}
