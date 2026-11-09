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

using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared;

namespace Gurux.DLMS.AMI.Client.Rest;

public sealed class GXAvailableRelease
{
    public string Version { get; set; } = "";
    public bool Prerelease { get; set; }
    public bool IsSecurityUpdate { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? ReleaseNotesUrl { get; set; }
    public string? ReleaseNotes { get; set; }
}

public enum GXUpdateStatus
{
    Unknown,
    UpToDate,
    UpdateAvailable,
    Available
}

public sealed class GXAvailableUpdate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? InstalledVersion { get; set; }
    public string? AvailableVersion { get; set; }
    public string? ReleaseNotesUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public GXUpdateStatus Status { get; set; }
    public List<GXAvailableRelease> Releases { get; set; } = [];
    public bool IncludePrereleases { get; set; }
    /// <summary>Catalog template identity for script rows.</summary>
    public string? CatalogId { get; set; }
    /// <summary>Installed script instance; null means the template can be imported.</summary>
    public Guid? ScriptId { get; set; }
    /// <summary>Saved source differs from the installed template baseline.</summary>
    public bool Customized { get; set; }

    [JsonIgnore]
    public bool SecurityUpdateRequired => SecurityRelease != null;

    [JsonIgnore]
    public GXAvailableRelease? SecurityRelease => Releases
        .Where(r => r.IsSecurityUpdate && (!r.Prerelease || IncludePrereleases) && IsNewer(r.Version))
        .OrderByDescending(r => Major(r.Version) == Major(InstalledVersion))
        .ThenByDescending(r => r.Version, GXReleaseVersionComparer.Instance).FirstOrDefault();

    [JsonIgnore]
    public GXAvailableRelease? LatestRelease
    {
        get
        {
            var ordered = Releases.OrderByDescending(r => r.Version, GXReleaseVersionComparer.Instance);
            return ordered.FirstOrDefault(r => Major(r.Version) == Major(InstalledVersion)) ?? ordered.FirstOrDefault();
        }
    }

    [JsonIgnore]
    public IEnumerable<GXAvailableRelease> AdditionalReleases => Releases
        .Where(r => IsNewer(r.Version) && Major(r.Version) != Major(LatestRelease?.Version))
        .GroupBy(r => Major(r.Version))
        .Select(group => group.OrderByDescending(r => r.Version, GXReleaseVersionComparer.Instance).First())
        .Where(r => r.Version != SecurityRelease?.Version)
        .OrderByDescending(r => r.Version, GXReleaseVersionComparer.Instance);

    private bool IsNewer(string version) => GXReleaseVersionComparer.TryNormalize(InstalledVersion, out var installed) &&
        GXReleaseVersionComparer.TryNormalize(version, out var available) && GXReleaseVersionComparer.Instance.Compare(available, installed) > 0;

    private static string? Major(string? version) => GXReleaseVersionComparer.TryNormalize(version, out var normalized) ? normalized.Split('.')[0] : null;
}

public sealed class GXAvailableUpdatesResponse
{
    /// <summary>The execution ID for a manual check; null for a status-only response.</summary>
    public Guid? RunId { get; set; }
    public DateTimeOffset? Checked { get; set; }
    public GXAvailableUpdate Core { get; set; } = new();
    public List<GXAvailableUpdate> DeviceProfiles { get; set; } = [];
    public List<GXAvailableUpdate> Modules { get; set; } = [];
    public List<GXAvailableUpdate> Agents { get; set; } = [];
    public List<GXAvailableUpdate> Scripts { get; set; } = [];
}
