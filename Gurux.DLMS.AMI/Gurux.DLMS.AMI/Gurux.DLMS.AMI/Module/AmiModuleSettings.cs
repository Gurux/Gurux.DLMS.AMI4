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
    /// <summary>Sensitive text retained until the enclosing scope clears it.</summary>
    private string? _value = value;
    /// <summary>Settings schema belonging to the current module generation.</summary>
    private AmiModuleSettingsSchema? _schema;
    /// <summary>Protects the published snapshot while it is replaced or read.</summary>
    private readonly Lock _snapshotLock = new();
    /// <summary>Serializer settings shared by requests or persisted state in this component.</summary>
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    /// <summary>Optional database access used when a persistent store is configured.</summary>
    private readonly IDbConnectionFactory? _database = database;
    /// <summary>Identifier of the module that owns this operation.</summary>
    private readonly string? _moduleId = moduleId;
    /// <summary>Serialized module settings, read and written atomically.</summary>
    public string? Value
    {
        get => Volatile.Read(ref _value);
        internal set => SetValidatedValue(Normalize(value));
    }
    /// <summary>Stores the normalized settings value after validation has succeeded.</summary>
    /// <param name="settings">Serialized module configuration.</param>
    internal void SetValidatedValue(string? settings)
    {
        lock (_snapshotLock)
        {
            Volatile.Write(ref _value, AmiModuleSettingsStorage.PreserveState(settings, _value));
        }
    }
    /// <summary>Normalizes module settings through the current schema when one is registered.</summary>
    /// <param name="settings">Serialized module configuration.</param>
    /// <returns>Normalized configuration, or the unchanged input when no schema applies.</returns>
    internal string? Normalize(string? settings)
    {
        settings = AmiModuleSettingsStorage.PreserveSchemaVersion(settings, Value);
        return _schema?.Normalize(settings) ?? settings;
    }

    /// <summary>Migrates and validates persisted settings before publishing the module generation's schema and normalized snapshot.</summary>
    /// <param name="settingsSchema">Settings schema to validate and publish, or null to leave registration unchanged.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes once schema registration and any required persistence succeed.</returns>
    internal async Task RegisterSchemaAsync(AmiModuleSettingsSchema? settingsSchema, CancellationToken cancellationToken)
    {
        if (settingsSchema == null)
        {
            return;
        }
        // Complete migration and validation before publishing either metadata or a new snapshot.
        string? normalized = null;
        if (_database != null && _moduleId != null)
        {
            using var gate = await AmiModuleSettingsStorage.LockAsync(_moduleId, cancellationToken);
            await using var db = await _database.GetConnectionAsync(cancellationToken, false);
            var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, _moduleId, cancellationToken);
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

    /// <summary>Atomically publishes a validated schema and its normalized configuration snapshot.</summary>
    /// <param name="settingsSchema">Settings schema to validate and publish, or null to leave registration unchanged.</param>
    /// <param name="normalized">Configuration that has already passed the schema's migration and validation.</param>
    private void PublishSchema(AmiModuleSettingsSchema settingsSchema, string normalized)
    {
        lock (_snapshotLock)
        {
            _schema = settingsSchema;
            Volatile.Write(ref _value, normalized);
        }
    }
    /// <summary>Deserializes the settings or creates default settings when no value is available.</summary>
    public T Read<T>() where T : new()
    {
        string? snapshot = Value;
        if (_schema != null && !string.IsNullOrWhiteSpace(snapshot))
        {
            // Match schema validation: host-owned metadata is not part of the configuration model.
            var configuration = AmiModuleSettingsStorage.Parse(snapshot);
            configuration.Remove(AmiModuleSettingsSchema.VersionKey);
            configuration.Remove(AmiModuleSettingsStorage.StateKey);
            configuration.Remove(AmiModuleSettingsStorage.ResourcesKey);
            return configuration.Deserialize<T>(_json) ?? new T();
        }
        return string.IsNullOrWhiteSpace(snapshot) ? new T() : JsonSerializer.Deserialize<T>(snapshot, _json) ?? new T();
    }

    /// <inheritdoc />
    public async Task<T> ReadStateAsync<T>(CancellationToken cancellationToken = default) where T : class, new()
    {
        EnsurePersistent();
        using var gate = await AmiModuleSettingsStorage.LockAsync(_moduleId!, cancellationToken);
        await using var db = await _database!.GetConnectionAsync(cancellationToken, false);
        var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, _moduleId!, cancellationToken)
            ?? throw new InvalidOperationException("Module settings record is missing.");
        RefreshState(module.GetSettings());
        return AmiModuleSettingsStorage.Parse(module.GetSettings())[AmiModuleSettingsStorage.StateKey]?.Deserialize<T>(_json) ?? new T();
    }

    /// <inheritdoc />
    public async Task<T> UpdateStateAsync<T>(Action<T> update, CancellationToken cancellationToken = default) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(update);
        EnsurePersistent();
        using var gate = await AmiModuleSettingsStorage.LockAsync(_moduleId!, cancellationToken);
        await using var db = await _database!.GetConnectionAsync(cancellationToken, false);
        var module = await AmiModuleSettingsStorage.ReadModuleAsync(db, _moduleId!, cancellationToken)
            ?? throw new InvalidOperationException("Module settings record is missing.");
        var settings = AmiModuleSettingsStorage.Parse(module.GetSettings());
        var state = settings[AmiModuleSettingsStorage.StateKey]?.Deserialize<T>(_json) ?? new T();
        update(state);
        settings[AmiModuleSettingsStorage.StateKey] = JsonSerializer.SerializeToNode(state, _json);
        module.Id = _moduleId!;
        module.SetSettings(settings.ToJsonString());
        await AmiModuleSettingsStorage.SavePropertiesAsync(db, module, cancellationToken);
        RefreshState(module.GetSettings());
        return state;
    }

    /// <summary>Rejects settings operations when the module has no persistent identity.</summary>
    private void EnsurePersistent()
    {
        if (_database == null || _moduleId == null)
        {
            throw new NotSupportedException("Persistent module settings are unavailable.");
        }
    }

    /// <summary>Refreshes reserved host state while preserving the generation's existing configuration values.</summary>
    /// <param name="persisted">Authoritative persisted configuration supplying reserved host state.</param>
    private void RefreshState(string? persisted)
    {
        lock (_snapshotLock)
        {
            Volatile.Write(ref _value, AmiModuleSettingsStorage.PreserveState(_value, persisted));
        }
    }
}
