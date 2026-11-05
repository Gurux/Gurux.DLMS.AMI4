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

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>A snapshot of discovered capabilities, separate from persisted agent configuration.</summary>
public sealed class GXAgentCapabilities
{
    /// <summary>Creates a snapshot of discovered serial ports.</summary>
    /// <param name="serialPorts">Available port names.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serialPorts"/> is <see langword="null"/>.</exception>
    /// <remarks>Blank entries are removed; names are distinct and sorted using ordinal comparison.</remarks>
    public GXAgentCapabilities(IEnumerable<string> serialPorts)
    {
        ArgumentNullException.ThrowIfNull(serialPorts);
        SerialPorts = Array.AsReadOnly(serialPorts.Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Gets the read-only list of discovered serial port names.</summary>
    /// <remarks>The selected port is stored separately in <see cref="GXAgentOptions.SerialPort"/>.</remarks>
    public IReadOnlyList<string> SerialPorts { get; }
}
