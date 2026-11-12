using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>An inspected replacement ZIP to retain for the next startup.</summary>
/// <param name="Id">Module identity.</param>
/// <param name="Version">Prepared catalog version.</param>
/// <param name="Zip">Local ZIP source.</param>
/// <param name="CurrentVersion">Currently installed version.</param>
/// <param name="Name">Display name.</param>
public sealed record GXModuleUpdatePackage(string Id, string Version, string Zip, string? CurrentVersion = null, string? Name = null);
internal sealed class GXModulePendingPackage
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string? CurrentVersion { get; set; }
    public string Archive { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public GXModulePredecessor? Previous { get; set; }
}
internal sealed class GXModulePredecessor
{
    public string? Version { get; set; }
    public string? ReleaseVersion { get; set; }
    public string? FileName { get; set; }
    public string? Path { get; set; }
    public bool? Enabled { get; set; }
    public ModuleStatus? Status { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Protocols { get; set; }
    public ModuleType? Type { get; set; }
}
internal sealed class GXModulePendingBatch
{
    public Guid Id { get; set; }
    public string State { get; set; } = "PendingRestart";
    public DateTimeOffset PreparedAt { get; set; }
    public string? Error { get; set; }
    public List<GXModulePendingPackage> Packages { get; set; } = [];
    public List<string> RequiredActive { get; set; } = [];
    public Dictionary<string, string?> PreviousSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public GXModulePendingUpdateStatus ToResponse() => new()
    {
        Id = Id, State = State, PreparedAt = PreparedAt, Error = Error,
        Modules = [.. Packages.Select(item => new GXModulePendingVersion(item.Id, item.Name, item.CurrentVersion, item.Version))]
    };
}

/// <summary>Durable immutable packages and the recovery journal on the persistent module volume.</summary>
internal sealed class GXModulePendingUpdates(IHostEnvironment environment)
{
    private readonly string root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Modules", ".pending-updates"));
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    internal static void ValidateSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || value is "." or ".." ||
            !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant))
        {
            throw new InvalidDataException("Invalid module package identity.");
        }
    }
    private string Confined(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidDataException("Module update path is outside the pending directory.");
        }
        for (var current = new DirectoryInfo(Path.GetDirectoryName(path)!); current != null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Module update directories cannot contain symbolic links.");
            }
            if (current.FullName == root)
            {
                break;
            }
        }
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Module update files cannot be symbolic links.");
        }
        return path;
    }
    public string ArchivePath(GXModulePendingBatch batch, GXModulePendingPackage package)
        => Confined(batch.Id.ToString("N") + "/" + package.Archive);
    public string PackageDirectory(GXModulePendingBatch batch, GXModulePendingPackage package)
    {
        ValidateSegment(package.Id);
        return Confined(batch.Id.ToString("N") + "/expanded/" + package.Id);
    }
    public async Task<T> ExclusiveAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(root);
            using var ownership = new FileStream(Confined("journal.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return await action(token);
        }
        finally
        {
            gate.Release();
        }
    }
    public async Task<GXModulePendingBatch?> ReadAsync(CancellationToken token)
    {
        var path = Confined("journal.json");
        if (!File.Exists(path))
        {
            return null;
        }
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, true);
        return await JsonSerializer.DeserializeAsync<GXModulePendingBatch>(stream, Json, token)
            ?? throw new InvalidDataException("Invalid pending module journal.");
    }
    internal async Task SaveAsync(GXModulePendingBatch batch, CancellationToken token)
    {
        var target = Confined("journal.json");
        var temporary = Confined("journal-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            var text = JsonSerializer.Serialize(batch, Json).Replace("\r\n", "\n").Replace("\n", "\r\n");
            await File.WriteAllTextAsync(temporary, text, new System.Text.UTF8Encoding(false), token);
            using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                stream.Flush(true);
            }
            File.Move(temporary, target, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
    public Task<GXModulePendingUpdateStatus> QueueAsync(IReadOnlyList<GXModuleUpdatePackage> packages, CancellationToken token, Func<GXModulePendingBatch, CancellationToken, Task>? validate = null)
        => ExclusiveAsync(async ct =>
        {
            var current = await ReadAsync(ct);
            if (current?.State is "PendingRestart" or "Applying")
            {
                throw new InvalidOperationException("A module update is already pending. Restart AMI or cancel that update first.");
            }
            if (packages.Count == 0 || packages.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != packages.Count)
            {
                throw new InvalidDataException("The module update batch is empty or contains duplicate identities.");
            }
            var batch = new GXModulePendingBatch { Id = Guid.NewGuid(), PreparedAt = DateTimeOffset.UtcNow };
            foreach (var item in packages)
            {
                ValidateSegment(item.Id);
                ValidateSegment(item.Version);
                var package = new GXModulePendingPackage
                {
                    Id = item.Id, Name = item.Name ?? item.Id, Version = item.Version, CurrentVersion = item.CurrentVersion,
                    Archive = Guid.NewGuid().ToString("N") + ".zip"
                };
                var destination = ArchivePath(batch, package);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using (var input = new FileStream(item.Zip, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                {
                    if (input.Length > 100 * 1024 * 1024)
                    {
                        throw new InvalidDataException("Module update ZIP exceeds 100 MiB.");
                    }
                    await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                    await input.CopyToAsync(output, ct);
                    await output.FlushAsync(ct);
                    output.Flush(true);
                }
                await using var bytes = File.OpenRead(destination);
                package.Sha256 = Convert.ToHexString(await SHA256.HashDataAsync(bytes, ct));
                batch.Packages.Add(package);
            }
            if (validate != null)
            {
                await validate(batch, ct);
            }
            await SaveAsync(batch, ct);
            return batch.ToResponse();
        }, token);
    public async Task VerifyAsync(GXModulePendingBatch batch, CancellationToken token)
    {
        if (batch.Id == Guid.Empty || batch.Packages.Count == 0)
        {
            throw new InvalidDataException("The pending module batch is invalid.");
        }
        foreach (var package in batch.Packages)
        {
            ValidateSegment(package.Id);
            ValidateSegment(package.Version);
            ValidateSegment(package.Archive);
            await using var stream = File.OpenRead(ArchivePath(batch, package));
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
            if (!string.Equals(hash, package.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("A pending module ZIP changed after preparation.");
            }
        }
    }
    public Task CancelAsync(CancellationToken token) => ExclusiveAsync(async ct =>
    {
        var batch = await ReadAsync(ct);
        if (batch?.State == "Applying")
        {
            throw new InvalidOperationException("A module update is being applied.");
        }
        if (batch?.State == "PendingRestart")
        {
            batch.State = "Cancelled";
            await SaveAsync(batch, ct);
        }
        return true;
    }, token);
}
