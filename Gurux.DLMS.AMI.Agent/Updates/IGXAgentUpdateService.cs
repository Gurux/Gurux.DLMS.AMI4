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

using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Stages verified releases and hands installation to an external updater after the host drains the worker.</summary>
internal interface IGXAgentUpdateService
{
    /// <summary>Gets a configuration or cooldown diagnostic, or <see langword="null"/> when local updating is available.</summary>
    string? UnavailableReason { get; }
    /// <summary>Gets the verified staged release, or <see langword="null"/> when no release has been prepared.</summary>
    GXPreparedAgentUpdate? PendingUpdate { get; }
    /// <summary>Downloads and verifies a complete release without replacing files in the live installation.</summary>
    /// <param name="version">Expected application release identifier.</param>
    /// <param name="urls">Candidate HTTPS package addresses, attempted in order with their signed sidecar manifests.</param>
    /// <param name="token">Cancels waiting for preparation and active downloads or validation.</param>
    /// <returns>A task that completes once <see cref="PendingUpdate"/> refers to a verified package.</returns>
    /// <exception cref="ArgumentException">The requested version is not a valid release identifier.</exception>
    /// <exception cref="InvalidOperationException">Configuration is unavailable, the download deadline expires, or no candidate is accepted.</exception>
    /// <exception cref="System.IO.InvalidDataException">A supplied package URL does not use HTTPS or contains credentials.</exception>
    /// <exception cref="OperationCanceledException">Preparation is canceled by the caller.</exception>
    Task PrepareAsync(string version, IEnumerable<string> urls, CancellationToken token);
    /// <summary>Launches the external installer for the prepared release.</summary>
    /// <param name="processId">Positive identifier of the current agent process; used for standalone update coordination.</param>
    /// <param name="token">Cancels the launch or its initial confirmation wait.</param>
    /// <returns>A task that confirms launch, not completion of package installation or application health checks.</returns>
    /// <remarks>The host must stop the worker and persist its in-memory task results before invoking this method.</remarks>
    /// <exception cref="InvalidOperationException">No release is staged, configuration is invalid, or the updater fails during launch.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="processId"/> is not positive.</exception>
    /// <exception cref="OperationCanceledException">The launch wait is canceled.</exception>
    Task LaunchAsync(int processId, CancellationToken token);
}
