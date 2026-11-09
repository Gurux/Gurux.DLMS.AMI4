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
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Module;
namespace Gurux.DLMS.AMI.Server.Module;
/// <summary>Dependency edges; all calls are serialized by the runtime's state lock.</summary>
public sealed class GXModuleDependencyGraph
{
    private readonly Dictionary<string, string[]> edges = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Performs the resolve operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="dependencies">The dependencies.</param>
    /// <param name="active">The active.</param>
    /// <param name="services">The services.</param>
    public static string[] Resolve(string id, IReadOnlyList<AmiModuleDependency> dependencies, IReadOnlyDictionary<string, AmiModuleManifest> active, IReadOnlyDictionary<string, string> services)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dependencies.Where(d => d.Kind != AmiModuleDependencyKind.HostAssembly))
        {
            string provider = d.Kind == AmiModuleDependencyKind.Service ? services.GetValueOrDefault(d.Id) ?? throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module {id} requires active service {d.Id}.") : d.Id;
            if (provider.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module {id} cannot depend on itself.");
            }

            if (!active.TryGetValue(provider, out var manifest) || !GXModuleDependencyValidator.IsVersionCompatible(manifest.CompatibilityVersion, d))
            {
                throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module {id} requires active {GXModuleDependencyValidator.Describe(d)}; offered {active.GetValueOrDefault(provider)?.CompatibilityVersion ?? "inactive/missing"}.");
            }

            result.Add(provider);
        }
        return [.. result.Order(StringComparer.OrdinalIgnoreCase)];
    }
    /// <summary>Performs the reserve operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="providers">The providers.</param>
    public void Reserve(string id, string[] providers) => edges.Add(id, providers);
    /// <summary>Performs the release operation.</summary>
    /// <param name="id">The item identifier.</param>
    public void Release(string id) => edges.Remove(id);
    /// <summary>Performs the get blocking dependents operation.</summary>
    /// <param name="id">The item identifier.</param>
    public string[] GetBlockingDependents(string id) => [.. edges.Where(e => e.Value.Contains(id, StringComparer.OrdinalIgnoreCase)).Select(e => e.Key).Order(StringComparer.OrdinalIgnoreCase)];
    /// <summary>Performs the ensure can stop operation.</summary>
    /// <param name="id">The item identifier.</param>
    public void EnsureCanStop(string id)
    {
        var blocked = GetBlockingDependents(id);
        if (blocked.Length != 0)
        {
            throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Module '{id}' cannot be stopped, removed or replaced. Dependent modules: {string.Join(", ", blocked)}.");
        }
    }
    /// <summary>Performs the get stop order operation.</summary>
    /// <param name="selected">The selected.</param>
    public string[] GetStopOrder(IEnumerable<string> selected)
    {
        var ids = selected.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var set = ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        void Visit(string id)
        {
            if (!visited.Add(id))
            {
                return;
            }

            foreach (var consumer in GetBlockingDependents(id).Where(set.Contains))
            {
                Visit(consumer);
            }

            result.Add(id);
        }
        foreach (var id in ids)
        {
            Visit(id);
        }

        return [.. result];
    }
}
