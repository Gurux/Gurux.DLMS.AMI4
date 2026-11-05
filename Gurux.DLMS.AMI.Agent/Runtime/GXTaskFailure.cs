using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Retains the failed target alongside the original operation error.</summary>
internal static class GXTaskFailure
{
    internal static string Describe(GXTask task, GXDevice? device)
    {
        var obj = task.Object?.Template != null ? task.Object : task.Attribute?.Object;
        var devices = new[] { device, task.Device, obj?.Device };
        var named = devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d?.Name));
        var id = devices.FirstOrDefault(d => d != null && d.Id != Guid.Empty)?.Id ?? task.TargetDevice;
        var fields = new List<string>();
        if (named != null)
        {
            fields.Add("Device: " + named.Name);
        }
        else if (id != null && id != Guid.Empty)
        {
            fields.Add("Device: " + id);
        }
        if (!string.IsNullOrWhiteSpace(obj?.Template?.LogicalName))
        {
            fields.Add("OBIS: " + obj.Template.LogicalName);
        }
        if (!string.IsNullOrWhiteSpace(obj?.Template?.Name))
        {
            fields.Add("Description: " + obj.Template.Name);
        }
        int? index = task.Index is > 0 ? task.Index : task.Attribute?.Template?.Index;
        if (index is > 0)
        {
            fields.Add("Attribute index: " + index);
        }
        return string.Join("; ", fields);
    }

    internal static void Record(GXTask task, GXDevice? device, string error)
    {
        string context = Describe(task, device);
        string message = context.Length == 0 ? error : context + ". " + error;
        task.Result = string.IsNullOrWhiteSpace(task.Result) ? message : task.Result + Environment.NewLine + message;
    }
}