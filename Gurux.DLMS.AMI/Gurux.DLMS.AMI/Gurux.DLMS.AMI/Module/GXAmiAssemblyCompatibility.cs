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

/// <summary>Defines the build-independent compatibility policy for Gurux DLMS AMI 4.1 assemblies.</summary>
internal static class GXAmiAssemblyCompatibility
{
    /// <summary>Accepts any pair of 4.1 builds belonging to the Gurux DLMS AMI assembly family.</summary>
    /// <param name="name">The assembly simple name or DLL basename.</param>
    /// <param name="required">The version referenced by the module.</param>
    /// <param name="installed">The version offered by the host.</param>
    /// <returns>True when the assembly belongs to Gurux DLMS AMI and both versions have major 4 and minor 1.</returns>
    internal static bool IsCompatible41(string? name, Version? required, Version? installed)
    {
        if (name == null || required is not { Major: 4, Minor: 1 } || installed is not { Major: 4, Minor: 1 })
        {
            return false;
        }
        var assemblyName = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        return string.Equals(assemblyName, "Gurux.DLMS.AMI", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("Gurux.DLMS.AMI.", StringComparison.OrdinalIgnoreCase);
    }
}
