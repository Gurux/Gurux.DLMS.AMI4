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

using Task = System.Threading.Tasks.Task;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.AMI.Agent.Worker;

internal static class GXProfileHistoryReader
{
    internal static async Task Read(HttpClient http, GXDLMSReader reader, GXTask task, GXDLMSProfileGeneric profile, CancellationToken token)
    {
        Guid attributeId = task.Object?.Attributes?.FirstOrDefault(a => a.Template?.Index == 2)?.Id ?? Guid.Empty;
        if (task.Id == Guid.Empty || attributeId == Guid.Empty)
            {
            throw new InvalidOperationException("Profile history requires persisted task and buffer attribute identifiers.");
        }
        // Read the meter's current layout, rather than guessing the first capture column is a clock.
        int sortAttribute = 0, sortData = 0;
        foreach (int index in new[] { 3, 4, 5, 6 })
        {
            token.ThrowIfCancellationRequested();
            task.Index = index;
            object value = await reader.Read(profile, index);
            if (value is string xml && (index == 3 || index == 6))
            {
                object decoded = GXDLMSTranslator.XmlToValue(xml);
                reader.Client.UpdateValue(profile, index, decoded);
                if (index == 6)
                    {
                    (sortAttribute, sortData) = ParseSortDescriptor(decoded);
                }
            }
            if (index == 3)
            {
                task.Index = 2;
                if (await TryReadNameplate(profile, task, attributeId, i => reader.Read(profile, i),
                    value => http.PostAsJson("api/Value/Add", value, token), token))
                {
                    return;
                }
            }
        }
        task.Index = 2;
        var clockColumn = GetClockColumn(profile, sortAttribute, sortData);
        var response = await http.PostAsJson<GXProfileBeginResponse>("api/ProfileHistory/Begin", new GXProfileBeginRequest { TaskId = task.Id, AttributeId = attributeId }, token)
            ?? throw new InvalidOperationException("Profile history begin returned no response.");
        if (response.Intervals.Any(job => job.TaskId != task.Id || job.AttributeId != attributeId))
            {
            throw new InvalidOperationException("Profile history response targets a different task or attribute.");
        }
        await Execute(response.Intervals, clockColumn, (int)profile.SortMethod == 1 ? checked((int)profile.CapturePeriod) : 0,
            (start, end) => reader.ReadRowsByRange(profile, start.UtcDateTime, end.UtcDateTime),
            commit => http.PostAsJson("api/ProfileHistory/Commit", commit, token), token, reader.Client.UseUtc2NormalTime);
    }

    /// <summary>Reads a clockless single-row profile as an ordinary buffer snapshot.</summary>
    internal static async Task<bool> TryReadNameplate(GXDLMSProfileGeneric profile, GXTask task, Guid attributeId,
        Func<int, Task<object>> read, Func<AddValue, Task> save, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (profile.CaptureObjects.Any(c => c.Key.ObjectType == ObjectType.Clock &&
            c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0))
        {
            return false;
        }
        // Read Entries in use from the meter; a cached count or capacity is not sufficient.
        task.Index = 7;
        object entries = await read(7);
        token.ThrowIfCancellationRequested();
        uint count = Convert.ToUInt32(entries, System.Globalization.CultureInfo.InvariantCulture);
        task.Index = 2;
        if (count != 1)
        {
            return false;
        }
        object buffer = await read(2);
        token.ThrowIfCancellationRequested();
        if (buffer is not GXArray)
        {
            throw new InvalidOperationException("Profile buffer read did not return an array.");
        }
        // Preserve the full buffer, including non-date octets. Read is the snapshot read time,
        // not a fabricated captured timestamp, and no time-range coverage is recorded.
        task.Data = GXDLMSTranslator.ValueToXml(buffer);
        await save(new AddValue
        {
            Values = [new GXValue
            {
                Attribute = new GXAttribute { Id = attributeId },
                Read = DateTime.UtcNow,
                Value = task.Data
            }]
        });
        return true;
    }

    internal static int GetClockColumn(GXDLMSProfileGeneric profile, int sortAttribute, int sortData)
    {
        if (profile.SortObject == null)
        {
            // Meters can omit the sort descriptor while capturing a clock. Range selection uses
            // the standard clock, so resolve its actual capture column instead of assuming column zero.
            var clocks = profile.CaptureObjects.Where(c => c.Key.ObjectType == ObjectType.Clock &&
                c.Key.LogicalName == "0.0.1.0.0.255" && c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0).ToArray();
            if (sortAttribute != 0 || sortData != 0 || clocks.Length != 1)
            {
                throw new InvalidOperationException("Profile history requires exactly one captured standard clock (0.0.1.0.0.255, attribute 2) when the sort object is not defined.");
            }
            // This changes only the client's local request layout; no meter setting is written.
            profile.SortObject = clocks[0].Key;
            sortAttribute = 2;
            sortData = 0;
        }
        int clockColumn = profile.CaptureObjects.FindIndex(c => c.Key.ObjectType == profile.SortObject.ObjectType && c.Key.LogicalName == profile.SortObject.LogicalName && c.Value.AttributeIndex == sortAttribute && c.Value.DataIndex == sortData);
        if (clockColumn < 0 || profile.SortObject.ObjectType != ObjectType.Clock || sortAttribute != 2 || sortData != 0 || profile.SortObject.LogicalName != "0.0.1.0.0.255")
            {
            throw new InvalidOperationException("Profile history requires a captured clock as its sort object.");
        }
        return clockColumn;
    }

    internal static (int Attribute, int Data) ParseSortDescriptor(object decoded)
    {
        if (decoded is not GXStructure sort || sort.Count != 4)
            {
            throw new InvalidOperationException("Profile sort descriptor is invalid.");
        }
        return (Convert.ToInt32(sort[2]), Convert.ToInt32(sort[3]));
    }

    internal static async Task Execute(IEnumerable<GXProfileReadInterval> jobs, int clockColumn, int capturePeriod,
        Func<DateTimeOffset, DateTimeOffset, object?> read, Func<GXProfileCommit, Task> commit, CancellationToken token = default, bool useUtc2NormalTime = false)
    {
        foreach (var job in jobs)
        {
            token.ThrowIfCancellationRequested();
            if (job.IntervalId == Guid.Empty || job.TaskId == Guid.Empty || job.AttributeId == Guid.Empty || job.End <= job.Start || job.End - job.Start > TimeSpan.FromHours(24))
                {
                throw new InvalidOperationException("Invalid profile history read job.");
            }
            // Selective access endpoints are inclusive on some meters. Filtering below owns [start,end).
            var selectionStart = job.End - job.Start < TimeSpan.FromHours(24)
                ? job.Start.AddSeconds(-Math.Min(1, (TimeSpan.FromHours(24) - (job.End - job.Start)).TotalSeconds)) : job.Start;
            object? value = read(selectionStart, job.End);
            token.ThrowIfCancellationRequested();
            if (value is not GXArray)
                {
                throw new InvalidOperationException("Profile range read did not return an array.");
            }
            var rows = new List<GXProfileRow>();
            DateTimeOffset? previous = null;
            foreach (object item in value as GXArray ?? new GXArray())
            {
                if (item is not GXStructure row || clockColumn >= row.Count)
                    {
                    throw new InvalidOperationException("Profile row does not match its capture layout.");
                }
                object? clock = row[clockColumn];
                if (clock is byte[] bytes) {
                    clock = GXDLMSClient.ChangeType(bytes, DataType.DateTime, useUtc2NormalTime);
                }
                if (clock is GXDateTime date && (date.Skip & (DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute)) != 0)
                    {
                    throw new InvalidOperationException("Profile clock has an incomplete timestamp or time zone deviation.");
                }
                DateTimeOffset captured = clock switch
                {
                    GXDateTime dt => dt.Value,
                    DateTimeOffset dt => dt,
                    null when previous != null && capturePeriod > 0 => previous.Value.AddSeconds(capturePeriod),
                    _ => throw new InvalidOperationException("Profile row has no reliable captured timestamp.")
                };
                previous = captured;
                // Retain meter row XML, including non-clock octet strings and explicit date-time offsets.
                string xml = GXDLMSTranslator.ValueToXml(row);
                if (captured >= job.Start && captured < job.End)
                    {
                    rows.Add(new GXProfileRow { CapturedAt = captured, Value = xml });
                }
            }
            token.ThrowIfCancellationRequested();
            await commit(new GXProfileCommit { SpanId = job.IntervalId, TaskId = job.TaskId, Completed = true, Rows = rows.ToArray() });
        }
    }
}

