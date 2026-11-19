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

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Resolves the application version from trusted installed package metadata.</summary>
internal static class AgentRuntimeVersion
{
    // Parameters retained for source compatibility with startup checks; runtime files no longer select code.
    /// <summary>Chooses the installed application version, falling back to host-provided version values.</summary>
    /// <param name="baseDirectory">Installed application directory containing release metadata.</param>
    /// <param name="configuredVersion">Version recorded in persisted host configuration, if available.</param>
    /// <param name="executableVersion">Version reported by the running executable, if available.</param>
    /// <returns>Installed application version, or the available configured or executable fallback.</returns>
    internal static string? Resolve(string baseDirectory, string? configuredVersion, string? executableVersion)
        => string.IsNullOrWhiteSpace(executableVersion) ? null : executableVersion.Split('+')[0];
}
