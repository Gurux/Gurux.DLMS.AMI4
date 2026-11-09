using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Module.Health;
using Microsoft.Extensions.Hosting;
namespace Gurux.DLMS.AMI.Server.Module;
public sealed partial class GXModuleRuntime
{
    private readonly Dictionary<string, AmiModuleHealth> completedHealth = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Reads status without resolving module services or acquiring a runtime lease.</summary>
    public AmiModuleHealth? GetHealth(string id)
    {
        lock (sync)
        {
            if (entries.TryGetValue(id, out var entry))
            {
                return SnapshotHealth(entry, entry.Manifest.State == AmiModuleState.Failed ? AmiModuleState.Failed : entry.State);
            }
            if (completedHealth.TryGetValue(id, out var completed))
            {
                return completed;
            }
            if (manifests.TryGetValue(id, out var manifest))
            {
                return new(id, "", manifest.State, 0, null, null, null, false, false,
                    manifest.Error == null ? null : "Module failed. See authorized module logs for details.", []);
            }
            return null;
        }
    }
    private static AmiModuleBackgroundHealth[] SnapshotBackground(Entry entry)
    {
        return entry.Background.Length == 0 ? entry.BackgroundHealth : [.. entry.Background.Select(service =>
        {
            var task = (service as BackgroundService)?.ExecuteTask;
            string state = task == null ? (entry.State == AmiModuleState.Stopping ? "Stopping" : "Started") :
                task.IsFaulted ? "Faulted" : task.IsCanceled ? "Canceled" : task.IsCompleted ? "Completed" :
                entry.State == AmiModuleState.Stopping ? "Stopping" : "Running";
            return new AmiModuleBackgroundHealth(service.GetType().Name, state);
        })];
    }
    private static AmiModuleHealth SnapshotHealth(Entry entry, AmiModuleState state)
    {
        return new(entry.Manifest.Id, entry.Generation, state, entry.Calls, entry.StartedAt,
            entry.StopRequestedAt, entry.StoppedAt, entry.StoppedAt == null && entry.State == AmiModuleState.Stopping,
            entry.ForcedCancellation, entry.Manifest.Error != null || entry.CleanupError != null || state == AmiModuleState.Failed
                ? "Module failed. See authorized module logs for details." : null, SnapshotBackground(entry));
    }
}
