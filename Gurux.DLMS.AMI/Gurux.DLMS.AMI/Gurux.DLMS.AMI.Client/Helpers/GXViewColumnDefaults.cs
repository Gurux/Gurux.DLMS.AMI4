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

using Gurux.DLMS.AMI.Shared.DTOs.View;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Combines code-defined table defaults with sparse shared column overrides.</summary>
public static class GXViewColumnDefaults
{
    /// <summary>Returns independent column settings, including unchanged baseline columns.</summary>
    public static List<GXViewColumn> Merge(IReadOnlyList<GXViewColumn> baseline, IEnumerable<GXViewColumn>? overrides)
    {
        var saved = (overrides ?? []).Where(c => c.Enabled != false && c.Removed == null && !string.IsNullOrWhiteSpace(c.Name))
            .ToDictionary(c => c.Name!, StringComparer.Ordinal);
        return [.. baseline.Select(column => Copy(saved.GetValueOrDefault(column.Name!) ?? column)).OrderBy(column => column.Position)];
    }

    /// <summary>Returns only columns whose selection, order or responsive visibility differs from the original baseline.</summary>
    public static List<GXViewColumn> Changes(IReadOnlyList<GXViewColumn> baseline, IEnumerable<GXViewColumn> current,
        IEnumerable<GXViewColumn>? previous = null)
    {
        var defaults = baseline.ToDictionary(c => c.Name!, StringComparer.Ordinal);
        var saved = (previous ?? []).ToDictionary(c => c.Name!, StringComparer.Ordinal);
        var result = new List<GXViewColumn>();
        foreach (var column in current)
        {
            if (column.Name == null || !defaults.TryGetValue(column.Name, out var original))
            {
                continue;
            }
            if (column.Visible == original.Visible && column.Position == original.Position && (column.Visibility ?? 0) == (original.Visibility ?? 0))
            {
                continue;
            }
            var change = Copy(column);
            if (saved.TryGetValue(column.Name, out var existing))
            {
                change.Id = existing.Id;
                change.Description = existing.Description;
                change.CreationTime = existing.CreationTime;
                change.Policies = existing.Policies?.ToList();
            }
            result.Add(change);
        }
        return result;
    }

    private static GXViewColumn Copy(GXViewColumn column) => new()
    {
        Id = column.Id, Name = column.Name, Visible = column.Visible, Enabled = column.Enabled,
        Position = column.Position, Visibility = column.Visibility, Description = column.Description,
        Policies = column.Policies?.ToList(), CreationTime = column.CreationTime, Updated = column.Updated
    };
}
