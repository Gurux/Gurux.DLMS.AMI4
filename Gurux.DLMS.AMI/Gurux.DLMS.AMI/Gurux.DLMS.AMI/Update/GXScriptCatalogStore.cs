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
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Gurux.Updater.Model;
using Gurux.Updater.Services;
using NuGet.Versioning;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Caches immutable manifests without downloading or modifying executable script source.</summary>
public sealed class GXScriptCatalogStore(IGXDatabase database)
{
    private static readonly SemaphoreSlim Writes = new(1, 1);

    /// <summary>Publishes a complete validated index; withdrawn versions remain retained but disabled.</summary>
    public async Task SynchronizeAsync(string source, GXScriptCatalog catalog, CancellationToken token = default)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || !GXCatalogUpdateService.IsSupportedUri(uri))
        {
            throw new InvalidDataException("Script index must use HTTPS or loopback HTTP.");
        }
        if (catalog.SchemaVersion != 1)
        {
            throw new InvalidDataException("Unsupported script catalog schema.");
        }
        foreach (var manifest in catalog.Items)
        {
            GXScriptService.Validate(manifest);
        }

        if (source.Length > 2048 || catalog.Items.Any(m => NuGetVersion.Parse(m.Version).ToNormalizedString().Length > 64))
        {
            throw new InvalidDataException("Script index URL or version exceeds the storage limit.");
        }
        if (catalog.Items.GroupBy(m => m.Id.ToUpperInvariant() + "/" + NuGetVersion.Parse(m.Version).ToNormalizedString()).Any(g => g.Count() != 1))
        {
            throw new InvalidDataException("Duplicate script release identity.");
        }
        await Writes.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            var schema = new GXSchemaManager(db);
            if (!schema.TableExist<GXScriptTemplateRelease>())
            {
                schema.CreateTable<GXScriptTemplateRelease>(false, false);
            }
        
            using var transaction = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var rows = await db.SelectAsync<GXScriptTemplateRelease>(transaction, GXSelectArgs.SelectAll<GXScriptTemplateRelease>(), token);
            foreach (var row in rows)
            {
                row.Enabled = false;
            }
        
            foreach (var manifest in catalog.Items)
            {
                var version = NuGetVersion.Parse(manifest.Version).ToNormalizedString();
                var row = rows.SingleOrDefault(r => r.CatalogId.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase) && r.Version == version);
                var json = JsonSerializer.Serialize(manifest);
                if (row != null && row.ManifestJson != json)
                {
                    throw new InvalidDataException($"Script release {manifest.Id}/{version} was changed after publication.");
                }
        
                if (row == null)
                {
                    row = new GXScriptTemplateRelease
                    {
                        Id = Guid.NewGuid(),
                        CatalogId = manifest.Id,
                        Version = version,
                        ManifestJson = json
                    };
                    rows.Add(row);
                    row.CatalogSource = source;
                    row.Enabled = true;
                    await db.InsertAsync(transaction, GXInsertArgs.Insert(row), token);
                }
                else
                {
                    row.CatalogSource = source;
                    row.Enabled = true;
                }
            }
        
            foreach (var row in rows)
            {
                await db.UpdateAsync(transaction, GXUpdateArgs.Update(row, r => new { r.Enabled, r.CatalogSource }), token);
            }
        
            transaction.Commit();
        }
        finally
        {
            Writes.Release();
        }
    }

    /// <summary>Resolves a retained enabled release by identity, never a client-provided URL.</summary>
    public async Task<GXScriptTemplateRelease> GetAsync(string catalogId, string version, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(catalogId) || !NuGetVersion.TryParse(version, out var parsed))
        {
            throw new ArgumentException("Select a script and version.");
        }
        var normalized = parsed.ToNormalizedString();
        await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
        return await db.SingleOrDefaultAsync<GXScriptTemplateRelease>(GXSelectArgs.SelectAll<GXScriptTemplateRelease>(r => r.CatalogId == catalogId && r.Version == normalized && r.Enabled), token)
            ?? throw new InvalidOperationException("The selected script release is unavailable. Refresh Updates.");
    }
}
