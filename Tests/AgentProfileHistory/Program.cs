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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.Rest;
await NameplateChecks.RunAsync();
var start = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
var job = new GXProfileReadInterval { IntervalId = Guid.NewGuid(), TaskId = Guid.NewGuid(), AttributeId = Guid.NewGuid(), Start = start, End = start.AddHours(1) };
var commits = new List<GXProfileCommit>();
var rows = new GXArray { new GXStructure { 7, new GXDateTime(start) }, new GXStructure { 8, new GXDateTime(start) }, new GXStructure { 9, new GXDateTime(job.End) } };
await GXProfileHistoryReader.Execute([job], 1, 900, (a,b) => rows, c => { commits.Add(c); return Task.CompletedTask; });
if (commits.Single().Rows.Length != 2 || commits[0].Rows.Any(r => r.CapturedAt.Offset != start.Offset)) {
    throw new Exception("Backfill, equal timestamps, clock column, offset or boundary filtering failed.");
}
commits.Clear();
await GXProfileHistoryReader.Execute([job], 1, 900, (a,b) => new GXArray(), c => { commits.Add(c); return Task.CompletedTask; });
if (commits.Single().Rows.Length != 0 || !commits[0].Completed) {
    throw new Exception("Empty success must complete.");
}
commits.Clear();
try { await GXProfileHistoryReader.Execute([job], 1, 900, (a,b) => throw new InvalidOperationException("meter failed"), c => { commits.Add(c); return Task.CompletedTask; }); throw new Exception("Expected read failure."); } catch (InvalidOperationException) { }
if (commits.Count != 0) {
    throw new Exception("Failed read committed coverage.");
}
commits.Clear();
var reconstructed = new GXArray { new GXStructure { new GXDateTime(start) }, new GXStructure { null } };
await GXProfileHistoryReader.Execute([job], 0, 60, (a,b) => reconstructed, c => { commits.Add(c); return Task.CompletedTask; });
if (commits.Single().Rows[1].CapturedAt != start.AddSeconds(60)) {
    throw new Exception("Null timestamp reconstruction must use seconds.");
}
commits.Clear();
try { await GXProfileHistoryReader.Execute([job], 0, 0, (a,b) => new GXArray { new GXStructure { null } }, c => { commits.Add(c); return Task.CompletedTask; }); throw new Exception("Expected unreliable clock failure."); } catch (InvalidOperationException) { }
if (commits.Count != 0) {
    throw new Exception("Unreliable timestamps committed coverage.");
}
using var canceled = new CancellationTokenSource();
try { await GXProfileHistoryReader.Execute([job], 0, 60, (a,b) => { canceled.Cancel(); return null; }, c => { commits.Add(c); return Task.CompletedTask; }, canceled.Token); throw new Exception("Expected cancellation."); } catch (OperationCanceledException) { }
if (commits.Count != 0) {
    throw new Exception("Canceled read committed coverage.");
}




var profile = new Gurux.DLMS.Objects.GXDLMSProfileGeneric();
var clockObject = new Gurux.DLMS.Objects.GXDLMSClock();
profile.CaptureObjects.Add(new(new Gurux.DLMS.Objects.GXDLMSData(), new Gurux.DLMS.Objects.GXDLMSCaptureObject(2,0)));
profile.CaptureObjects.Add(new(clockObject, new Gurux.DLMS.Objects.GXDLMSCaptureObject(2,0)));
profile.SortObject = clockObject;
var sortDescriptor = GXProfileHistoryReader.ParseSortDescriptor(new GXStructure { (ushort)8, new byte[] {0,0,1,0,0,255}, (sbyte)2, (ushort)0 });
var layout = GXProfileHistoryReader.GetClockColumn(profile, sortDescriptor.Attribute, sortDescriptor.Data);
if (layout != 1) {
    throw new Exception("Actual sort descriptor clock selection failed.");
}
profile.SortObject = null;
var unsortedLayout = GXProfileHistoryReader.GetClockColumn(profile, 0, 0);
if (unsortedLayout != 1 || profile.SortObject != clockObject)
{
    throw new Exception("A missing sort object must use the unique captured standard clock, not the first capture column.");
}
foreach (int clockCount in new[] { 0, 2 })
{
    var invalid = new Gurux.DLMS.Objects.GXDLMSProfileGeneric();
    for (int i = 0; i < clockCount; ++i)
    {
        invalid.CaptureObjects.Add(new(new Gurux.DLMS.Objects.GXDLMSClock(), new Gurux.DLMS.Objects.GXDLMSCaptureObject(2, 0)));
    }
    bool rejected = false;
    try
    {
        GXProfileHistoryReader.GetClockColumn(invalid, 0, 0);
    }
    catch (InvalidOperationException)
    {
        rejected = true;
    }
    if (!rejected || invalid.SortObject != null)
    {
        throw new Exception("A missing or ambiguous clock must not fabricate a profile-history layout.");
    }
}
profile.CapturePeriod = 900;
clockObject.LogicalName = "0.0.1.0.1.255";
try { GXProfileHistoryReader.GetClockColumn(profile, 2, 0); throw new Exception("Expected unsupported custom clock failure."); } catch (InvalidOperationException) { }
commits.Clear();
try { await GXProfileHistoryReader.Execute([job], 0, 60, (a,b) => null, c => { commits.Add(c); return Task.CompletedTask; }); throw new Exception("Expected null response failure."); } catch (InvalidOperationException) { }
var unknownDeviation = new GXDateTime(start) { Skip = Gurux.DLMS.Enums.DateTimeSkips.Deviation };
try { await GXProfileHistoryReader.Execute([job], 0, 60, (a,b) => new GXArray { new GXStructure { unknownDeviation } }, c => { commits.Add(c); return Task.CompletedTask; }); throw new Exception("Expected deviation failure."); } catch (InvalidOperationException) { }
if (commits.Count != 0) {
    throw new Exception("Invalid response committed coverage.");
}
// DLMS deviation +120 denotes UTC-2 normally and UTC+2 with reversed deviation configuration.
byte[] encoded = [0x07,0xE4,1,1,3,0,0,0,0,0,120,0];
foreach (bool reverse in new[] { false, true })
{
    var expected = (GXDateTime)GXDLMSClient.ChangeType(encoded, Gurux.DLMS.Enums.DataType.DateTime, reverse);
    if (expected.Value.Offset != TimeSpan.FromHours(reverse ? 2 : -2)) {
        throw new Exception("Unexpected deviation convention.");
    }
    var zoneJob = new GXProfileReadInterval { IntervalId = Guid.NewGuid(), TaskId = Guid.NewGuid(), AttributeId = Guid.NewGuid(), Start = expected.Value, End = expected.Value.AddHours(1) };
    commits.Clear();
    await GXProfileHistoryReader.Execute([zoneJob], 0, 60, (a,b) => new GXArray { new GXStructure { encoded } }, c => { commits.Add(c); return Task.CompletedTask; }, useUtc2NormalTime: reverse);
    if (commits.Single().Rows.Single().CapturedAt != expected.Value || commits[0].Rows[0].CapturedAt.Offset != expected.Value.Offset) {
        throw new Exception("Configured deviation convention was ignored.");
    }
}
var wildcard = new GXDateTime(start) { Skip = Gurux.DLMS.Enums.DateTimeSkips.Year };
try { await GXProfileHistoryReader.Execute([job], 0, 60, (a,b) => new GXArray { new GXStructure { wildcard } }, c => throw new Exception("Wildcard timestamp committed")); throw new Exception("Expected wildcard rejection."); } catch (InvalidOperationException) { }
Console.WriteLine("Agent profile history checks passed.");


