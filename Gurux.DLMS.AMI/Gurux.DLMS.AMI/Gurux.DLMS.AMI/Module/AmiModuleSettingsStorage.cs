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

using System.Collections.Concurrent;
using System.Data;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Coordinates configuration and server-state writes to module settings.</summary>
internal static class AmiModuleSettingsStorage
{
    internal const string StateKey = "$state";
    internal const string ResourcesKey = "$resources";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    internal static async Task<IDisposable> LockAsync(string moduleId, CancellationToken token)
    {
        var gate = Gates.GetOrAdd(moduleId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token);
        return new Releaser(gate);
    }

    internal static JsonObject Parse(string? settings) => string.IsNullOrWhiteSpace(settings)
        ? [] : JsonNode.Parse(settings) as JsonObject
            ?? throw new InvalidOperationException("Module settings must be a JSON object.");

    /// <summary>Ordinary edits may omit the schema marker; retain its persisted version without inventing one for legacy data.</summary>
    internal static string? PreserveSchemaVersion(string? incoming, string? stored)
    {
        var persisted = Parse(stored);
        if (!persisted.ContainsKey(AmiModuleSettingsSchema.VersionKey))
        {
            return incoming;
        }
        var settings = Parse(incoming);
        if (settings.ContainsKey(AmiModuleSettingsSchema.VersionKey))
        {
            return incoming;
        }
        settings[AmiModuleSettingsSchema.VersionKey] = persisted[AmiModuleSettingsSchema.VersionKey]?.DeepClone();
        return settings.ToJsonString();
    }

    /// <summary>Only the host can replace reserved state; omitted schema metadata retains its persisted version.</summary>
    internal static string? PreserveState(string? incoming, string? stored)
    {
        incoming = PreserveSchemaVersion(incoming, stored);
        var persisted = Parse(stored);
        if (persisted[StateKey] == null && persisted[ResourcesKey] == null && string.IsNullOrWhiteSpace(incoming))
        {
            return incoming;
        }
        var settings = Parse(incoming);
        foreach (var key in new[] { StateKey, ResourcesKey })
        {
            settings.Remove(key);
            if (persisted[key] != null)
            {
                settings[key] = persisted[key]!.DeepClone();
            }
        }
        return settings.ToJsonString();
    }

    internal static async Task<GXModule?> ReadModuleAsync(GXDbConnection connection, string moduleId,
        CancellationToken token, IDbTransaction? transaction = null)
    {
        var args = GXSelectArgs.Select<GXModule>(m => new { m.Id }, m => m.Id == moduleId);
        args.Columns.Add<GXModule>();
        args.Columns.Add<GXModuleProperty>();
        args.Columns.Exclude<GXModuleProperty>(p => p.Module!);
        args.Joins.AddLeftJoin<GXModule, GXModuleProperty>(j => j.Id, j => j.Module!);
        var module = await connection.SingleOrDefaultAsync<GXModule>(transaction, args, token);
        if (module != null) {
            module.Properties ??= [];
        }
        return module;
    }

    internal static async Task SavePropertiesAsync(GXDbConnection connection, GXModule module,
        CancellationToken token, IDbTransaction? transaction = null)
    {
        if (module.Properties == null) {
            return;
        }
        if (transaction == null)
        {
            await using var owned = await connection.BeginTransactionAsync(token);
            await SavePropertiesAsync(connection, module, token, owned);
            await owned.CommitAsync(token);
            return;
        }
        var id = module.Id;
        var properties = module.Properties;
        if (properties.Any(p => string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 64) ||
            properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Count)
            {
            throw new ArgumentException("Module property names must be unique and contain 1 to 64 characters.");
        }
        var stored = (await ReadModuleAsync(connection, id, token, transaction))?.Properties ?? [];
        foreach (var previous in stored.Where(p => !properties.Any(v => v.Name == p.Name && v.Removed == null)))
            {
            await connection.DeleteAsync(transaction,
                GXDeleteArgs.Delete<GXModuleProperty>(p => p.Id == previous.Id), token);
        }
        foreach (var property in properties.Where(p => p.Removed == null))
        {
            var previous = stored.SingleOrDefault(p => p.Name == property.Name);
            property.Module = new GXModule(id);
            if (previous == null)
            {
                property.Id = Guid.NewGuid();
                property.CreationTime = DateTimeOffset.UtcNow;
                property.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(property), token);
            }
            else
            {
                if (property.ConcurrencyStamp != null && property.ConcurrencyStamp != previous.ConcurrencyStamp)
                    {
                    throw new InvalidOperationException("The module property was edited by another operation.");
                }
                property.Id = previous.Id;
                property.CreationTime = previous.CreationTime;
                property.Updated = DateTimeOffset.UtcNow;
                property.ConcurrencyStamp = previous.ConcurrencyStamp;
                var update = GXUpdateArgs.Update(property, p => new { p.Value, p.Updated, p.Removed });
                var expectedVersion = previous.ConcurrencyStamp;
                update.Where.And<GXModuleProperty>(p => p.ConcurrencyStamp == expectedVersion);
                if (await connection.UpdateAsync(transaction, update, token) != 1)
                    {
                    throw new InvalidOperationException("The module property was edited by another operation.");
                }
            }
        }
    }
    private sealed class Releaser(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
