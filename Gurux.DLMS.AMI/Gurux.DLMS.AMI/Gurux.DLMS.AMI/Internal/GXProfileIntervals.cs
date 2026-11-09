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

using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Half-open coverage arithmetic; timestamps are compared as UTC instants.</summary>
internal static class GXProfileIntervals
{
    public static GXProfileRange[] Merge(IEnumerable<GXProfileRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        var result = new List<GXProfileRange>();
        foreach (var range in ranges.OrderBy(range => range.Start).ThenBy(range => range.End))
        {
            Validate(range);
            var normalized = new GXProfileRange(range.Start.ToUniversalTime(), range.End.ToUniversalTime());
            if (result.Count == 0 || normalized.Start > result[^1].End) {
                result.Add(normalized);
            }
            else if (normalized.End > result[^1].End) {
                result[^1] = result[^1] with { End = normalized.End };
            }
        }
        return [.. result];
    }

    public static GXProfileRange[] Missing(GXProfileRange requested, IEnumerable<GXProfileRange> covered, TimeSpan maxChunk)
    {
        Validate(requested);
        if (maxChunk <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(maxChunk));
        }
        var cursor = requested.Start.ToUniversalTime();
        var end = requested.End.ToUniversalTime();
        var gaps = new List<GXProfileRange>();
        foreach (var range in Merge(covered))
        {
            if (range.End <= cursor) {
                continue;
            }
            if (range.Start >= end) {
                break;
            }
            Add(cursor, range.Start < end ? range.Start : end);
            if (range.End > cursor) {
                cursor = range.End;
            }
            if (cursor >= end) {
                break;
            }
        }
        Add(cursor, end);
        return [.. gaps];

        void Add(DateTimeOffset from, DateTimeOffset until)
        {
            while (from < until)
            {
                var next = until - from <= maxChunk ? until : from.Add(maxChunk);
                gaps.Add(new(from, next));
                from = next;
            }
        }
    }

    private static void Validate(GXProfileRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        if (range.End <= range.Start) {
            throw new ArgumentException("A profile interval must have an end after its start.", nameof(range));
        }
    }
}
