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

using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.Reflection;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module dependency validator.</summary>
public static class GXModuleDependencyValidator
{
    /// <summary>Performs the read compatibility version operation.</summary>
    /// <param name="assembly">The assembly.</param>
    public static string ReadCompatibilityVersion(Assembly assembly)
    {
        var value = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version?.ToString();
        if (!GXReleaseVersionComparer.TryNormalize(value, out var normalized))
        {
            throw new ArgumentException($"Invalid compatibility version '{value}' in {assembly.GetName().Name}.");
        }

        return normalized.Split('+')[0];
    }
    /// <summary>Performs the validate declarations operation.</summary>
    /// <param name="moduleId">The module id.</param>
    /// <param name="declarations">The declarations.</param>
    public static IReadOnlyList<AmiModuleDependency> ValidateDeclarations(string moduleId, IEnumerable<AmiModuleDependency> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        var list = declarations.Select(d => d with { MinimumVersion = string.IsNullOrWhiteSpace(d.MinimumVersion) ? null : d.MinimumVersion, MaximumVersionExclusive = string.IsNullOrWhiteSpace(d.MaximumVersionExclusive) ? null : d.MaximumVersionExclusive }).ToArray();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in list)
        {
            if (!Enum.IsDefined(d.Kind) || string.IsNullOrWhiteSpace(d.Id) || !keys.Add(d.Kind + ":" + d.Id) ||
               (d.Kind == AmiModuleDependencyKind.Module && d.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Invalid dependency declaration on module '{moduleId}'.");
            }

            foreach (var bound in new[] { d.MinimumVersion, d.MaximumVersionExclusive })
            {
                if (bound != null && !GXReleaseVersionComparer.TryNormalize(bound, out _))
                {
                    throw new ArgumentException($"Invalid dependency version '{bound}'.");
                }
            }

            if (d.MinimumVersion != null && d.MaximumVersionExclusive != null && GXReleaseVersionComparer.Instance.Compare(d.MinimumVersion, d.MaximumVersionExclusive) >= 0)
            {
                throw new ArgumentException("Dependency minimum must be below its exclusive maximum.");
            }
        }
        return list;
    }
    /// <summary>Performs the is version compatible operation.</summary>
    /// <param name="offered">The offered.</param>
    /// <param name="dependency">The dependency.</param>
    public static bool IsVersionCompatible(string offered, AmiModuleDependency dependency)
    {
        if (!GXReleaseVersionComparer.TryNormalize(offered, out _))
        {
            throw new ArgumentException($"Invalid compatibility version '{offered}'.");
        }

        return (dependency.MinimumVersion == null || GXReleaseVersionComparer.Instance.Compare(offered, dependency.MinimumVersion) >= 0) &&
                    (dependency.MaximumVersionExclusive == null || GXReleaseVersionComparer.Instance.Compare(offered, dependency.MaximumVersionExclusive) < 0);
    }
    /// <summary>Performs the describe operation.</summary>
    /// <param name="dependency">The dependency.</param>
    public static string Describe(AmiModuleDependency dependency) => $"{dependency.Id} >= {dependency.MinimumVersion ?? "any"} and < {dependency.MaximumVersionExclusive ?? "unbounded"}";
    /// <summary>Performs the validate host dependencies operation.</summary>
    /// <param name="dependencies">The dependencies.</param>
    public static void ValidateHostDependencies(IEnumerable<AmiModuleDependency> dependencies)
    {
        foreach (var d in dependencies.Where(d => d.Kind == AmiModuleDependencyKind.HostAssembly))
        {
            var assembly = PluginLoadContext.ResolveSharedAssembly(d.Id);
            var version = ReadCompatibilityVersion(assembly);
            if (!IsVersionCompatible(version, d))
            {
                throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Required host dependency {Describe(d)}; offered {version}.");
            }
        }
    }
}
