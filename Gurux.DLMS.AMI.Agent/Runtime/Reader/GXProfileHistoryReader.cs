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
using System.Net.Http.Json;
using System.Globalization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Plans and executes profile history reads using the device clock and capture layout.</summary>
internal static class GXProfileHistoryReader
{
    /// <summary>Reads permitted profile metadata or decodes its stored value for a bulk read.</summary>
    /// <param name="reader">The connected protocol reader.</param>
    /// <param name="task">The task containing the profile templates and cached metadata.</param>
    /// <param name="profile">The profile whose metadata is required.</param>
    /// <param name="index">The required metadata index.</param>
    /// <returns>The protocol-compatible metadata value.</returns>
    /// <exception cref="InvalidOperationException">Required metadata has no permitted read or cached value.</exception>
    /// <exception cref="FormatException">Cached numeric metadata is invalid.</exception>
    private static async Task<object> ReadMetadataAsync(GXDLMSReader reader, GXTask task, GXDLMSProfileGeneric profile, int index)
    {
        object? value = await GXObjectReadPolicy.ReadMetadataAsync(task.Object!, index, attribute => reader.Read(profile, attribute));
        if (value is null || value is string { Length: 0 })
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                Properties.Resources.ResourceManager.GetString("ProfileMetadataUnavailable", CultureInfo.CurrentUICulture)!, index));
        }
        if (value is string stored)
        {
            if (index is 4 or 7)
            {
                return uint.Parse(stored, CultureInfo.InvariantCulture);
            }
            if (index == 5)
            {
                return Enum.TryParse<Gurux.DLMS.Objects.Enums.SortMethod>(stored, true, out var method)
                    ? (byte)method : byte.Parse(stored, CultureInfo.InvariantCulture);
            }
        }
        return value;
    }
    /// <summary>Reads profile metadata and plans bounded history intervals before persisting the retrieved rows.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    /// <param name="reader">Protocol reader connected to the target device.</param>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="profile">Profile generic object whose buffered history is requested.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Reads profile metadata and plans bounded history intervals before persisting the retrieved rows.</returns>
    internal static async Task Read(HttpClient http, GXDLMSReader reader, GXTask task, GXDLMSProfileGeneric profile, CancellationToken token)
    {
        Guid attributeId = task.Object?.Attributes?.FirstOrDefault(a => a.Template?.Index == 2)?.Id ?? Guid.Empty;
        if (task.Id == Guid.Empty || attributeId == Guid.Empty)
            {
            throw new InvalidOperationException("Profile history requires persisted task and buffer attribute identifiers.");
        }
        task.Index = 7;
        uint entries = Convert.ToUInt32(await ReadMetadataAsync(reader, task, profile, 7), CultureInfo.InvariantCulture);
        task.Index = 2;
        if (entries == 0)
        {
            return;
        }
        if (entries == 1)
        {
            await TryReadSnapshot(profile, task, attributeId, index => reader.Read(profile, index),
                value => http.PostAsJson("api/Value/Add", value, token), token, force: true);
            return;
        }
        // Read the meter's current layout, rather than guessing the first capture column is a clock.
        int sortAttribute = 0, sortData = 0;
        foreach (int index in new[] { 3, 4, 5, 6 })
        {
            token.ThrowIfCancellationRequested();
            task.Index = index;
            object value = await ReadMetadataAsync(reader, task, profile, index);
            object decoded = value is string xml && (index == 3 || index == 6)
                ? GXDLMSTranslator.XmlToValue(xml) : value;
            reader.Client.UpdateValue(profile, index, decoded);
            if (index == 6)
            {
                (sortAttribute, sortData) = ParseSortDescriptor(decoded);
            }
        }
        task.Index = 2;
        var clockColumn = GetClockColumn(profile, sortAttribute, sortData);
        int capturePeriod = (int)profile.SortMethod == 1 ? checked((int)profile.CapturePeriod) : 0;
        bool readAll = false;
        if (!string.IsNullOrWhiteSpace(task.Data))
        {
            try
            {
                using var intent = System.Text.Json.JsonDocument.Parse(task.Data);
                readAll = intent.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                    intent.RootElement.EnumerateObject().Any(property =>
                        property.Name.Equals("ReadAll", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == System.Text.Json.JsonValueKind.True);
            }
            catch (System.Text.Json.JsonException)
            {
                // Begin retains responsibility for rejecting invalid serialized task intents.
            }
        }
        GXArray? collected = null;
        if (readAll)
        {
            collected = await GXProfileBufferCollector.ReadAllAsync(entries, DateTimeOffset.Now,
                (index, count) => reader.ReadRowsByEntry(profile, index, count),
                (start, end) => reader.ReadRowsByRange(profile, start.UtcDateTime, end.UtcDateTime),
                () => reader.Read(profile, 2),
                (rows, start, end) =>
                {
                    if (clockColumn < 0)
                    {
                        throw new InvalidOperationException(Properties.Resources.ResourceManager.GetString("ProfileRangeClockRequired", CultureInfo.CurrentUICulture));
                    }
                    var selected = new GXArray();
                    selected.AddRange(DecodeRows(rows, clockColumn, capturePeriod, reader.Client.UseUtc2NormalTime)
                        .Where(row => row.CapturedAt >= start && row.CapturedAt < end)
                        .Select(row => GXDLMSTranslator.XmlToValue(row.Value)));
                    return selected;
                },
                Properties.Resources.ResourceManager.GetString("ProfileBufferReadIncomplete", CultureInfo.CurrentUICulture)!, token);
        }
        if (clockColumn < 0)
        {
            await TryReadSnapshot(profile, task, attributeId,
                index => collected != null ? Task.FromResult<object>(collected) : reader.Read(profile, index),
                value => http.PostAsJson("api/Value/Add", value, token), token);
            return;
        }
        using var begin = new HttpRequestMessage(HttpMethod.Post, "api/ProfileHistory/Begin")
        {
            Content = JsonContent.Create(new GXProfileBeginRequest { TaskId = task.Id, AttributeId = attributeId })
        };
        begin.Headers.Add("X-Gurux-Profile-Entries", entries.ToString(CultureInfo.InvariantCulture));
        if (collected?.Count > 0)
        {
            var captured = DecodeRows(collected, clockColumn, capturePeriod, reader.Client.UseUtc2NormalTime);
            begin.Headers.Add("X-Gurux-Profile-Oldest", captured.Min(row => row.CapturedAt).ToString("O", CultureInfo.InvariantCulture));
            begin.Headers.Add("X-Gurux-Profile-Newest", captured.Max(row => row.CapturedAt).ToString("O", CultureInfo.InvariantCulture));
        }
        using var beginResponse = await http.SendAsync(begin, token);
        await beginResponse.EnsureSuccessAsync(token);
        var response = await beginResponse.Content.ReadFromJsonAsync<GXProfileBeginResponse>(token)
            ?? throw new InvalidOperationException("Profile history begin returned no response.");
        if (response.Intervals.Any(job => job.TaskId != task.Id || job.AttributeId != attributeId))
            {
            throw new InvalidOperationException("Profile history response targets a different task or attribute.");
        }
        await Execute(response.Intervals, clockColumn, capturePeriod,
            (start, end) => collected != null ? collected : reader.ReadRowsByRange(profile, start.UtcDateTime, end.UtcDateTime),
            commit => http.PostAsJson("api/ProfileHistory/Commit", commit, token), token, reader.Client.UseUtc2NormalTime);
    }

    /// <summary>Reads a complete snapshot when no clock is captured or direct single-entry reading is required.</summary>
    /// <param name="profile">Profile generic object whose buffered history is requested.</param>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="attributeId">Server attribute identifier receiving the profile metadata value.</param>
    /// <param name="read">Reads the complete buffer attribute from the device.</param>
    /// <param name="save">Persists a successfully read profile metadata value.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <param name="force">Whether the single-entry buffer must be read directly even when a clock is captured.</param>
    /// <returns>True when the full buffer was saved, or false when a captured clock requires history reads.</returns>
    internal static async Task<bool> TryReadSnapshot(GXDLMSProfileGeneric profile, GXTask task, Guid attributeId,
        Func<int, Task<object>> read, Func<AddValue, Task> save, CancellationToken token = default, bool force = false)
    {
        token.ThrowIfCancellationRequested();
        if (!force && profile.CaptureObjects.Any(c => c.Key.ObjectType == ObjectType.Clock &&
            c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0))
        {
            return false;
        }
        task.Index = 2;
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

    /// <summary>Uses the captured clock identified by the sort object, then searches the capture list.</summary>
    /// <param name="profile">The profile whose capture layout was read from the meter.</param>
    /// <param name="sortAttribute">Attribute index from the sort descriptor.</param>
    /// <param name="sortData">Data index from the sort descriptor.</param>
    /// <returns>The captured clock column, or minus one when the full buffer must be read.</returns>
    internal static int GetClockColumn(GXDLMSProfileGeneric profile, int sortAttribute, int sortData)
    {
        if (profile.SortObject?.ObjectType == ObjectType.Clock &&
            sortAttribute == 2 && sortData == 0)
        {
            int sortedColumn = profile.CaptureObjects.FindIndex(c => c.Key.ObjectType == ObjectType.Clock &&
                c.Key.LogicalName == profile.SortObject.LogicalName && c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0);
            if (sortedColumn >= 0)
            {
                return sortedColumn;
            }
        }
        int clockColumn = profile.CaptureObjects.FindIndex(c => c.Key.ObjectType == ObjectType.Clock &&
             c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0);
        if (clockColumn >= 0)
        {
            // Select the request's restricting object locally without changing the meter configuration.
            profile.SortObject = profile.CaptureObjects[clockColumn].Key;
        }
        return clockColumn;
    }

    /// <summary>Extracts attribute and data indexes from the decoded profile sort descriptor.</summary>
    /// <param name="decoded">Decoded DLMS sort descriptor value.</param>
    /// <returns>The attribute and data indexes decoded from the profile sort descriptor.</returns>
    internal static (int Attribute, int Data) ParseSortDescriptor(object decoded)
    {
        if (decoded is not GXStructure sort || sort.Count != 4)
            {
            throw new InvalidOperationException("Profile sort descriptor is invalid.");
        }
        return (Convert.ToInt32(sort[2]), Convert.ToInt32(sort[3]));
    }

    /// <summary>Decodes captured timestamps while retaining the meter's raw row XML and repeated rows.</summary>
    /// <param name="value">The raw buffer array.</param>
    /// <param name="clockColumn">The captured clock column.</param>
    /// <param name="capturePeriod">The period used to infer omitted consecutive timestamps.</param>
    /// <param name="useUtc2NormalTime">Whether clock deviations use the reversed convention.</param>
    /// <returns>The decoded rows in their meter order.</returns>
    /// <exception cref="InvalidOperationException">A row lacks reliable layout or timestamp metadata.</exception>
    private static List<GXProfileRow> DecodeRows(GXArray value, int clockColumn, int capturePeriod, bool useUtc2NormalTime)
    {
        var rows = new List<GXProfileRow>();
        DateTimeOffset? previous = null;
        foreach (object item in value as GXArray ?? [])
        {
            if (item is not GXStructure row || clockColumn >= row.Count)
                {
                throw new InvalidOperationException("Profile row does not match its capture layout.");
            }
            object? clock = row[clockColumn];
            if (clock is byte[] bytes) {
                clock = GXDLMSClient.ChangeType(bytes, DataType.DateTime, useUtc2NormalTime);
            }
            if (clock is GXDateTime date && (date.Skip & (DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute | DateTimeSkips.Deviation)) != 0)
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
            rows.Add(new GXProfileRow { CapturedAt = captured, Value = xml });
        }
        return rows;
    }

    /// <summary>Executes planned profile intervals and persists their ordered history results.</summary>
    /// <param name="jobs">Planned profile time intervals to retrieve.</param>
    /// <param name="clockColumn">Captured value column containing profile timestamps.</param>
    /// <param name="capturePeriod">Profile capture interval in seconds.</param>
    /// <param name="read">Reads one planned profile interval from the device.</param>
    /// <param name="commit">Persists the decoded profile interval results.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <param name="useUtc2NormalTime">Whether device timestamps use UTC-to-local conversion when planning intervals.</param>
    /// <returns>A task representing this operation: Executes planned profile intervals and persists their ordered history results.</returns>
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
            var rows = DecodeRows((GXArray)value, clockColumn, capturePeriod, useUtc2NormalTime)
                .Where(row => row.CapturedAt >= job.Start && row.CapturedAt < job.End).ToArray();
            token.ThrowIfCancellationRequested();
            await commit(new GXProfileCommit { SpanId = job.IntervalId, TaskId = job.TaskId, Completed = true, Rows = [.. rows] });
        }
    }
}

