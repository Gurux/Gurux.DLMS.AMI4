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

using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Plans chronological daily collection from the latest successful boundary.</summary>
public static class GXProfileHistoryBackfill
{
    /// <summary>Plans automatic collection, using one year when no successful boundary exists.</summary>
    /// <param name="now">The exclusive current collection boundary.</param>
    /// <param name="lastRead">The latest persisted captured timestamp, if known.</param>
    /// <param name="successful">Completed intervals, including successful empty reads.</param>
    /// <param name="entriesInUse">The meter buffer row count, if known.</param>
    /// <returns>Chronological daily intervals through the current boundary.</returns>
    public static GXProfileRange[] PlanAutomatic(DateTimeOffset now, DateTimeOffset? lastRead,
        IEnumerable<GXProfileRange> successful, uint? entriesInUse = null)
    {
        if (entriesInUse == 0)
        {
            return [];
        }
        var boundaries = successful.Select(range => range.End);
        if (lastRead.HasValue)
        {
            boundaries = boundaries.Append(lastRead.Value);
        }
        return PlanAll(boundaries.DefaultIfEmpty(now.AddYears(-1)).Max(), now);
    }

    /// <summary>Splits a complete buffer range into daily intervals without a one-year limit.</summary>
    /// <param name="start">The inclusive oldest captured timestamp.</param>
    /// <param name="end">The exclusive current collection boundary.</param>
    /// <returns>Daily intervals covering the entire range, or none for an empty range.</returns>
    public static GXProfileRange[] PlanAll(DateTimeOffset start, DateTimeOffset end)
    {
        return start < end ? GXProfileIntervals.Missing(new(start, end), [], TimeSpan.FromDays(1)) : [];
    }
}