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
    /// <summary>Configuration record name used to persist this updater's settings.</summary>
    const string ConfigurationName = "update-notifications";
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Persisted update settings and the time they were last checked.</summary>
    sealed class State
    {
        /// <summary>Update configuration persisted for subsequent checks.</summary>
        public UpdateSettings Settings { get; set; } = new();
        /// <summary>UTC time of the last completed update check, when one has run.</summary>
        public DateTimeOffset? Checked { get; set; }
        /// <summary>Notification identifiers already delivered to avoid duplicate update notices.</summary>
        public HashSet<string> Delivered { get; set; } = [];
    }

    /// <summary>Serializes access to persisted update settings and optionally commits changes made by the supplied callback.</summary>
    /// <typeparam name="T">The callback result or deserialized response type.</typeparam>
    /// <param name="action">The callback executed within the restored scope or settings transaction.</param>
    /// <param name="write">Whether callback changes should be serialized and committed to the settings row.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task containing the callback's result after the settings transaction completes.</returns>
    async Task<T> AccessAsync<T>(Func<State, T> action, bool write, CancellationToken token)
    {
        await _gate.WaitAsync(token);
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
            _gate.Release();
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

    /// <summary>Checks whether the update notification identified by the key has already been delivered.</summary>
    /// <param name="key">The deduplication key identifying one delivered update notification.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task indicating whether the notification key is already recorded as delivered.</returns>
    internal Task<bool> WasDeliveredAsync(string key, CancellationToken token)
        => AccessAsync(s => s.Delivered.Contains(key), false, token);

    /// <summary>Persists the notification key so the same update notification is not delivered again.</summary>
    /// <param name="key">The deduplication key identifying one delivered update notification.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task that completes after the delivered-notification key has been persisted.</returns>
    internal Task MarkDeliveredAsync(string key, CancellationToken token)
        => AccessAsync(s => s.Delivered.Add(key), true, token);
}
