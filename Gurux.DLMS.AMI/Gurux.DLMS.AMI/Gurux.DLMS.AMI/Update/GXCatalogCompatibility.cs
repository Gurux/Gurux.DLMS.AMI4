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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gurux.Updater.Enums;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Evaluates catalog releases against the host dependency policy and serializes catalog metadata.</summary>
internal static class GXCatalogCompatibility
{
    /// <summary>Creates web JSON options supporting catalog enum names.</summary>
    /// <returns>Serializer options using camel-case string enums.</returns>
    internal static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    /// <summary>Reads effective host assembly versions, preferring runtime-selected paths over application-directory fallbacks.</summary>
    /// <returns>One effective assembly version per DLL basename.</returns>
    /// <remarks>Shadowed application copies are excluded before the updater validates assembly versions.</remarks>
    internal static IReadOnlyDictionary<string, Version> HostAssemblies()
    {
        var paths = ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? [])
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"));
        // Runtime-selected paths precede application-directory fallbacks. A stale
        // shadowed DLL must not compete with the assembly selected by the host.
        return GXModuleCompatibility.ReadAssemblyVersions(paths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Checks release metadata, accepting compatible host contracts and available Microsoft dependencies.</summary>
    /// <param name="release">Release whose dependencies are evaluated.</param>
    /// <param name="assemblies">Available host assembly versions indexed by DLL basename.</param>
    /// <returns>True when metadata and every dependency satisfy the catalog policy.</returns>
    /// <remarks>Microsoft dependencies must exist. System dependencies accept newer host versions. Runtime API compatibility is enforced during loading and execution.</remarks>
    internal static bool IsCompatible(GXUpdateRelease release, IReadOnlyDictionary<string, Version> assemblies)
    {
        var result = GXModuleCompatibility.Check(release, assemblies);
        // AMI 4.1 builds are interchangeable; other contracts retain the runtime loader's minimum policy.
        // Microsoft libraries require availability; System libraries also require at least the referenced version.
        return result.IsKnown && result.IsAmiVersionCompatible != false && result.Issues.All(issue =>
            (issue.Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) && issue.InstalledVersion != null) ||
            Version.TryParse(issue.RequiredVersion, out var required) &&
            Version.TryParse(issue.InstalledVersion, out var installed) &&
            ((issue.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) && installed >= required) ||
             Gurux.DLMS.AMI.Server.Module.GXAmiAssemblyCompatibility.IsCompatible41(issue.Name, required, installed) ||
             (string.Equals(issue.Name, "Gurux.UI.Components.dll", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(issue.Name, "Gurux.DLMS.AMI.Module.dll", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(issue.Name, "Gurux.DLMS.AMI.Shared.dll", StringComparison.OrdinalIgnoreCase)) &&
             required.Major == installed.Major && installed >= required));
    }

    /// <summary>Applies dependency checks to modules while allowing applications and agents to replace their own libraries.</summary>
    /// <param name="type">Product deployment category.</param>
    /// <param name="release">Release to evaluate.</param>
    /// <param name="assemblies">Available host assembly versions indexed by DLL basename.</param>
    /// <returns>True when the release satisfies its deployment category policy.</returns>
    internal static bool IsCompatible(CatalogProductType type, GXUpdateRelease release, IReadOnlyDictionary<string, Version> assemblies)
    {
        // Applications replace the host; agents execute with their own libraries.
        // Deployment compatibility is enforced separately by the deployment controller.
        return type is CatalogProductType.Agent or CatalogProductType.Application || IsCompatible(release, assemblies);
    }

    /// <summary>Computes a stable fingerprint for invalidating cached host compatibility results.</summary>
    /// <returns>The SHA-256 digest of sorted host assembly names and versions.</returns>
    internal static string HostAssemblyFingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", HostAssemblies().OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key.ToUpperInvariant() + ":" + pair.Value)))));
}
