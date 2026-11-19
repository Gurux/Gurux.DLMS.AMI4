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

/// <summary>Coordinates module generations, dependency availability, event delivery, and lifecycle operations.</summary>
public sealed partial class GXModuleRuntime
{
    /// <summary>Validates and migrates configuration for a currently registered module schema.</summary>
    /// <remarks>Does not mutate the live snapshot. Unloaded modules are validated when activated.</remarks>
    internal string? NormalizeSettings(string id, string? settings) => PrepareSettings(id, settings).Value;

    /// <summary>Normalizes proposed configuration using the selected generation's schema and retains its generation identifier.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="settings">Proposed serialized module configuration.</param>
    /// <returns>Normalized settings together with the generation that supplied the schema, if loaded.</returns>
    private (string? Value, string? Generation) PrepareSettings(string id, string? settings)
    {
        Entry? entry;
        lock (_sync)
        {
            entry = _entries.GetValueOrDefault(id);
        }
        // Keep the generation alongside the schema to prevent applying an edit to a replacement module.
        return (entry?.Settings.Normalize(settings) ?? settings, entry?.Generation);
    }
}
