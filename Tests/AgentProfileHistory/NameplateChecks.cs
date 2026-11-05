using Gurux.DLMS;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Objects;

internal static class NameplateChecks
{
    internal static async Task RunAsync()
    {
        var profile = new GXDLMSProfileGeneric();
        profile.CaptureObjects.Add(new(new GXDLMSData(), new GXDLMSCaptureObject(2, 0)));
        var task = new GXTask { Id = Guid.NewGuid() };
        var attributeId = Guid.NewGuid();
        var reads = new List<int>();
        var saved = new List<AddValue>();
        byte[] identifier = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var buffer = new GXArray { new GXStructure { "manufacturer", identifier } };
        Task<object> Read(int index)
        {
            reads.Add(index);
            return Task.FromResult<object>(index == 7 ? 1U : index == 2 ? buffer : throw new Exception("Unexpected attribute read."));
        }
        if (!await GXProfileHistoryReader.TryReadNameplate(profile, task, attributeId, Read, request => { saved.Add(request); return Task.CompletedTask; }))
        {
            throw new Exception("A clockless one-row nameplate must read and save its entire buffer.");
        }
        var value = saved.Single().Values.Single();
        if (!reads.SequenceEqual(new[] { 7, 2 }) || value.Attribute?.Id != attributeId || value.Read == null || value.Value != GXDLMSTranslator.ValueToXml(buffer) || task.Data != value.Value)
        {
            throw new Exception("Nameplate read order, target attribute, read time or full buffer data was lost.");
        }
        var decoded = (GXArray)GXDLMSTranslator.XmlToValue(value.Value);
        if (!((byte[])((GXStructure)decoded[0])[1]).SequenceEqual(identifier))
        {
            throw new Exception("Nameplate octets were incorrectly interpreted as a timestamp.");
        }
        foreach (uint count in new uint[] { 0, 2 })
        {
            reads.Clear();
            saved.Clear();
            bool handled = await GXProfileHistoryReader.TryReadNameplate(profile, task, attributeId,
                index => { reads.Add(index); return Task.FromResult<object>(count); }, request => { saved.Add(request); return Task.CompletedTask; });
            if (handled || !reads.SequenceEqual(new[] { 7 }) || saved.Count != 0)
            {
                throw new Exception("Only Entries in use = 1 can select the nameplate path.");
            }
        }
        var timed = new GXDLMSProfileGeneric();
        timed.CaptureObjects.Add(new(new GXDLMSClock(), new GXDLMSCaptureObject(2, 0)));
        if (await GXProfileHistoryReader.TryReadNameplate(timed, task, attributeId,
            _ => throw new Exception("A timestamped profile must retain history reads."), _ => throw new Exception("Unexpected save.")))
        {
            throw new Exception("Timestamped profile was treated as a nameplate.");
        }
        saved.Clear();
        bool failed = false;
        try
        {
            await GXProfileHistoryReader.TryReadNameplate(profile, task, attributeId,
                index => index == 7 ? Task.FromResult<object>(1U) : throw new IOException("Meter buffer read failed."),
                request => { saved.Add(request); return Task.CompletedTask; });
        }
        catch (IOException)
        {
            failed = true;
        }
        if (!failed || saved.Count != 0)
        {
            throw new Exception("A failed nameplate read saved data.");
        }
        using var cancellation = new CancellationTokenSource();
        bool cancelled = false;
        reads.Clear();
        try
        {
            await GXProfileHistoryReader.TryReadNameplate(profile, task, attributeId,
                index =>
                {
                    reads.Add(index);
                    cancellation.Cancel();
                    return Task.FromResult<object>(1U);
                }, _ => throw new Exception("A cancelled nameplate read saved data."), cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        if (!cancelled || !reads.SequenceEqual(new[] { 7 }))
        {
            throw new Exception("Cancellation after Entries must prevent reading the full buffer.");
        }
        Console.WriteLine("PASS clockless nameplate reads fresh Entries in use, reads all rows only for count 1, preserves octets and bypasses time-series history.");
    }
}