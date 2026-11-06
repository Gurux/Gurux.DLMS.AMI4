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

using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Host-owned request timings and configuration consumed by the optional Diagnostics module.</summary>
public interface IGXAmiDiagnosticsSource
{
    /// <summary>Snapshot persistence interval in seconds; zero or negative disables collection.</summary>
    int PerformanceInterval { get; }
    /// <summary>Occurs when the configured persistence interval changes.</summary>
    event Action<int>? IntervalChanged;
    /// <summary>Reports target, the Shared CrudAction byte value, and elapsed time. Contains no request bodies or secrets.</summary>
    event Action<string, byte, TimeSpan>? RequestCompleted;
    /// <summary>Resolves the host's existing administrator identity for background persistence.</summary>
    /// <param name="cancellationToken">Cancels identity resolution.</param>
    /// <returns>The persistence principal, or null when no administrator exists.</returns>
    Task<ClaimsPrincipal?> GetPersistenceUserAsync(CancellationToken cancellationToken);
}
