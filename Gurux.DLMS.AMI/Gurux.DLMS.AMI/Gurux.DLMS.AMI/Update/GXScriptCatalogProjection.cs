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

using System.Reflection;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Gurux.Updater.Model;
using NuGet.Versioning;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Projects provenance and cached releases for Updates without network requests.</summary>
public static class GXScriptCatalogProjection
{
    /// <summary>Host product version used by the SDK AMI compatibility check.</summary>
    public static string AmiVersion => typeof(GXScriptCatalogProjection).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(GXScriptCatalogProjection).Assembly.GetName().Version!.ToString();

    /// <summary>Returns compatible releases and each installed instance's current source-change state.</summary>
    public static async Task<List<GXAvailableUpdate>> ReadAsync(GXDbConnection db, CatalogUpdaterOptions options, CancellationToken token = default)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<GXScriptTemplateRelease>())
        {
            return [];
        }
        var rows = await db.SelectAsync<GXScriptTemplateRelease>(GXSelectArgs.SelectAll<GXScriptTemplateRelease>(r => r.Enabled), token);
        var scripts = schema.TableExist<GXScript>()
            ? await db.SelectAsync<GXScript>(GXSelectArgs.Select<GXScript>(s => new { s.Id, s.Name, s.CatalogId, s.CatalogVersion, s.SourceCode, s.CatalogSourceCode }, s => s.Removed == null), token) : [];
        var result = new List<GXAvailableUpdate>();
        foreach (var group in rows.GroupBy(r => r.CatalogId, StringComparer.OrdinalIgnoreCase))
        {
            var manifests = group.Select(r => JsonSerializer.Deserialize<GXScriptManifest>(r.ManifestJson)!).Where(m =>
                NuGetVersion.TryParse(AmiVersion, out var host) && VersionRange.Parse(m.AmiVersionRange).Satisfies(host)).ToList();
            var releases = manifests.Select(m => new GXAvailableRelease { Version = NuGetVersion.Parse(m.Version).ToNormalizedString(),
                Prerelease = NuGetVersion.Parse(m.Version).IsPrerelease, ReleaseNotes = m.Description }).OrderByDescending(r => NuGetVersion.Parse(r.Version)).ToList();
            var latest = releases.FirstOrDefault(r => options.IncludePrereleases || !r.Prerelease);
            if (latest == null)
            {
                continue;
            }
            var installed = scripts.Where(s => string.Equals(s.CatalogId, group.Key, StringComparison.OrdinalIgnoreCase)).ToList();
            if (installed.Count == 0)
            {
                result.Add(Create(null));
            }
            else
            {
                foreach (var script in installed)
                {
                    result.Add(Create(script));
                }
            }

            GXAvailableUpdate Create(GXScript? script) => new() {
                Id = script?.Id.ToString() ?? "script:" + group.Key, CatalogId = group.Key, ScriptId = script?.Id,
                Name = script?.Name ?? group.Key, Description = manifests.First(m => NuGetVersion.Parse(m.Version) == NuGetVersion.Parse(latest.Version)).Description,
                InstalledVersion = script?.CatalogVersion, AvailableVersion = latest.Version, Releases = releases,
                IncludePrereleases = options.IncludePrereleases, Customized = script != null && script.SourceCode != script.CatalogSourceCode,
                Status = script == null ? GXUpdateStatus.Available :
                    NuGetVersion.TryParse(script.CatalogVersion, out var current) && NuGetVersion.Parse(latest.Version) > current ? GXUpdateStatus.UpdateAvailable : GXUpdateStatus.UpToDate
            };
        }
        return [.. result.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
