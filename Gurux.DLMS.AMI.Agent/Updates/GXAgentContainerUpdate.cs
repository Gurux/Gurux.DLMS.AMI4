using System.Text.Json;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Hands a drained, verified agent update to the independent container updater.</summary>
internal static class GXAgentContainerUpdate
{
    internal static DateTimeOffset RetryAfter(string root)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "status.json")));
            var value = document.RootElement;
            if (value.TryGetProperty("state", out var state) && state.GetString() is "Failed" or "RolledBack" or "MaintenanceRequired" &&
                value.TryGetProperty("updatedAt", out var updated) && updated.TryGetDateTimeOffset(out var time))
            {
                var retry = time.AddMinutes(5);
                return retry > DateTimeOffset.UtcNow ? retry : default;
            }
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException) { }
        return default;
    }

    internal static async Task WaitForFailureAsync(string root, string requestId, CancellationToken token)
    {
        // Stay alive and drained while the independent updater prepares/replaces this container.
        // A stopped or uncertain updater must never cause work to resume during activation.
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "status.json"), token));
                var status = document.RootElement;
                if (status.GetProperty("requestId").GetString() == requestId &&
                    status.GetProperty("state").GetString() is "Failed" or "RolledBack" or "MaintenanceRequired" &&
                    !File.Exists(Path.Combine(root, "queue", "request.json")))
                {
                    return;
                }
            }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException) { }
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
    }

    internal static async Task<string> QueueAsync(string root, string keyFile, string version, CancellationToken token)
    {
        if (version.Length > 128 || !System.Text.RegularExpressions.Regex.IsMatch(version, @"\A[0-9]+(?:\.[0-9]+){2,3}(?:-[0-9A-Za-z.-]+)?\z"))
        {
            throw new ArgumentException("Invalid agent image version.", nameof(version));
        }
        var queue = Path.Combine(root, "queue");
        var requestPath = Path.Combine(queue, "request.json");
        using var gate = new FileStream(Path.Combine(queue, "queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(requestPath))
        {
            throw new IOException("A container update is already queued.");
        }
        var key = (await File.ReadAllTextAsync(keyFile, token)).Trim();
        if (key.Length < 64)
        {
            throw new InvalidOperationException("The container update request key is invalid.");
        }
        var id = Guid.NewGuid().ToString("N");
        var request = Gurux.Updater.Services.GXDeploymentAuthentication.Sign(version, id, key);
        var temporary = Path.Combine(queue, ".agent-request-" + id);
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(output, request, new JsonSerializerOptions(JsonSerializerDefaults.Web), token);
                output.Flush(true);
            }
            File.Move(temporary, requestPath);
            // The caller has drained work before handoff; no HTTP response needs to be sent here.
            await File.WriteAllTextAsync(Path.Combine(queue, id + ".ready"), "", token);
            return id;
        }
        finally { File.Delete(temporary); }
    }
}
