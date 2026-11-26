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

using Gurux.DLMS;
using Task = System.Threading.Tasks.Task;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Collects complete profile buffers using entry counts and reverse daily range fallback.</summary>
internal static class GXProfileBufferCollector
{
    /// <summary>Reads no rows for empty buffers, reads one-row buffers directly and otherwise tries each entry before daily ranges.</summary>
    /// <param name="entriesInUse">The meter's reported buffer row count.</param>
    /// <param name="now">The current date and offset defining the first fallback day.</param>
    /// <param name="readEntry">Reads a one-based entry and requested row count.</param>
    /// <param name="readRange">Reads a single fallback day.</param>
    /// <param name="readBuffer">Reads buffer attribute two without selective access.</param>
    /// <param name="selectRows">Keeps rows within a half-open day, excluding inclusive boundary duplicates.</param>
    /// <param name="errorMessage">The localized message used when the meter returns invalid or incomplete buffer data.</param>
    /// <param name="token">Cancels entry or daily collection before the next protocol request.</param>
    /// <returns>The complete raw buffer, preserving genuine repeated rows.</returns>
    /// <exception cref="InvalidOperationException">The buffer is invalid or the advertised count cannot be collected before the minimum date.</exception>
    /// <exception cref="OperationCanceledException">Collection was cancelled.</exception>
    internal static async Task<GXArray> ReadAllAsync(uint entriesInUse, DateTimeOffset now,
        Func<uint, uint, object?> readEntry, Func<DateTimeOffset, DateTimeOffset, object?> readRange,
        Func<Task<object>> readBuffer, Func<GXArray, DateTimeOffset, DateTimeOffset, GXArray> selectRows,
        string errorMessage, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (entriesInUse == 0)
        {
            return new GXArray();
        }
        if (entriesInUse == 1)
        {
            var buffer = await readBuffer() as GXArray ?? throw new InvalidOperationException(errorMessage);
            token.ThrowIfCancellationRequested();
            return buffer;
        }
        var collected = new GXArray();
        try
        {
            for (uint offset = 0; offset < entriesInUse; ++offset)
            {
                token.ThrowIfCancellationRequested();
                if (readEntry(offset + 1, 1) is not GXArray row || row.Count != 1)
                {
                    throw new InvalidOperationException(errorMessage);
                }
                collected.Add(row[0]);
            }
            token.ThrowIfCancellationRequested();
            return collected;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Restart the collection so rows already read by entry cannot be duplicated by fallback.
            token.ThrowIfCancellationRequested();
            collected.Clear();
        }
        var start = new DateTimeOffset(now.Date, now.Offset);
        var end = start.AddDays(1);
        while ((long)collected.Count < entriesInUse)
        {
            token.ThrowIfCancellationRequested();
            if (readRange(start, end) is not GXArray rows)
            {
                throw new InvalidOperationException(errorMessage);
            }
            collected.AddRange(selectRows(rows, start, end));
            token.ThrowIfCancellationRequested();
            if ((long)collected.Count >= entriesInUse)
            {
                return collected;
            }
            if (start.UtcDateTime <= DateTime.MinValue.AddDays(1))
            {
                throw new InvalidOperationException(errorMessage);
            }
            end = start;
            start = start.AddDays(-1);
        }
        return collected;
    }
}