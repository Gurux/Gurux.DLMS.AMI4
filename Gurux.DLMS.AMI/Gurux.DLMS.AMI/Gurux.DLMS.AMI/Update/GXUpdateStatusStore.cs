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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Server.Module;
using UpdateDbModule = Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule;
using UpdateDbAgent = Gurux.DLMS.AMI.Shared.DTOs.Agent.GXAgent;
using Gurux.DLMS.AMI.Shared.DTOs.Update;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using CatalogProductType = Gurux.Updater.Enums.CatalogProductType;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Stores the last successful complete check; reads never contact update feeds.</summary>
public sealed class GXUpdateStatusStore(IGXDatabase database, CatalogUpdaterOptions options)
{
    const string ConfigurationName = "updates";
    static readonly SemaphoreSlim Writes = new(1, 1);

    static string? CoreVersion()
    {
        var assembly = typeof(GXUpdateStatusStore).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? assembly.GetName().Version?.ToString() : version;
    }

    static GXUpdateStatus Status(string? installed, string? available)
    {
        if (!GXReleaseVersionComparer.TryNormalize(installed, out var local) ||
            !GXReleaseVersionComparer.TryNormalize(available, out var remote))
        {
            return GXUpdateStatus.Unknown;
        }

        return GXReleaseVersionComparer.Instance.Compare(remote, local) > 0 ? GXUpdateStatus.UpdateAvailable : GXUpdateStatus.UpToDate;
    }

    GXAvailableUpdate Core(GXUpdateCatalog? catalog)
    {
        var item = catalog?.Items.FirstOrDefault(i => i.Type == CatalogProductType.Application &&
            string.Equals(i.Id, options.CoreProductId, StringComparison.OrdinalIgnoreCase));
        var release = StableRelease(item);
        var installed = CoreVersion();
        return new GXAvailableUpdate
        {
            Id = options.CoreProductId,
            Name = item?.Name ?? "Gurux DLMS AMI",
            InstalledVersion = installed,
            AvailableVersion = Normalized(release?.Version),
            ReleaseNotesUrl = SafeUrl(release?.ReleaseUrl),
            PublishedAt = release?.PublishedAt,
            Releases = item == null ? [] : GXCatalogReleaseSummary.Create(item),
            Status = Status(installed, release?.Version)
        };
    }

    static GXUpdateRelease? StableRelease(GXUpdateCatalogItem? item)
    {
        if (item == null)
        {
            return null;
        }
        var assemblies = GXCatalogCompatibility.HostAssemblies();
        return item.Releases.Where(r => GXCatalogCompatibility.IsCompatible(item.Type, r, assemblies))
            .Where(r => !r.IsPrerelease && GXReleaseVersionComparer.TryNormalize(r.Version, out var version) && !GXReleaseVersionComparer.IsPrerelease(version))
            .OrderByDescending(r => r.Version, GXReleaseVersionComparer.Instance).FirstOrDefault();
    }
    static string? Normalized(string? version) => GXReleaseVersionComparer.TryNormalize(version, out var normalized) ? normalized : null;
    static string? SafeUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && GXCatalogUpdateService.IsSupportedUri(uri) ? value : null;

    static async Task<List<UpdateDbModule>> InstalledModulesAsync(GXDbConnection db, CancellationToken token)
    {
        if (!new GXSchemaManager(db).TableExist<UpdateDbModule>())
        {
            return [];
        }

        var modules = await db.SelectAsync<UpdateDbModule>(GXSelectArgs.Select<UpdateDbModule>(m =>
                    new
                    {
                        m.Id,
                        m.Name,
                        m.Version,
                        m.ReleaseVersion,
                        m.Status
                    }), token);
        return [.. modules.Where(m => (m.Status.GetValueOrDefault() & (ModuleStatus.Installed | ModuleStatus.CustomBuild)) != 0).OrderBy(m => m.Name ?? m.Id, StringComparer.OrdinalIgnoreCase)];
    }

    static GXAvailableUpdate Module(UpdateDbModule module, GXAvailableUpdate? cached = null)
    {
        var installed = module.ReleaseVersion ?? module.Version;
        return new GXAvailableUpdate
        {
            Id = module.Id,
            Name = module.Name ?? module.Id,
            InstalledVersion = installed,
            AvailableVersion = cached?.AvailableVersion,
            ReleaseNotesUrl = cached?.ReleaseNotesUrl,
            PublishedAt = cached?.PublishedAt,
            Releases = cached?.Releases ?? [],
            IncludePrereleases = cached?.IncludePrereleases ?? false,
            Status = Status(installed, cached?.AvailableVersion)
        };
    }

    static async Task<List<GXAvailableUpdate>> AgentsAsync(GXDbConnection db, CancellationToken token)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<UpdateDbAgent>()) {
            return [];
        }
        var columns = schema.GetColumns<UpdateDbAgent>();
        if (!columns.Contains("CatalogId", StringComparer.OrdinalIgnoreCase) ||
            !columns.Contains("AvailableVersion", StringComparer.OrdinalIgnoreCase)) {
            return [];
        }
        var agents = await db.SelectAsync<UpdateDbAgent>(
            GXSelectArgs.Select<UpdateDbAgent>(a => new
            {
                a.Id, a.Name, a.Type, a.CatalogId, a.Template, a.Enabled, a.Version, a.AvailableVersion
            }, a => a.Removed == null), token);
        var templates = agents.Where(a => a.Template == true && a.Enabled != false).ToArray();
        return [.. agents.Where(a => a.Template != true).OrderBy(a => a.Name).Select(agent =>
        {
            // Legacy registrations have no catalog identity, just as the agent download
            // API selects the latest installer for them. Explicit identities stay isolated.
            var available = templates.Where(t => string.IsNullOrWhiteSpace(agent.CatalogId)
                    ? t.Type == agent.Type : string.Equals(t.CatalogId, agent.CatalogId, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.AvailableVersion).Append(agent.AvailableVersion)
                .Select(Normalized).Where(v => v != null && !GXReleaseVersionComparer.IsPrerelease(v))
                .OrderByDescending(v => v, GXReleaseVersionComparer.Instance).FirstOrDefault();
            return new GXAvailableUpdate
            {
                Id = agent.Id.ToString(), Name = agent.Name ?? "", InstalledVersion = agent.Version,
                AvailableVersion = available, Status = Status(agent.Version, available)
            };
        })];
    }

    /// <summary>Performs the read operation.</summary>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task<GXAvailableUpdatesResponse> ReadAsync(CancellationToken token = default)
    {
        await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
        GXAvailableUpdatesResponse? snapshot = null;
        DateTimeOffset? checkedAt = null;
        if (new GXSchemaManager(db).TableExist<GXConfiguration>())
        {
            var modules = await db.SingleOrDefaultAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == Gurux.DLMS.AMI.Client.Shared.GXConfigurations.Modules), token);
            if (!string.IsNullOrWhiteSpace(modules?.Settings))
            {
                checkedAt = JsonNode.Parse(modules.Settings)?["Checked"]?.GetValue<DateTimeOffset>();
            }
        
            var configuration = await db.SingleOrDefaultAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == ConfigurationName), token);
            if (!string.IsNullOrWhiteSpace(configuration?.Settings))
            {
                var settings = JsonNode.Parse(configuration.Settings);
                snapshot = settings?["Snapshot"]?.Deserialize<GXAvailableUpdatesResponse>();
                // Only module compatibility depends on the installed host DLLs.
                // Application releases replace the host and remain valid after upgrades.
                if (snapshot != null && settings?["HostAssemblyFingerprint"]?.GetValue<string>() != GXCatalogCompatibility.HostAssemblyFingerprint())
                {
                    snapshot.Modules = [];
                }
            }
        }
        snapshot ??= new GXAvailableUpdatesResponse
        {
            Core = Core(null),
            DeviceProfiles = [new GXAvailableUpdate { Id = options.ManufacturerProductId, Name = "Device profiles", Status = GXUpdateStatus.Unknown }]
        };
        snapshot.Checked ??= checkedAt;
        snapshot.Core.InstalledVersion = CoreVersion();
        snapshot.Core.Status = Status(snapshot.Core.InstalledVersion, snapshot.Core.AvailableVersion);
        var cached = snapshot.Modules.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
        var moduleUpdates = new List<GXAvailableUpdate>();
        foreach (var module in await InstalledModulesAsync(db, token))
        {
            var release = cached.GetValueOrDefault(module.Id);
            // Module catalog checks can refresh installable versions independently of the full snapshot.
            // Reuse those versions when the snapshot is missing or was invalidated by a host upgrade.
            if (string.IsNullOrWhiteSpace(release?.AvailableVersion) && new GXSchemaManager(db).TableExist<Gurux.DLMS.AMI.Shared.DTOs.Module.GXModuleVersion>())
            {
                var versions = await GXModuleVersionStorage.ReadAsync(db, module, token);
                var installable = versions.Where(v => v.Enabled != false && !string.IsNullOrWhiteSpace(v.FileName) &&
                        !string.IsNullOrWhiteSpace(v.Url) && GXReleaseVersionComparer.TryNormalize(v.Number, out _))
                    .Select(v => new CatalogRelease(v.Number!, v.Prerelease || GXReleaseVersionComparer.IsPrerelease(v.Number!),
                        v.CreationTime, v.Description, v.FileName, v.Url)).ToArray();
                var latest = GXCatalogReleaseSelector.Latest(installable, options.IncludePrereleases);
                release = new GXAvailableUpdate
                {
                    AvailableVersion = latest?.Number,
                    PublishedAt = latest?.PublishedAt,
                    IncludePrereleases = options.IncludePrereleases,
                    Releases = [.. installable.OrderByDescending(v => v.Number, GXReleaseVersionComparer.Instance)
                        .Select(v => new GXAvailableRelease
                        {
                            Version = v.Number, Prerelease = v.Prerelease, PublishedAt = v.PublishedAt,
                            ReleaseNotes = v.Description
                        })]
                };
            }
            moduleUpdates.Add(Module(module, release));
        }
        snapshot.Modules = moduleUpdates;
        snapshot.Scripts = await GXScriptCatalogProjection.ReadAsync(db, options, token);
        snapshot.Agents = await AgentsAsync(db, token);
        snapshot.DeviceProfiles = [];
        var schema = new GXSchemaManager(db);
        if (schema.TableExist<Gurux.DLMS.AMI.Shared.DTOs.Manufacturer.GXManufacturer>())
        {
            var columns = schema.GetColumns<Gurux.DLMS.AMI.Shared.DTOs.Manufacturer.GXManufacturer>();
            if (columns.Contains("Version", StringComparer.OrdinalIgnoreCase) && columns.Contains("AvailableVersion", StringComparer.OrdinalIgnoreCase) && columns.Contains("ProfilesInstalled", StringComparer.OrdinalIgnoreCase))
            {
                var manufacturers = await db.SelectAsync<Gurux.DLMS.AMI.Shared.DTOs.Manufacturer.GXManufacturer>(
                    GXSelectArgs.SelectAll<Gurux.DLMS.AMI.Shared.DTOs.Manufacturer.GXManufacturer>(m => m.Template == true && m.Removed == null && m.ProfilesInstalled == true), token);
                snapshot.DeviceProfiles = [.. manufacturers.Where(GXManufacturerRevision.IsUpdateAvailable)
                    .OrderBy(m => m.Name).Select(m => new GXAvailableUpdate
                    {
                        Id = m.Id.ToString(), Name = m.Name ?? "", InstalledVersion = m.Version,
                        AvailableVersion = m.AvailableVersion, Status = GXUpdateStatus.UpdateAvailable
                    })];
            }
        }
        return snapshot;
    }

    /// <summary>Performs the save check operation.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="deviceProfiles">The device profiles.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task SaveCheckAsync(GXUpdateCatalog catalog, GXAvailableUpdate deviceProfiles, CancellationToken token = default)
    {
        await Writes.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            var schema = new GXSchemaManager(db);
            if (!schema.TableExist<GXConfiguration>())
            {
                schema.CreateTable<GXConfiguration>(false, false);
            }

            var mapping = schema.TableExist<GXCatalogProduct>()
                            ? (await db.SelectAsync<GXCatalogProduct>(GXSelectArgs.SelectAll<GXCatalogProduct>(), token)).ToDictionary(p => p.Id, p => p.EntityId, StringComparer.OrdinalIgnoreCase)
                            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var releases = new Dictionary<string, GXAvailableUpdate>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in catalog.Items.Where(i => i.Type == CatalogProductType.Module))
            {
                token.ThrowIfCancellationRequested();
                var selected = await GXCatalogReleaseSelector.SelectAsync(item, options.AssetPatterns.GetValueOrDefault(item.Id) ?? options.DefaultModuleAssetPattern);
                var latest = GXCatalogReleaseSelector.Latest(selected, options.IncludePrereleases);
                var source = latest == null ? null : item.Releases.FirstOrDefault(r => Normalized(r.Version) == latest.Number);
                var id = mapping.GetValueOrDefault("module:" + item.Id) ?? item.Id;
                releases[id] = new GXAvailableUpdate
                {
                    AvailableVersion = latest?.Number, PublishedAt = latest?.PublishedAt,
                    ReleaseNotesUrl = SafeUrl(source?.ReleaseUrl), Releases = GXCatalogReleaseSummary.Create(item, selected),
                    IncludePrereleases = options.IncludePrereleases
                };
            }
            var snapshot = new GXAvailableUpdatesResponse
            {
                Core = Core(catalog),
                DeviceProfiles = [deviceProfiles],
                Modules = [.. (await InstalledModulesAsync(db, token)).Select(m => Module(m, releases.GetValueOrDefault(m.Id)))],
                Scripts = await GXScriptCatalogProjection.ReadAsync(db, options, token)
            };
            using var transaction = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var configuration = await db.SingleOrDefaultAsync<GXConfiguration>(transaction, GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == ConfigurationName), token);
            var added = configuration == null;
            configuration ??= new GXConfiguration { Id = Guid.NewGuid(), Name = ConfigurationName, Enabled = false };
            var settings = string.IsNullOrWhiteSpace(configuration.Settings) ? [] : JsonNode.Parse(configuration.Settings)?.AsObject() ?? [];
            token.ThrowIfCancellationRequested();
            snapshot.Checked = DateTimeOffset.UtcNow;
            settings["Snapshot"] = JsonSerializer.SerializeToNode(snapshot);
            settings["HostAssemblyFingerprint"] = GXCatalogCompatibility.HostAssemblyFingerprint();
            configuration.Settings = settings.ToJsonString();
            if (added)
            {
                await db.InsertAsync(transaction, GXInsertArgs.Insert(configuration), token);
            }
            else
            {
                await db.UpdateAsync(transaction, GXUpdateArgs.Update(configuration, c => c.Settings!), token);
            }

            token.ThrowIfCancellationRequested();
            transaction.Commit();
        }
        finally
        {
            Writes.Release();
        }
    }
}
