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
using Gurux.DLMS.AMI.Module;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module startup candidate.</summary>
/// <param name="Id">The id.</param>
/// <param name="CompatibilityVersion">The compatibility version.</param>
/// <param name="Dependencies">The dependencies.</param>
/// <param name="Services">The services.</param>
public sealed record GXModuleStartupCandidate(string Id, string CompatibilityVersion, IReadOnlyList<AmiModuleDependency> Dependencies, IReadOnlyList<string> Services);
/// <summary>Provides module startup planner.</summary>
public static class GXModuleStartupPlanner
{
    /// <summary>Performs the plan operation.</summary>
    /// <param name="candidates">The candidates.</param>
    /// <param name="active">The active.</param>
    /// <param name="roots">The roots.</param>
    public static IReadOnlyList<GXModuleStartupCandidate> Plan(IReadOnlyList<GXModuleStartupCandidate> candidates, IReadOnlyList<GXModuleStartupCandidate> active, IEnumerable<string>? roots = null)
    {
        var nodes = candidates.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var existing = active.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var providers = candidates.Concat(active).SelectMany(c => c.Services.Select(s => (Service: s, Candidate: c))).GroupBy(s => s.Service, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Select(s => s.Candidate).DistinctBy(c => c.Id, StringComparer.OrdinalIgnoreCase).ToArray(), StringComparer.Ordinal);
        var result = new List<GXModuleStartupCandidate>();
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();
        void Visit(string id)
        {
            if (done.Contains(id) || existing.ContainsKey(id))
            {
                return;
            }

            if (path.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException("Module dependency cycle: " + string.Join(" -> ", path.Append(id)));
            }

            if (!nodes.TryGetValue(id, out var candidate))
            {
                throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Required active module '{id}' is missing or disabled.");
            }

            path.Add(id);
            var dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, candidate.Dependencies);
            GXModuleDependencyValidator.ValidateHostDependencies(dependencies);
            foreach (var dependency in dependencies.Where(d => d.Kind != AmiModuleDependencyKind.HostAssembly))
            {
                string provider = dependency.Id;
                if (dependency.Kind == AmiModuleDependencyKind.Service)
                {
                    var choices = providers.GetValueOrDefault(dependency.Id) ?? [];
                    if (choices.Length != 1)
                    {
                        throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Service '{dependency.Id}' requires exactly one active/configured provider; found {choices.Length}.");
                    }

                    provider = choices[0].Id;
                }
                var offered = existing.GetValueOrDefault(provider) ?? nodes.GetValueOrDefault(provider) ?? throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module {id} requires active {dependency.Id}.");
                if (!GXModuleDependencyValidator.IsVersionCompatible(offered.CompatibilityVersion, dependency))
                {
                    throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module {id} requires {GXModuleDependencyValidator.Describe(dependency)}; offered {offered.CompatibilityVersion}.");
                }

                Visit(provider);
            }
            path.RemoveAt(path.Count - 1);
            done.Add(id);
            result.Add(candidate);
        }
        foreach (var root in roots ?? candidates.Select(c => c.Id))
        {
            Visit(root);
        }

        return result;
    }
}
