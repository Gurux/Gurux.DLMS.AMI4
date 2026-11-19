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

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>A sampled view of completion storage and task admission.</summary>
/// <param name="PendingCount">Queued records plus results awaiting persistence; excludes quarantine.</param>
/// <param name="PendingBytes">Queued JSON and UTF-8 bytes awaiting persistence; excludes quarantine.</param>
/// <param name="OldestSeconds">Age of the oldest queued file in seconds; zero when no queued files exist.</param>
/// <param name="QuarantinedCount">Records retained for operator recovery without automatic replay.</param>
/// <param name="FreeBytes">Last sampled available space on the queue's drive, in bytes.</param>
/// <param name="UnpersistedCount">Results whose only retained copy is still in process memory.</param>
/// <param name="ClaimsPaused">Whether shutdown, storage failure or an admission threshold prevents new claims.</param>
/// <remarks>Storage is sampled independently of HTTP. After a sampling failure, previous storage counts may remain in this snapshot.</remarks>
internal sealed record GXCompletionOutboxStatus(int PendingCount, long PendingBytes, double OldestSeconds,
    int QuarantinedCount, long FreeBytes, int UnpersistedCount, bool ClaimsPaused)
{
    /// <summary>Gets whether shutdown is waiting for results held only in memory to be persisted.</summary>
    public bool ShutdownBlocked { get; init; }
}
