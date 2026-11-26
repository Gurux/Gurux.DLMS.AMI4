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

using Gurux.DLMS.Enums;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Reports a stable meter-buffer count and an optional verified oldest captured timestamp.</summary>
internal sealed class GXProfileBufferBounds
{
    /// <summary>Bounds the optional timestamp scan so metadata probing cannot read an arbitrarily large buffer.</summary>
    private const uint MaximumProbeEntries = 256;

    /// <summary>Gets the most recently verified Entries In Use value.</summary>
    internal uint EntriesInUse { get; init; }

    /// <summary>Gets the oldest captured time when the ordered buffer boundaries were stable.</summary>
    internal DateTimeOffset? AvailableFrom { get; init; }

    /// <summary>Scans a bounded complete timestamp column and rechecks its count and endpoints before advertising a lower history boundary.</summary>
    /// <param name="clockColumn">The Clock time column within the selected entry response.</param>
    /// <param name="readCount">Reads Entries In Use from the meter.</param>
    /// <param name="readEntries">Reads a bounded range of entries using a one-based index and row count.</param>
    /// <param name="useUtc2NormalTime">Whether timestamp deviations use the reversed convention.</param>
    /// <param name="token">Cancels probing without returning a partial boundary.</param>
    /// <returns>Verified metadata, or null when the meter does not support the optional metadata reads.</returns>
    /// <exception cref="OperationCanceledException">The owning device task is cancelled.</exception>
    internal static async Task<GXProfileBufferBounds?> ReadAsync(int clockColumn,
        Func<Task<object>> readCount, Func<uint, uint, object?> readEntries, bool useUtc2NormalTime = false, CancellationToken token = default)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            uint count = Convert.ToUInt32(await readCount(), System.Globalization.CultureInfo.InvariantCulture);
            token.ThrowIfCancellationRequested();
            if (count == 0 || count > MaximumProbeEntries)
            {
                uint verified = Convert.ToUInt32(await readCount(), System.Globalization.CultureInfo.InvariantCulture);
                token.ThrowIfCancellationRequested();
                return verified == count ? new() { EntriesInUse = verified } : null;
            }
            object? value = readEntries(1, count);
            token.ThrowIfCancellationRequested();
            uint current = Convert.ToUInt32(await readCount(), System.Globalization.CultureInfo.InvariantCulture);
            token.ThrowIfCancellationRequested();
            if (count != current)
            {
                return null;
            }
            if (value is not GXArray rows || rows.Count != count)
            {
                return new() { EntriesInUse = current };
            }
            DateTimeOffset? oldest = null;
            foreach (var row in rows)
            {
                if (!TryTimestamp(new GXArray { row }, clockColumn, useUtc2NormalTime, out var captured))
                {
                    return new() { EntriesInUse = current };
                }
                oldest = !oldest.HasValue || captured < oldest.Value ? captured : oldest;
            }
            string firstValue = GXDLMSTranslator.ValueToXml(new GXArray { rows[0] });
            string lastValue = GXDLMSTranslator.ValueToXml(new GXArray { rows[rows.Count - 1] });
            object? firstAgain = readEntries(1, 1);
            token.ThrowIfCancellationRequested();
            object? lastAgain = count == 1 ? firstAgain : readEntries(count, 1);
            token.ThrowIfCancellationRequested();
            if (GXDLMSTranslator.ValueToXml(firstAgain) != firstValue || GXDLMSTranslator.ValueToXml(lastAgain) != lastValue)
            {
                return new() { EntriesInUse = current };
            }
            return new() { EntriesInUse = current, AvailableFrom = oldest };
        }
        catch (GXDLMSException)
        {
            // Entry selection and Entries In Use are optional optimizations. Range reads still work without them.
            token.ThrowIfCancellationRequested();
            return null;
        }
    }

    /// <summary>Extracts a complete captured timestamp without inventing missing clock fields or offsets.</summary>
    /// <param name="value">The decoded single-entry response.</param>
    /// <param name="column">The captured clock column.</param>
    /// <param name="reverse">Whether byte-array timestamps use reversed deviations.</param>
    /// <param name="timestamp">Receives the captured timestamp on success.</param>
    /// <returns>True when exactly one complete timestamped row was returned.</returns>
    private static bool TryTimestamp(object? value, int column, bool reverse, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (value is not GXArray rows || rows.Count != 1 || rows[0] is not GXStructure row || column < 0 || column >= row.Count)
        {
            return false;
        }
        object? clock = row[column];
        if (clock is byte[] bytes)
        {
            try
            {
                clock = GXDLMSClient.ChangeType(bytes, DataType.DateTime, reverse);
            }
            catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
            {
                return false;
            }
        }
        if (clock is GXDateTime date && (date.Skip & (DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day |
            DateTimeSkips.Hour | DateTimeSkips.Minute | DateTimeSkips.Deviation)) == 0)
        {
            timestamp = date.Value;
            return true;
        }
        if (clock is DateTimeOffset offset)
        {
            timestamp = offset;
            return true;
        }
        return false;
    }
}
