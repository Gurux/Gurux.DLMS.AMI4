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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.Service.Orm.Enums;
using CatalogDbModule = Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule;
using CatalogDbAgent = Gurux.DLMS.AMI.Shared.DTOs.Agent.GXAgent;
using Gurux.DLMS.AMI.Shared.DTOs.Update;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Gurux.Updater.Model;
namespace Gurux.DLMS.AMI.Update;

/// <summary>Provides catalog store.</summary>
/// <param name="database">The database.</param>
/// <param name="options">The options.</param>
/// <param name="notifications">The notifications.</param>
/// <param name="logger">The logger.</param>
/// <param name="iconClient">Optional HTTP transport for catalog module icons.</param>
public sealed class GXCatalogStore(IGXDatabase database, CatalogUpdaterOptions? options = null, NotificationCache? notifications = null, ILogger<GXCatalogStore>? logger = null, HttpClient? iconClient = null)
{
    /// <summary>Performs the upsert localizations operation.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="source">The source.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task UpsertLocalizationsAsync(Gurux.Updater.Model.GXLocalizationCatalog catalog, string source, CancellationToken token)
    {
        await _writes.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            var schema = new GXSchemaManager(db);
            if (!schema.TableExist<Gurux.DLMS.AMI.Shared.DTOs.GXLocalization>())
            {
                schema.CreateTable<Gurux.DLMS.AMI.Shared.DTOs.GXLocalization>(false, false);
            }

            using var transaction = db.BeginTransaction();
            var rows = await db.SelectAsync<Gurux.DLMS.AMI.Shared.DTOs.GXLocalization>(transaction, GXSelectArgs.SelectAll<Gurux.DLMS.AMI.Shared.DTOs.GXLocalization>(), token);
            foreach (var row in rows)
            {
                row.Enabled = false;
            }

            foreach (var owner in catalog.Items)
            {
                foreach (var release in owner.Releases)
                {
                    token.ThrowIfCancellationRequested();
                    var row = rows.FirstOrDefault(r => r.Owner == owner.Owner && r.OwnerType == owner.OwnerType && r.Version == release.Version);
                    bool added = row == null;
                    row ??= new Gurux.DLMS.AMI.Shared.DTOs.GXLocalization
                    {
                        Id = Guid.NewGuid(),
                        Owner = owner.Owner,
                        OwnerType = owner.OwnerType,
                        Version = release.Version
                    };
                    row.Cultures = string.Join(",", release.Cultures);
                    row.OwnerVersionRange = release.OwnerVersionRange;
                    row.DefaultCulture = release.DefaultCulture;
                    row.Description = release.Description ?? owner.Description;
                    row.ReleaseNotes = release.ReleaseNotes;
                    row.Url = new Uri(new Uri(source), release.Path).AbsoluteUri;
                    row.Sha256 = release.Sha256;
                    row.Size = release.Size;
                    row.Enabled = true;
                    row.Updated = DateTimeOffset.UtcNow;
                    if (added)
                    {
                        await db.InsertAsync(transaction, GXInsertArgs.Insert(row), token);
                        rows.Add(row);
                    }
                }
            }

            if (rows.Count != 0)
            {
                await db.UpdateAsync(transaction, GXUpdateArgs.UpdateRange(rows), token);
            }

            transaction.Commit();
            await NotifyAsync(db, "configuration.view", "configuration.edit", "localization", "Localization", token);
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <summary>Serializes writes so persisted state cannot be overwritten concurrently.</summary>
    static readonly SemaphoreSlim _writes = new(1, 1);
    /// <summary>Resolves the configured catalog owner or selects an existing administrator account.</summary>
    /// <param name="db">The open database connection used to read or persist update state.</param>
    /// <param name="tx">The caller's database transaction.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task containing the user that owns synchronized catalog records.</returns>
    /// <exception cref="InvalidOperationException">No configured catalog owner or eligible administrator exists.</exception>
    async Task<GXUser> CreatorAsync(GXDbConnection db, System.Data.IDbTransaction tx, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(options?.CreatorId))
        {
            return await db.SingleOrDefaultAsync<GXUser>(tx, GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == options.CreatorId && u.Removed == null), token) ?? throw new InvalidOperationException("Updater CreatorId must identify an existing user.");
        }

        if (!new GXSchemaManager(db).TableExist(tx, typeof(GXUserRole)))
        {
            throw new InvalidOperationException("Configure Updater:CreatorId with an existing catalog owner.");
        }

        var query = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
        query.Joins.AddInnerJoin<GXUser, GXUserRole>(u => u.Id!, r => r.UserId!);
        query.Joins.AddInnerJoin<GXUserRole, GXRole>(r => r.RoleId!, r => r.Id);
        query.Where.And<GXRole>(r => r.Name == Gurux.DLMS.AMI.Client.Shared.GXRoles.Admin && r.Removed == null);
        query.OrderBy.Add<GXUser>(u => u.Id!);
        return (await db.SelectAsync<GXUser>(tx, query, token)).FirstOrDefault() ?? throw new InvalidOperationException("Configure Updater:CreatorId with an existing catalog owner.");
    }
    /// <summary>Selects the highest stable catalog version that has a downloadable release URL.</summary>
    /// <param name="versions">The catalog releases to inspect for a stable downloadable version.</param>
    /// <returns>The highest eligible release number, or null when no stable downloadable release exists.</returns>
    static string? Stable(IEnumerable<CatalogRelease> versions) => versions.Where(v => !v.Prerelease && !string.IsNullOrWhiteSpace(v.Url)).OrderByDescending(v => v.Number, GXReleaseVersionComparer.Instance).FirstOrDefault()?.Number;
    /// <summary>Creates the catalog-to-local-product mapping table if the installation does not yet contain it.</summary>
    /// <param name="db">The open database connection used to read or persist update state.</param>
    static void EnsureMapping(GXDbConnection db)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<GXCatalogProduct>())
        {
            schema.CreateTable<GXCatalogProduct>(false, false);
        }
    }
    /// <summary>Retrieves the persisted local-product mapping for a catalog identity inside the caller's transaction.</summary>
    /// <param name="db">The open database connection used to read or persist update state.</param>
    /// <param name="tx">The caller's database transaction.</param>
    /// <param name="key">The lookup key identifying the record or cached value.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task containing the mapping, or null when the catalog identity has not been mapped.</returns>
    static async Task<GXCatalogProduct?> MappingAsync(GXDbConnection db, System.Data.IDbTransaction tx, string key, CancellationToken token) => await db.SingleOrDefaultAsync<GXCatalogProduct>(tx, GXSelectArgs.SelectAll<GXCatalogProduct>(p => p.Id == key), token);
    /// <summary>Truncates optional text to the configured maximum length.</summary>
    /// <param name="value">Input value to inspect or transform.</param>
    /// <param name="max">Maximum number of characters to retain.</param>
    /// <returns>The original text or its prefix limited to the requested length.</returns>
    static string? Text(string? value, int max) => value?.Length > max ? value[..max] : value;
    /// <summary>Performs the upsert module operation.</summary>
    /// <param name="item">The item.</param>
    /// <param name="releases">The releases.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task UpsertModuleAsync(GXUpdateCatalogItem item, IReadOnlyList<CatalogRelease> releases, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(item.Id) || item.Id.Length > 64)
        {
            throw new InvalidDataException("Catalog module ID must contain1–64 characters.");
        }

        string? icon = null;
        if (!string.IsNullOrWhiteSpace(item.IconUrl))
        {
            try
            {
                icon = await GXCatalogModuleIcon.ReadAsync(item.IconUrl, iconClient ?? GXCatalogModuleIcon.Client, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or OperationCanceledException)
            {
                logger?.LogWarning(error, "Catalog icon for module {Id} could not be refreshed; preserving the saved icon", item.Id);
            }
        }

        await _writes.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureMapping(db);
            using var tx = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var key = "module:" + item.Id.ToUpperInvariant();
            var mapping = await MappingAsync(db, tx, key, token);
            var moduleId = mapping?.EntityId ?? item.Id;
            var module = await db.SingleOrDefaultAsync<CatalogDbModule>(tx, GXSelectArgs.SelectAll<CatalogDbModule>(m => m.Id == moduleId), token);
            if (module == null)
            {
                module = new CatalogDbModule(moduleId) { Name = Text(item.Name ?? item.Id, 64), Description = Text(item.Description, 256), Creator = await CreatorAsync(db, tx, token), Enabled = false, Status = ModuleStatus.Installable, CreationTime = DateTimeOffset.UtcNow };
                var insert = GXInsertArgs.Insert(module);
                insert.Exclude<CatalogDbModule>(m => new { m.Versions, m.Assemblies, m.Scripts });
                await db.InsertAsync(tx, insert, token);
            }
            if (mapping == null)
            {
                await db.InsertAsync(tx, GXInsertArgs.Insert(new GXCatalogProduct { Id = key, EntityId = module.Id }), token);
            }

            var old = await db.SelectAsync<GXModuleVersion>(tx, GXSelectArgs.SelectAll<GXModuleVersion>(v => v.Module == module), token);
            // A release no longer in the catalog has no current compatibility approval.
            foreach (var stale in old.Where(v => !releases.Any(r => GXReleaseVersionComparer.TryNormalize(v.Number, out var n) && n == r.Number)))
            {
                stale.Enabled = false;
                stale.Url = null;
                await db.UpdateAsync(tx, GXUpdateArgs.Update(stale, v => new { v.Enabled, v.Url }), token);
            }
            foreach (var release in releases)
            {
                var version = old.FirstOrDefault(v => GXReleaseVersionComparer.TryNormalize(v.Number, out var n) && n == release.Number);
                if (version == null)
                {
                    version = new GXModuleVersion { Id = Guid.NewGuid(), Module = module, Number = release.Number };
                    old.Add(version);
                    Apply(version, release);
                    await db.InsertAsync(tx, GXInsertArgs.Insert(version), token);
                }
                else
                {
                    Apply(version, release);
                    await db.UpdateAsync(tx, GXUpdateArgs.Update(version, v => new { v.Number, v.Prerelease, v.CreationTime, v.Description, v.FileName, v.Url, v.Enabled }), token);
                }
            }
            module.AvailableVersion = GXCatalogReleaseSelector.Latest(old.Where(v => v.Enabled != false)
                .Select(v => new CatalogRelease(v.Number!, v.Prerelease, v.CreationTime, v.Description, v.FileName, v.Url)),
                options?.IncludePrereleases == true)?.Number;
            module.NewVersion = module.AvailableVersion != null && GXReleaseVersionComparer.Instance.Compare(module.AvailableVersion, module.ReleaseVersion ?? module.Version) > 0;
            module.Icon = icon ?? module.Icon;
            module.Updated = DateTimeOffset.UtcNow;
            await db.UpdateAsync(tx, GXUpdateArgs.Update(module, m => new { m.AvailableVersion, m.NewVersion, m.Icon, m.Updated }), token);
            tx.Commit();
            await NotifyAsync(db, "module.view", GXCatalogNotifications.ModuleUpdated, module.Id, module.Name, token);
        }
        finally
        {
            _writes.Release();
        }
    }
    /// <summary>Copies catalog release metadata into a local module-version record.</summary>
    /// <param name="version">The local module-version record receiving the catalog metadata.</param>
    /// <param name="release">The catalog release whose download and publication metadata are copied.</param>
    static void Apply(GXModuleVersion version, CatalogRelease release)
    {
        version.Number = release.Number;
        version.Prerelease = release.Prerelease;
        version.CreationTime = release.PublishedAt;
        version.Description = release.Description;
        version.FileName = release.FileName;
        version.Url = release.Url;
        version.Enabled = release.Url != null;
    }
    /// <summary>Performs the upsert agent operation.</summary>
    /// <param name="item">The item.</param>
    /// <param name="releases">The releases.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task UpsertAgentAsync(GXUpdateCatalogItem item, IReadOnlyList<CatalogRelease> releases, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(item.Id) || item.Id.Length > 128)
        {
            throw new InvalidDataException("Catalog agent ID must contain1–128 characters.");
        }

        await _writes.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureMapping(db);
            using var tx = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var key = "agent:" + item.Id.ToUpperInvariant();
            var mapping = await MappingAsync(db, tx, key, token);
            var agent = await db.SingleOrDefaultAsync<CatalogDbAgent>(tx, GXSelectArgs.SelectAll<CatalogDbAgent>(a => a.CatalogId == item.Id && a.Template == true), token);
            if (agent == null)
            {
                agent = new CatalogDbAgent { Id = Guid.NewGuid(), CatalogId = item.Id, Name = Text(item.Name ?? item.Id, 64), Description = Text(item.Description, 256), Creator = await CreatorAsync(db, tx, token), Template = true, CreationTime = DateTimeOffset.UtcNow };
                var insert = GXInsertArgs.Insert(agent);
                insert.Exclude<CatalogDbAgent>(a => new { a.Versions, a.Tasks });
                await db.InsertAsync(tx, insert, token);
            }
            if (mapping == null)
            {
                await db.InsertAsync(tx, GXInsertArgs.Insert(new GXCatalogProduct { Id = key, EntityId = agent.Id.ToString() }), token);
            }
            else if (mapping.EntityId != agent.Id.ToString())
            {
                throw new InvalidDataException("Catalog agent identity changed unexpectedly.");
            }

            var old = await db.SelectAsync<GXAgentVersion>(tx, GXSelectArgs.SelectAll<GXAgentVersion>(v => v.Agent == agent), token);
            foreach (var stale in old.Where(v => !releases.Any(r => GXReleaseVersionComparer.TryNormalize(v.Number, out var n) && n == r.Number)))
            {
                stale.Enabled = false;
                stale.Url = null;
                await db.UpdateAsync(tx, GXUpdateArgs.Update(stale, v => new { v.Enabled, v.Url }), token);
            }
            foreach (var release in releases)
            {
                var version = old.FirstOrDefault(v => GXReleaseVersionComparer.TryNormalize(v.Number, out var n) && n == release.Number);
                bool added = version == null;
                version ??= new GXAgentVersion { Id = Guid.NewGuid(), Agent = agent };
                version.Number = release.Number;
                version.Prerelease = release.Prerelease;
                version.CreationTime = release.PublishedAt;
                version.Description = release.Description;
                version.FileName = release.FileName;
                version.Url = release.Url;
                version.Enabled = release.Url != null;
                if (added)
                {
                    old.Add(version);
                    await db.InsertAsync(tx, GXInsertArgs.Insert(version), token);
                }
                else
                {
                    await db.UpdateAsync(tx, GXUpdateArgs.Update(version, v => new { v.Number, v.Prerelease, v.CreationTime, v.Description, v.FileName, v.Url, v.Enabled }), token);
                }
            }
            agent.AvailableVersion = Stable(old.Where(v => v.Enabled != false).Select(v => new CatalogRelease(v.Number!, v.Prerelease == true, v.CreationTime, v.Description, v.FileName, v.Url)));
            agent.Updated = DateTimeOffset.UtcNow;
            await db.UpdateAsync(tx, GXUpdateArgs.Update(agent, a => new { a.AvailableVersion, a.Updated }), token);
            tx.Commit();
            await NotifyAsync(db, "agent.view", "agent.edit", agent.Id.ToString(), agent.Name, token);
        }
        finally
        {
            _writes.Release();
        }
    }
    /// <summary>Notifies authorized users about committed catalog changes without rolling them back if notification delivery fails.</summary>
    /// <param name="db">The open database connection used to read or persist update state.</param>
    /// <param name="permission">The policy identifying users who receive the catalog-change notification.</param>
    /// <param name="action">The hub action used to describe the committed catalog change.</param>
    /// <param name="id">The identifier of the catalog product that changed.</param>
    /// <param name="name">The optional product display name included in the notification.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task that completes after notifications have been queued or a non-cancellation failure has been logged.</returns>
    async Task NotifyAsync(GXDbConnection db, string permission, string action, string id, string? name, CancellationToken token)
    {
        if (notifications == null)
        {
            return;
        }

        try
        {
            var query = GXQuery.GetUsersByPermission(NotificationAction.Edit, permission);
            var users = await db.SelectAsync<GXUser>(query, token);
            notifications.Add([.. users.Where(u => u.Id != null).Select(u => u.Id!)], new GXTarget(action, id, name));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        // Catalog data is already committed; notification failure must not undo version synchronization.
        catch (Exception error)
        {
            logger?.LogWarning(error, "Catalog notification for {Id} failed after synchronization committed", id);
        }
    }
    /// <summary>Performs the mark checked operation.</summary>
    /// <param name="name">The name.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task MarkCheckedAsync(string name, CancellationToken token)
    {
        await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
        if (!new GXSchemaManager(db).TableExist<GXConfiguration>())
        {
            return;
        }

        foreach (var configuration in await db.SelectAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == name), token))
        {
            var settings = string.IsNullOrWhiteSpace(configuration.Settings) ? [] : JsonNode.Parse(configuration.Settings)?.AsObject() ?? [];
            settings["Checked"] = DateTimeOffset.UtcNow;
            configuration.Settings = settings.ToJsonString();
            await db.UpdateAsync(GXUpdateArgs.Update(configuration, c => c.Settings!), token);
            await NotifyAsync(db, GXConfigurationPolicies.View,
                name == GXConfigurations.Modules ? GXCatalogNotifications.ConfigurationChecked : GXConfigurationPolicies.Edit,
                configuration.Id.ToString(), configuration.Name, token);
        }
    }
}







