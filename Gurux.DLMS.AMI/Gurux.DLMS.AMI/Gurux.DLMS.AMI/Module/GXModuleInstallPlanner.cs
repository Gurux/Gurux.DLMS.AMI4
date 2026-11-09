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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>An inspected package and its runtime compatibility version.</summary>
public sealed record GXModuleInstallCandidate(string Id, string CompatibilityVersion, IReadOnlyList<AmiModuleDependency> Dependencies);

/// <summary>Validates the complete catalog dependency graph before installations begin.</summary>
public static class GXModuleInstallPlanner
{
    /// <summary>Distinguishes a user-disabled installation from an uninstalled catalog placeholder.</summary>
    public static bool IsExplicitlyDisabled(GXModule module) => module.Enabled == false && IsInstalled(module);

    /// <summary>Whether a module has an installed package rather than only catalog metadata.</summary>
    public static bool IsInstalled(GXModule module) =>
        ((module.Status.GetValueOrDefault() & (ModuleStatus.Installed | ModuleStatus.CustomBuild)) != 0 ||
         !string.IsNullOrWhiteSpace(module.FileName));

    /// <summary>Chooses the newest usable catalog release satisfying a module requirement.</summary>
    public static GXModuleVersion? SelectVersion(IEnumerable<GXModuleVersion> versions, AmiModuleDependency requirement, bool includePrereleases) =>
        versions.Where(v => v.Enabled != false && (includePrereleases || !v.Prerelease) &&
            !string.IsNullOrWhiteSpace(v.Url) && !string.IsNullOrWhiteSpace(v.FileName) &&
            GXReleaseVersionComparer.TryNormalize(v.Number, out _) &&
            GXModuleDependencyValidator.IsVersionCompatible(v.Number!, requirement))
        .OrderByDescending(v => v.Number, GXReleaseVersionComparer.Instance).FirstOrDefault();

    /// <summary>Resolves missing module dependencies and returns packages in installation order.</summary>
    public static async Task<IReadOnlyList<GXModuleInstallCandidate>> PlanAsync(GXModuleInstallCandidate root,
        IReadOnlyDictionary<string, string> activeVersions,
        Func<AmiModuleDependency, CancellationToken, Task<GXModuleInstallCandidate?>> resolve,
        CancellationToken cancellationToken = default)
    {
        var active = new Dictionary<string, string>(activeVersions, StringComparer.OrdinalIgnoreCase);
        active.Remove(root.Id);
        var selected = new Dictionary<string, GXModuleInstallCandidate>(StringComparer.OrdinalIgnoreCase) { [root.Id] = root };
        // If a later consumer tightens a shared range, select its intersection and inspect
        // the graph again. Previously inspected but unreachable packages are never installed.
        for (int attempt = 0; attempt < 128; ++attempt)
        {
            var ranges = new Dictionary<string, AmiModuleDependency>(StringComparer.OrdinalIgnoreCase);
            var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var path = new List<string>();
            var result = new List<GXModuleInstallCandidate>();
            AmiModuleDependency Merge(AmiModuleDependency requirement)
            {
                if (!ranges.TryGetValue(requirement.Id, out var previous))
                    {
                    return ranges[requirement.Id] = requirement;
                }
                string? min = previous.MinimumVersion;
                if (requirement.MinimumVersion != null && (min == null || GXReleaseVersionComparer.Instance.Compare(requirement.MinimumVersion, min) > 0))
                    {
                    min = requirement.MinimumVersion;
                }
                string? max = previous.MaximumVersionExclusive;
                if (requirement.MaximumVersionExclusive != null && (max == null || GXReleaseVersionComparer.Instance.Compare(requirement.MaximumVersionExclusive, max) < 0))
                    {
                    max = requirement.MaximumVersionExclusive;
                }
                if (min != null && max != null && GXReleaseVersionComparer.Instance.Compare(min, max) >= 0)
                    {
                    throw new AmiModuleDependencyException($"Conflicting dependency requirements for '{requirement.Id}': >= {min} and < {max}.");
                }
                return ranges[requirement.Id] = requirement with { MinimumVersion = min, MaximumVersionExclusive = max };
            }
            async Task<GXModuleInstallCandidate> Resolve(AmiModuleDependency requirement)
            {
                var dependency = await resolve(requirement, cancellationToken)
                    ?? throw new AmiModuleDependencyException($"No compatible catalog release is available for {GXModuleDependencyValidator.Describe(requirement)}.");
                if (!string.Equals(dependency.Id, requirement.Id, StringComparison.OrdinalIgnoreCase))
                    {
                    throw new AmiModuleDependencyException($"Expected dependency '{requirement.Id}', package declares '{dependency.Id}'.");
                }
                if (!GXModuleDependencyValidator.IsVersionCompatible(dependency.CompatibilityVersion, requirement))
                    {
                    throw new AmiModuleDependencyException($"Dependency {GXModuleDependencyValidator.Describe(requirement)} conflicts with package version {dependency.CompatibilityVersion}.");
                }
                return dependency;
            }
            async Task Visit(GXModuleInstallCandidate candidate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (path.Contains(candidate.Id, StringComparer.OrdinalIgnoreCase))
                    {
                    throw new AmiModuleDependencyException("Module dependency cycle: " + string.Join(" -> ", path.Append(candidate.Id)));
                }
                if (done.Contains(candidate.Id)) {
                    return;
                }
                path.Add(candidate.Id);
                var declarations = GXModuleDependencyValidator.ValidateDeclarations(candidate.Id, candidate.Dependencies);
                GXModuleDependencyValidator.ValidateHostDependencies(declarations);
                foreach (var declaration in declarations.Where(d => d.Kind == AmiModuleDependencyKind.Module))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (path.Contains(declaration.Id, StringComparer.OrdinalIgnoreCase))
                        {
                        throw new AmiModuleDependencyException("Module dependency cycle: " + string.Join(" -> ", path.Append(declaration.Id)));
                    }
                    var requirement = Merge(declaration);
                    if (selected.TryGetValue(requirement.Id, out var dependency))
                    {
                        if (!GXModuleDependencyValidator.IsVersionCompatible(dependency.CompatibilityVersion, requirement))
                        {
                            selected[requirement.Id] = await Resolve(requirement);
                            throw new ReplanException();
                        }
                    }
                    else
                    {
                        if (active.TryGetValue(requirement.Id, out var offered) && GXModuleDependencyValidator.IsVersionCompatible(offered, requirement))
                            {
                            continue;
                        }
                        selected[requirement.Id] = dependency = await Resolve(requirement);
                    }
                    await Visit(dependency);
                }
                path.RemoveAt(path.Count - 1);
                done.Add(candidate.Id); result.Add(candidate);
            }
            try { await Visit(root); }
            catch (ReplanException) { continue; }
            // An active dependency reused in an earlier branch may have been selected
            // for replacement by a later branch. Put it before all of its consumers.
            done.Clear(); path.Clear(); result.Clear();
            void Order(GXModuleInstallCandidate candidate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (path.Contains(candidate.Id, StringComparer.OrdinalIgnoreCase))
                    {
                    throw new AmiModuleDependencyException("Module dependency cycle: " + string.Join(" -> ", path.Append(candidate.Id)));
                }
                if (done.Contains(candidate.Id)) {
                    return;
                }
                path.Add(candidate.Id);
                foreach (var requirement in candidate.Dependencies.Where(d => d.Kind == AmiModuleDependencyKind.Module))
                    {
                    if (selected.TryGetValue(requirement.Id, out var child))
                    {
                        if (!GXModuleDependencyValidator.IsVersionCompatible(child.CompatibilityVersion, requirement))
                            {
                            throw new AmiModuleDependencyException($"Conflicting dependency requirements for '{requirement.Id}'.");
                        }
                        Order(child);
                    }
                }
                path.RemoveAt(path.Count - 1);
                done.Add(candidate.Id); result.Add(candidate);
            }
            Order(root);
            return result;
        }
        throw new AmiModuleDependencyException("Module dependency versions could not be resolved consistently.");
    }

    private sealed class ReplanException : Exception;
}
