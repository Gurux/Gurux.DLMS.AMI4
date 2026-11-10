using System.Diagnostics;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Persists a fresh connected-runtime heartbeat for container health and update verification.</summary>
internal static class GXAgentContainerHealth
{
    private sealed record Status(string Version, bool Running, bool Drained, int ProcessId, long ProcessStartedAt, string BootId, DateTimeOffset UpdatedAt);
    private static readonly long StartedAt = ReadProcessStart(Environment.ProcessId);
    private static readonly string BootIdentity = OperatingSystem.IsLinux() ? File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim() : "";
    private static readonly SemaphoreSlim WriteGate = new(1, 1);
    private static int drained;
    internal static void MarkDrained() => Volatile.Write(ref drained, 1);
    internal static void Resume() => Volatile.Write(ref drained, 0);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static bool IsHealthy(string root, string version)
    {
        try
        {
            var value = JsonSerializer.Deserialize<Status>(File.ReadAllText(Path.Combine(root, "queue", "agent-health.json")), Json);
            return value != null && (value.Running || value.Drained) && value.Version == version && value.ProcessId > 0 && value.BootId == BootIdentity &&
                ProcessMatches(value.ProcessId, value.ProcessStartedAt) &&
                value.UpdatedAt <= DateTimeOffset.UtcNow.AddSeconds(5) && DateTimeOffset.UtcNow - value.UpdatedAt < TimeSpan.FromSeconds(30);
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return false; }
    }
    private static long ReadProcessStart(int id)
    {
        if (OperatingSystem.IsLinux())
        {
            // Process.StartTime converts a monotonic clock to UTC and can differ between readers.
            // proc field 22 is the stable kernel start identifier; skip the parenthesized command first.
            var stat = File.ReadAllText($"/proc/{id}/stat");
            var fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return long.Parse(fields[19], System.Globalization.CultureInfo.InvariantCulture);
        }
        using var process = Process.GetProcessById(id);
        return process.StartTime.ToUniversalTime().Ticks;
    }
    private static bool ProcessMatches(int id, long startedAt)
    {
        try
        {
            using var process = Process.GetProcessById(id);
            return !process.HasExited && ReadProcessStart(id) == startedAt;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
    internal static async Task WriteAsync(string root, string version, bool running, CancellationToken token)
    {
        var path = Path.Combine(root, "queue", "agent-health.json");
        var temporary = path + "." + Guid.NewGuid().ToString("N");
        await WriteGate.WaitAsync(token);
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new Status(version, running, Volatile.Read(ref drained) != 0, Environment.ProcessId, StartedAt, BootIdentity, DateTimeOffset.UtcNow), Json), token);
            File.Move(temporary, path, true);
        }
        finally
        {
            try { File.Delete(temporary); }
            finally { WriteGate.Release(); }
        }
    }
    internal static async Task RunAsync(string root, string version, Func<bool> running, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await WriteAsync(root, version, running(), token); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Container health storage is unavailable; the updater will wait for a fresh heartbeat.");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }
}
