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

internal static class GXCatalogCompatibility
{
    internal static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    internal static IReadOnlyDictionary<string, Version> HostAssemblies()
    {
        var paths = ((AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? [])
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"));
        return GXModuleCompatibility.ReadAssemblyVersions(paths.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    internal static bool IsCompatible(GXUpdateRelease release, IReadOnlyDictionary<string, Version> assemblies)
    {
        var result = GXModuleCompatibility.Check(release, assemblies);
        // UI.Components is host-provided; treat its reference as a same-major minimum.
        // Keep the catalog's other assembly and metadata checks unchanged.
        return result.IsKnown && result.IsAmiVersionCompatible != false && result.Issues.All(issue =>
            string.Equals(issue.Name, "Gurux.UI.Components.dll", StringComparison.OrdinalIgnoreCase) &&
            Version.TryParse(issue.RequiredVersion, out var required) &&
            Version.TryParse(issue.InstalledVersion, out var installed) &&
            required.Major == installed.Major && installed >= required);
    }

    internal static bool IsCompatible(CatalogProductType type, GXUpdateRelease release, IReadOnlyDictionary<string, Version> assemblies)
    {
        // Applications replace the host; agents execute with their own libraries.
        // Deployment compatibility is enforced separately by the deployment controller.
        return type is CatalogProductType.Agent or CatalogProductType.Application || IsCompatible(release, assemblies);
    }

    internal static string HostAssemblyFingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", HostAssemblies().OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key.ToUpperInvariant() + ":" + pair.Value)))));
}
