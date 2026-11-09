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

using Gurux.DLMS.AMI.Services.Database;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Persists update preferences and successful notification deliveries.</summary>
public sealed class GXUpdateSettingsStore(IGXDatabase database)
{
    const string ConfigurationName = "update-notifications";
    static readonly SemaphoreSlim Gate = new(1, 1);

    sealed class State
    {
        public UpdateSettings Settings { get; set; } = new();
        public DateTimeOffset? Checked { get; set; }
        public HashSet<string> Delivered { get; set; } = [];
    }

    async Task<T> AccessAsync<T>(Func<State, T> action, bool write, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            var schema = new GXSchemaManager(db);
            if (!schema.TableExist<GXConfiguration>())
            {
                schema.CreateTable<GXConfiguration>(false, false);
            }
        
            using var transaction = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var row = await db.SingleOrDefaultAsync<GXConfiguration>(transaction, GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == ConfigurationName), token);
            var state = string.IsNullOrWhiteSpace(row?.Settings) ? new State() : JsonSerializer.Deserialize<State>(row.Settings) ?? new State();
            var result = action(state);
            if (write)
            {
                bool added = row == null;
                row ??= new GXConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = ConfigurationName,
                    Enabled = false
                };
                row.Settings = JsonSerializer.Serialize(state);
                if (added)
                {
                    await db.InsertAsync(transaction, GXInsertArgs.Insert(row), token);
                }
                else
                {
                    await db.UpdateAsync(transaction, GXUpdateArgs.Update(row, c => c.Settings!), token);
                }
            }
        
            transaction.Commit();
            return result;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Reads update preferences.</summary>
    public Task<UpdateSettings> ReadAsync(CancellationToken token = default)
        => AccessAsync(s => s.Settings, false, token);

    /// <summary>Validates and saves preferences without resetting delivery history.</summary>
    public Task SaveAsync(UpdateSettings settings, CancellationToken token = default)
    {
        GXUpdateNotificationPolicy.Validate(settings);
        return AccessAsync(s => { s.Settings = settings; return true; }, true, token);
    }

    /// <summary>Whether the automatic check interval has elapsed.</summary>
    public Task<bool> IsDueAsync(CancellationToken token = default)
        => AccessAsync(s => GXUpdateNotificationPolicy.IsDue(s.Settings, s.Checked, DateTimeOffset.UtcNow), false, token);

    /// <summary>Records a successful update check.</summary>
    public Task MarkCheckedAsync(CancellationToken token = default)
        => AccessAsync(s => { s.Checked = DateTimeOffset.UtcNow; return true; }, true, token);

    internal Task<bool> WasDeliveredAsync(string key, CancellationToken token)
        => AccessAsync(s => s.Delivered.Contains(key), false, token);

    internal Task MarkDeliveredAsync(string key, CancellationToken token)
        => AccessAsync(s => s.Delivered.Add(key), true, token);
}
