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

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Durable immutable packages and the recovery journal on the persistent module volume.</summary>
internal sealed class GXModulePendingUpdates(IHostEnvironment environment)
{
    /// <summary>Root directory containing persistent files managed by this component.</summary>
    private readonly string _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Modules", ".pending-updates"));
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Serializer settings shared by requests or persisted state in this component.</summary>
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Rejects package identity segments containing unsupported characters, traversal markers, or excessive length.</summary>
    /// <param name="value">Module identity, version, or archive filename segment to validate.</param>
    /// <exception cref="InvalidDataException">The segment is blank, exceeds the limit, or contains unsupported characters or traversal markers.</exception>
    internal static void ValidateSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || value is "." or ".." ||
            !Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant))
        {
            throw new InvalidDataException("Invalid module package identity.");
        }
    }
    /// <summary>Resolves a path below the pending update root and rejects traversal or symbolic links along that path.</summary>
    /// <param name="relative">Path relative to the pending update root that must not traverse symbolic links or escape storage.</param>
    /// <returns>The confined absolute path.</returns>
    /// <exception cref="InvalidDataException">The resolved path escapes pending storage or traverses a symbolic link.</exception>
    private string Confined(string relative)
    {
        var path = Path.GetFullPath(Path.Combine(_root, relative));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidDataException("Module update path is outside the pending directory.");
        }
        for (var current = new DirectoryInfo(Path.GetDirectoryName(path)!); current != null; current = current.Parent)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Module update directories cannot contain symbolic links.");
            }
            if (current.FullName == _root)
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
    /// <summary>Resolves the retained ZIP archive path within the selected pending batch directory.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="package">Package entry whose retained archive or expanded directory is selected.</param>
    /// <returns>The confined absolute archive path.</returns>
    public string ArchivePath(GXModulePendingBatch batch, GXModulePendingPackage package)
        => Confined(batch.Id.ToString("N") + "/" + package.Archive);
    /// <summary>Validates the module identity and resolves its expanded package directory within the pending batch.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="package">Package entry whose retained archive or expanded directory is selected.</param>
    /// <returns>The confined absolute directory containing the expanded module.</returns>
    public string PackageDirectory(GXModulePendingBatch batch, GXModulePendingPackage package)
    {
        ValidateSegment(package.Id);
        return Confined(batch.Id.ToString("N") + "/expanded/" + package.Id);
    }
    /// <summary>Runs one journal operation while holding both an in-process gate and an exclusive filesystem lock.</summary>
    /// <typeparam name="T">Result produced by the supplied callback.</typeparam>
    /// <param name="action">Journal operation executed while both ownership locks are held.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The result produced by the exclusive journal operation.</returns>
    public async Task<T> ExclusiveAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(_root);
            using var ownership = new FileStream(Confined("journal.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return await action(token);
        }
        finally
        {
            _gate.Release();
        }
    }
    /// <summary>Reads the pending update recovery journal if one exists.</summary>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The saved update batch, or null when no journal exists.</returns>
    /// <exception cref="InvalidDataException">The retained journal does not deserialize to an update batch.</exception>
    public async Task<GXModulePendingBatch?> ReadAsync(CancellationToken token)
    {
        var path = Confined("journal.json");
        if (!File.Exists(path))
        {
            return null;
        }
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, true);
        return await JsonSerializer.DeserializeAsync<GXModulePendingBatch>(stream, _json, token)
            ?? throw new InvalidDataException("Invalid pending module journal.");
    }
    /// <summary>Durably writes the update journal to a temporary file and atomically replaces the previous journal.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the replacement journal has been flushed and installed.</returns>
    internal async Task SaveAsync(GXModulePendingBatch batch, CancellationToken token)
    {
        var target = Confined("journal.json");
        var temporary = Confined("journal-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            var text = JsonSerializer.Serialize(batch, _json).Replace("\r\n", "\n").Replace("\n", "\r\n");
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
    /// <summary>Copies and hashes replacement ZIPs, validates the prepared batch, and persists it for the next restart.</summary>
    /// <param name="packages">Inspected replacement archives to retain as one pending update batch.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="validate">Optional batch validation performed after archive copies and hashes are complete, before journal publication.</param>
    /// <returns>Public status for the newly prepared pending update batch.</returns>
    /// <exception cref="InvalidOperationException">Another batch is pending or being applied.</exception>
    /// <exception cref="InvalidDataException">The package list, identity segments, or archive size are invalid.</exception>
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
    /// <summary>Checks pending batch identities and verifies that every retained archive still matches its prepared SHA-256 digest.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes when all pending packages pass verification.</returns>
    /// <exception cref="InvalidDataException">The batch or a package identity is invalid, or an archive digest no longer matches.</exception>
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
    /// <summary>Marks a queued update as cancelled while preventing cancellation during active application.</summary>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes when pending update cancellation has been persisted.</returns>
    /// <exception cref="InvalidOperationException">The pending batch is already being applied.</exception>
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
