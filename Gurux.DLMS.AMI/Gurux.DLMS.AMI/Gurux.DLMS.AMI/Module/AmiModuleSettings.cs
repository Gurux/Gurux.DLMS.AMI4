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

using System.Text.Json;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>
/// A module-owned settings snapshot; serializer metadata is not shared across modules.
/// </summary>
/// <remarks>Creates a settings snapshot from an optional serialized value.</remarks>
internal sealed class AmiModuleSettings(string? value = null, IDbConnectionFactory? database = null, string? moduleId = null) : IGXAmiModuleSettingsSnapshot
{
    private string? value = value;
    private AmiModuleSettingsSchema? schema;
    private readonly Lock snapshotLock = new();
    private readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
    private readonly IDbConnectionFactory? database = database;
    private readonly string? moduleId = moduleId;
    /// <summary>Serialized module settings, read and written atomically.</summary>
    public string? Value
    {
        get => Volatile.Read(ref value);
        internal set => SetValidatedValue(Normalize(value));
    }
    internal void SetValidatedValue(string? settings)
    {
        lock (snapshotLock)
        {
            Volatile.Write(ref value, AmiModuleSettingsStorage.PreserveState(settings, value));
        }
    }
    internal string? Normalize(string? settings) => schema?.Normalize(settings) ?? settings;

    internal async Task RegisterSchemaAsync(AmiModuleSettingsSchema? settingsSchema, CancellationToken cancellationToken)
    {
        if (settingsSchema == null)
        {
            return;
        }
        // Complete migration and validation before publishing either metadata or a new snapshot.
        string? normalized = null;
        if (database != null && moduleId != null)
        {
            using var gate = await AmiModuleSettingsStorage.LockAsync(moduleId, cancellationToken);
            await using var db = await database.GetConnectionAsync(cancellationToken, false);
            var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, moduleId, cancellationToken);
            if (module != null)
            {
                // Configuration may have changed while activation waited for the settings gate.
                normalized = settingsSchema.Normalize(module.GetSettings());
                module.SetSettings(normalized);
                await AmiModuleSettingsStorage.SavePropertiesAsync(db, module, cancellationToken);
                PublishSchema(settingsSchema, normalized);
                return;
            }
        }
        normalized ??= settingsSchema.Normalize(Value);
        PublishSchema(settingsSchema, normalized);
    }

    private void PublishSchema(AmiModuleSettingsSchema settingsSchema, string normalized)
    {
        lock (snapshotLock)
        {
            schema = settingsSchema;
            Volatile.Write(ref value, normalized);
        }
    }
    /// <summary>Deserializes the settings or creates default settings when no value is available.</summary>
    public T Read<T>() where T : new()
    {
        string? snapshot = Value;
        if (schema != null && !string.IsNullOrWhiteSpace(snapshot))
        {
            // Match schema validation: host-owned metadata is not part of the configuration model.
            var configuration = AmiModuleSettingsStorage.Parse(snapshot);
            configuration.Remove(AmiModuleSettingsSchema.VersionKey);
            configuration.Remove(AmiModuleSettingsStorage.StateKey);
            configuration.Remove(AmiModuleSettingsStorage.ResourcesKey);
            return configuration.Deserialize<T>(json) ?? new T();
        }
        return string.IsNullOrWhiteSpace(snapshot) ? new T() : JsonSerializer.Deserialize<T>(snapshot, json) ?? new T();
    }

    /// <inheritdoc />
    public async Task<T> ReadStateAsync<T>(CancellationToken cancellationToken = default) where T : class, new()
    {
        EnsurePersistent();
        using var gate = await AmiModuleSettingsStorage.LockAsync(moduleId!, cancellationToken);
        await using var db = await database!.GetConnectionAsync(cancellationToken, false);
        var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, moduleId!, cancellationToken)
            ?? throw new InvalidOperationException("Module settings record is missing.");
        RefreshState(module.GetSettings());
        return AmiModuleSettingsStorage.Parse(module.GetSettings())[AmiModuleSettingsStorage.StateKey]?.Deserialize<T>(json) ?? new T();
    }

    /// <inheritdoc />
    public async Task<T> UpdateStateAsync<T>(Action<T> update, CancellationToken cancellationToken = default) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(update);
        EnsurePersistent();
        using var gate = await AmiModuleSettingsStorage.LockAsync(moduleId!, cancellationToken);
        await using var db = await database!.GetConnectionAsync(cancellationToken, false);
        var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, moduleId!, cancellationToken)
            ?? throw new InvalidOperationException("Module settings record is missing.");
        var settings = AmiModuleSettingsStorage.Parse(module.GetSettings());
        var state = settings[AmiModuleSettingsStorage.StateKey]?.Deserialize<T>(json) ?? new T();
        update(state);
        settings[AmiModuleSettingsStorage.StateKey] = JsonSerializer.SerializeToNode(state, json);
        module.Id = moduleId!;
        module.SetSettings(settings.ToJsonString());
        await AmiModuleSettingsStorage.SavePropertiesAsync(db, module, cancellationToken);
        RefreshState(module.GetSettings());
        return state;
    }

    private void EnsurePersistent()
    {
        if (database == null || moduleId == null)
        {
            throw new NotSupportedException("Persistent module settings are unavailable.");
        }
    }

    private void RefreshState(string? persisted)
    {
        lock (snapshotLock)
        {
            Volatile.Write(ref value, AmiModuleSettingsStorage.PreserveState(value, persisted));
        }
    }
}
