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

/// <summary>Limits durable raw notification delivery without changing transport payloads.</summary>
public sealed class GXRawDeliveryOptions
{
    /// <summary>Maximum number of raw records retained before accepting more input is blocked.</summary>
    public int MaxPendingRecords { get; set; } = 10000;
    /// <summary>Maximum total byte size of persisted raw records.</summary>
    public long MaxPendingBytes { get; set; } = 268435456;
    /// <summary>Minimum free disk space required to persist more raw records.</summary>
    public long MinFreeBytes { get; set; } = 67108864;
    /// <summary>Maximum duration of an individual raw record delivery request.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    /// <summary>Delay before retrying a failed raw record delivery.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>Maximum time allowed to flush pending raw records during shutdown.</summary>
    public TimeSpan ShutdownFlushBudget { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Rejects storage limits and time intervals that cannot be used by delivery timers.</summary>
    /// <exception cref="ArgumentException">A limit is negative, zero where prohibited, or exceeds the supported timer interval.</exception>
    internal void Validate()
    {
        if (MaxPendingRecords < 1 || MaxPendingBytes < 1 || MinFreeBytes < 0 || RequestTimeout <= TimeSpan.Zero ||
            RetryDelay <= TimeSpan.Zero || ShutdownFlushBudget <= TimeSpan.Zero ||
            RequestTimeout.TotalMilliseconds > int.MaxValue || RetryDelay.TotalMilliseconds > int.MaxValue || ShutdownFlushBudget.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentException("Raw delivery storage limits must be positive, free space nonnegative, and timeouts finite positive timer intervals.");
        }
    }
}
