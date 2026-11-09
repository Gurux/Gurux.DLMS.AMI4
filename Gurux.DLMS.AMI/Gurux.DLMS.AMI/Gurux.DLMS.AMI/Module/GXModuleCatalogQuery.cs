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
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module catalog query.</summary>
/// <param name="database">The database.</param>
public sealed class GXModuleCatalogQuery(IGXDatabase database)
{
    /// <summary>Performs the project operation.</summary>
    /// <param name="authorized">The authorized.</param>
    /// <param name="includePrereleases">The include prereleases.</param>
    /// <param name="latestFilter">The latest filter.</param>
    /// <param name="index">The index.</param>
    /// <param name="count">The count.</param>
    /// <param name="orderBy">The order by.</param>
    /// <param name="descending">The descending.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task<ListModulesResponse> ProjectAsync(IEnumerable<GXModule> authorized, bool includePrereleases, string? latestFilter, long index, long count, string? orderBy, bool descending, CancellationToken token)
    {
        if (index < 0 || count < 0)
        {
            throw new ArgumentException("Invalid module paging.");
        }

        var modules = authorized.ToArray();
        // Select parent IDs explicitly; the ORM does not materialize navigation IDs without a join.
        var ret = new ListModulesResponse();
        var visible = new List<GXModule>();
        foreach (var module in modules)
        {
            var rows = await database.SelectAsync<GXModuleVersion>(v => new { v.Id, v.Number, v.Prerelease, v.Url, v.FileName, v.Enabled }, v => v.Module == module, token);
            var releases = rows.Where(v => v.Number != null && (includePrereleases || !v.Prerelease)).OrderByDescending(v => v.Number, GXReleaseVersionComparer.Instance).Select(v => new ModuleReleaseInfo(v.Id, v.Number!, v.Prerelease, v.Enabled != false && !string.IsNullOrWhiteSpace(v.Url) && !string.IsNullOrWhiteSpace(v.FileName))).ToList();
            if (releases.Count == 0 && (module.Status.GetValueOrDefault() &
                (Gurux.DLMS.AMI.Shared.DTOs.Enums.ModuleStatus.Installed | Gurux.DLMS.AMI.Shared.DTOs.Enums.ModuleStatus.CustomBuild)) == 0)
            {
                continue;
            }

            var latest = releases.FirstOrDefault(v => v.Installable)?.Number;
            if (!string.IsNullOrWhiteSpace(latestFilter) && !(latest ?? "").Contains(latestFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            visible.Add(new GXModule(module.Id) { Name = module.Name, Icon = module.Icon, Description = module.Description, Version = module.ReleaseVersion ?? module.Version, AvailableVersion = latest, Status = module.Status, Enabled = module.Enabled, RuntimeState = module.RuntimeState, CreationTime = module.CreationTime, Updated = module.Updated, NewVersion = module.NewVersion });
            ret.CatalogVersions[module.Id] = releases;
        }
        IEnumerable<GXModule> sorted = visible;
        if (string.Equals(orderBy, nameof(GXModule.AvailableVersion), StringComparison.OrdinalIgnoreCase))
        {
            sorted = descending ? visible.OrderByDescending(m => m.AvailableVersion, GXReleaseVersionComparer.Instance) : visible.OrderBy(m => m.AvailableVersion, GXReleaseVersionComparer.Instance);
        }

        ret.Count = visible.Count;
        var skipped = sorted.Skip((int)Math.Min(index, visible.Count));
        var page = count == 0 ? skipped : skipped.Take((int)Math.Min(count, visible.Count));
        ret.Modules = [.. page];
        var selected = ret.Modules.Select(m => m.Id).ToHashSet();
        ret.CatalogVersions = ret.CatalogVersions.Where(p => selected.Contains(p.Key)).ToDictionary();
        return ret;
    }
}



