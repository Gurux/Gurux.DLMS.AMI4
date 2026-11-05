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
/// <summary>Calculates bounded exponential retry delays with jitter.</summary>
internal static class GXRetryBackoff
{
    /// <summary>Calculates a delay between half the exponential ceiling and the ceiling, capped by the maximum.</summary>
    /// <param name="attempt">One-based failed-attempt count; values below one use the initial ceiling.</param>
    /// <param name="initial">Positive ceiling for the first attempt.</param>
    /// <param name="maximum">Positive upper bound for the exponential ceiling.</param>
    /// <param name="sample">Optional jitter sample clamped to zero through one; <see langword="null"/> selects a random sample.</param>
    /// <returns>A jittered retry interval no greater than <paramref name="maximum"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initial"/> or <paramref name="maximum"/> is not positive.</exception>
    internal static TimeSpan GetDelay(int attempt, TimeSpan initial, TimeSpan maximum, double? sample = null)
    {
        if (initial <= TimeSpan.Zero || maximum <= TimeSpan.Zero)
            {
            throw new ArgumentOutOfRangeException(nameof(initial));
        }
        double ceiling = Math.Min(maximum.TotalMilliseconds, initial.TotalMilliseconds * Math.Pow(2, Math.Clamp(attempt - 1, 0, 30)));
        return TimeSpan.FromMilliseconds(ceiling * (0.5 + 0.5 * Math.Clamp(sample ?? Random.Shared.NextDouble(), 0, 1)));
    }
}
