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

using System.Collections.Frozen;
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Coordinates module generations, dependency availability, event delivery, and lifecycle operations.</summary>
public sealed partial class GXModuleRuntime
{
    // Immutable contract-only snapshot: never retain an exported service instance.
    /// <summary>Snapshot of service types currently available to modules.</summary>
    private FrozenSet<Type> _availableServices = FrozenSet<Type>.Empty;

    /// <summary>Replaces the available-service snapshot after module state or exports change.</summary>
    private void RefreshServiceAvailability()
    {
        lock (_sync)
        {
            var snapshot = _exports.Where(pair => _entries.TryGetValue(pair.Value, out var entry) &&
                    entry.State == AmiModuleState.Active && !entry.UninstallRequested)
                .Select(pair => pair.Key).ToFrozenSet();
            Volatile.Write(ref _availableServices, snapshot);
        }
    }
}
