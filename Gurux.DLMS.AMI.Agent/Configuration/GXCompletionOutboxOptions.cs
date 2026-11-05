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

/// <summary>Defines completion storage admission limits and HTTP acknowledgement deadlines.</summary>
/// <remarks>Admission limits include quarantined results. Already-running tasks may finish after admission is paused.</remarks>
internal sealed class GXCompletionOutboxOptions
{
    /// <summary>Gets or sets the queued and quarantined record count at which new claims pause. Defaults to 10,000.</summary>
    public int MaxPendingRecords { get; set; } = 10000;
    /// <summary>Gets or sets the admission threshold in bytes, including quarantine and results awaiting persistence. Defaults to 256 MiB.</summary>
    public long MaxPendingBytes { get; set; } = 256L * 1024 * 1024;
    /// <summary>Gets or sets the minimum available disk space in bytes. Defaults to 64 MiB; zero disables this threshold.</summary>
    public long MinFreeBytes { get; set; } = 64L * 1024 * 1024;
    /// <summary>Gets or sets the timeout for one acknowledgement request. Defaults to five seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Gets or sets the time budget for an acknowledgement pass. Defaults to five seconds.</summary>
    /// <remarks>This budget does not cancel persistence of results held only in memory.</remarks>
    public TimeSpan FlushBudget { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Checks admission thresholds and acknowledgement timeouts.</summary>
    /// <exception cref="ArgumentException">A threshold is invalid, or a timeout is not positive or exceeds five minutes.</exception>
    internal void Validate()
    {
        if (MaxPendingRecords <= 0 || MaxPendingBytes <= 0 || MinFreeBytes < 0 || RequestTimeout <= TimeSpan.Zero ||
            RequestTimeout > TimeSpan.FromMinutes(5) || FlushBudget <= TimeSpan.Zero || FlushBudget > TimeSpan.FromMinutes(5))
            {
            throw new ArgumentException("Invalid completion storage limits or timeouts.");
        }
    }
    /// <summary>Reads and validates completion settings from the <c>Completions</c> configuration section.</summary>
    /// <param name="config">Host configuration, including deployment and environment overrides.</param>
    /// <returns>Validated options, using defaults for omitted settings.</returns>
    /// <exception cref="ArgumentException">A parsed threshold or timeout fails validation.</exception>
    internal static GXCompletionOutboxOptions Read(IConfiguration config)
    {
        var result = new GXCompletionOutboxOptions
        {
            MaxPendingRecords = config.GetValue("Completions:MaxPendingRecords", 10000),
            MaxPendingBytes = config.GetValue("Completions:MaxPendingBytes", 256L * 1024 * 1024),
            MinFreeBytes = config.GetValue("Completions:MinFreeBytes", 64L * 1024 * 1024),
            RequestTimeout = config.GetValue("Completions:RequestTimeout", TimeSpan.FromSeconds(5)),
            FlushBudget = config.GetValue("Completions:FlushBudget", TimeSpan.FromSeconds(5))
        };
        result.Validate();
        return result;
    }
}

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
