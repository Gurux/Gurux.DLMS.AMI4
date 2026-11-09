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

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module table cleanup plan.</summary>
public static class ModuleTableCleanupPlan
{
    /// <summary>Performs the create operation.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="selected">The selected.</param>
    /// <param name="dropTables">The drop tables.</param>
    public static IReadOnlyList<DatabaseTable> Create(DatabaseTableGraph graph, IReadOnlyList<GXModuleTableOwnership> selected, bool dropTables = false)
    {
        var wanted = selected.Select(t => t.Id).ToHashSet();
        var tables = graph.Tables.Where(t => wanted.Contains(t.Key)).ToDictionary(t => t.Key);
        if (tables.Count == 0)
        {
            return [];
        }

        if (dropTables && graph.BlocksDrop)
        {
            throw new InvalidOperationException("Database/server DDL triggers are present or cannot be verified; table removal was not started.");
        }

        foreach (var edge in graph.ForeignKeys)
        {
            if (tables.TryGetValue(edge.Target, out DatabaseTable? value) && !tables.ContainsKey(edge.Source))
            {
                throw new InvalidOperationException("External table references module table " + value.Name + "; cleanup was not started.");
            }
        }

        if (tables.Keys.Any(graph.BlockedTables.Contains))
        {
            throw new InvalidOperationException("Module tables with triggers or temporal history cannot be cleaned automatically.");
        }

        var ordered = new List<DatabaseTable>();
        var remaining = tables.Keys.ToHashSet();
        while (remaining.Count > 0)
        {
            var children = remaining.Where(key => !graph.ForeignKeys.Any(f => f.Target == key && remaining.Contains(f.Source))).OrderBy(key => tables[key].Name, StringComparer.Ordinal).ToArray();
            if (children.Length == 0)
            {
                throw new InvalidOperationException("Cyclic table dependencies prevent safe module cleanup.");
            }

            foreach (var key in children)
            {
                ordered.Add(tables[key]);
                remaining.Remove(key);
            }
        }
        return ordered;
    }
}


