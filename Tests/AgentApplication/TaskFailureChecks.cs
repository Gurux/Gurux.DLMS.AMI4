using System.Net;
using System.Reflection;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.Secure;

internal static class TaskFailureChecks
{
    internal static async Task RunAsync()
    {
        using var http = new HttpClient(new Logs()) { BaseAddress = new Uri("https://ami.test/") };
        await using var worker = new GXAgentWorker(http);
        var execute = typeof(GXAgentWorker).GetMethod("ExecuteTasks", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var device = new GXDevice { Id = Guid.NewGuid(), Name = "Meter A" };
        var obj = new GXObject
        {
            Device = device,
            Template = new GXObjectTemplate { ObjectType = 1, LogicalName = "1.0.1.5.0.255", Name = "Active energy" },
            Attributes = [new GXAttribute { Template = new GXAttributeTemplate { Index = 2, AccessLevel = 3 } }]
        };
        var read = new GXTask { TaskType = TaskType.Read, Object = obj };
        await (Task)execute.Invoke(worker, [device, null, new GXDLMSSecureClient(), read, CancellationToken.None])!;
        Check(read.Result, "Meter A", "1.0.1.5.0.255", "Active energy", "Attribute index: 2");
        var write = new GXTask { TaskType = TaskType.Write, Object = obj, Index = 2, Data = "<invalid>" };
        await (Task)execute.Invoke(worker, [device, null, new GXDLMSSecureClient(), write, CancellationToken.None])!;
        Check(write.Result, "Meter A", "1.0.1.5.0.255", "Active energy", "Attribute index: 2");
        var next = new GXObject
        {
            Device = device,
            Template = new GXObjectTemplate { ObjectType = 1, LogicalName = "1.0.2.5.0.255", Name = "Export energy" },
            Attributes = [new GXAttribute { Template = new GXAttributeTemplate { Index = 2, AccessLevel = 3 } }]
        };
        var whole = new GXTask { TaskType = TaskType.Read, Device = new GXDevice { Objects = [obj, next] } };
        await (Task)execute.Invoke(worker, [device, null, new GXDLMSSecureClient(), whole, CancellationToken.None])!;
        Check(whole.Result, "1.0.1.5.0.255", "Active energy", "1.0.2.5.0.255", "Export energy");
        var attributeTask = new GXTask { TaskType = TaskType.Write, Attribute = new GXAttribute { Object = obj, Template = new GXAttributeTemplate { Index = 2, AccessLevel = 3 } }, Index = 2, Data = "<invalid>" };
        await (Task)execute.Invoke(worker, [device, null, new GXDLMSSecureClient(), attributeTask, CancellationToken.None])!;
        Check(attributeTask.Result, "Meter A", "1.0.1.5.0.255", "Active energy", "Attribute index: 2");
        var missing = new GXTask { TargetDevice = device.Id, Index = 0 };
        GXTaskFailure.Record(missing, null, "Insufficient permissions.");
        Check(missing.Result, device.Id.ToString(), "Insufficient permissions.");
        if (missing.Result!.Contains("OBIS:") || missing.Result.Contains("Attribute index:"))
        {
            throw new Exception("Missing target metadata produced misleading placeholder values.");
        }
        Console.WriteLine("PASS read/write failures retain device, OBIS, description and attribute index, including each whole-device failure.");
    }

    private static void Check(string? result, params string[] expected)
    {
        if (result == null || expected.Any(value => !result.Contains(value, StringComparison.Ordinal)))
        {
            throw new Exception("Operation error is missing target context: " + result);
        }
    }

    private sealed class Logs : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}