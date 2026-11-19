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

using System.Diagnostics;
using System.IO.Compression;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Coordinates module discovery, installation, persistent configuration, and pending update recovery.</summary>
internal sealed partial class GXModuleService
{
    /// <summary>Persists pending packages and the module update recovery journal.</summary>
    private readonly GXModulePendingUpdates _pendingUpdates;
    /// <summary>Pending update batch currently being applied or recovered.</summary>
    private GXModulePendingBatch? _applyingUpdates;
    /// <summary>Inspected replacement module metadata retained before pending update application.</summary>
    /// <param name="Directory">Prepared module directory retained during update application.</param>
    /// <param name="FileName">Filename of the inspected module assembly.</param>
    /// <param name="BinaryVersion">Version read from the inspected assembly.</param>
    /// <param name="Name">Human-readable identity retained in persisted module metadata.</param>
    /// <param name="Description">Human-readable module description from inspection.</param>
    /// <param name="Icon">Optional icon reference supplied by the module.</param>
    /// <param name="Protocols">Optional protocol identifiers advertised by the module.</param>
    /// <param name="Type">Classification declared by the inspected module.</param>
    /// <param name="Candidate">Validated startup candidate for the prepared module.</param>
    private sealed record PreparedModule(string Directory, string FileName, string BinaryVersion,
        string Name, string Description, string? Icon, string? Protocols, Shared.Enums.ModuleType Type,
        GXModuleStartupCandidate Candidate);

    /// <inheritdoc />
    public async Task QueueModuleUpdatesAsync(IReadOnlyList<GXModuleUpdatePackage> packages, CancellationToken cancellationToken = default)
    {
        await _pendingUpdates.QueueAsync(packages, cancellationToken, async (batch, token) =>
        {
            _ = await ValidatePreparedGraphAsync(batch, token);
        });
    }

    /// <summary>Verifies retained archives and validates the prepared replacement dependency graph against persisted module choices.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>Inspected replacement metadata indexed by module identity.</returns>
    private async Task<Dictionary<string, PreparedModule>> ValidatePreparedGraphAsync(GXModulePendingBatch batch, CancellationToken token)
    {
        await _pendingUpdates.VerifyAsync(batch, token);
        var prepared = new Dictionary<string, PreparedModule>(StringComparer.OrdinalIgnoreCase);
        var previousServices = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        await using var connection = await _dbFactory.GetConnectionAsync(token);
        var stored = await connection.SelectAsync<GXModule>(GXSelectArgs.SelectAll<GXModule>(), token);
        foreach (var package in batch.Packages)
        {
            var current = stored.SingleOrDefault(item => item.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("A queued module no longer exists. Cancel and prepare the update again.");
            if (_developmentModules.ContainsKey(package.Id) || current.Path?.Contains(".development", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new InvalidOperationException("Development modules must be rebuilt through their configured source.");
            }
            if (GXModuleInstallPlanner.IsExplicitlyDisabled(current))
            {
                throw new InvalidOperationException("Enable the module before preparing an update; disabled modules are never enabled implicitly.");
            }
            if (GXModuleInstallPlanner.IsInstalled(current) && package.CurrentVersion != null &&
                GXReleaseVersionComparer.Instance.Compare(current.ReleaseVersion ?? current.Version, package.CurrentVersion) != 0)
            {
                throw new InvalidOperationException("The installed module changed after preparation. Cancel and prepare the update again.");
            }
            if (GXModuleInstallPlanner.IsInstalled(current) &&
                GXReleaseVersionComparer.Instance.Compare(current.ReleaseVersion ?? current.Version, package.Version) == 0)
            {
                throw new InvalidOperationException("The selected module version is already installed.");
            }
            if (GXModuleInstallPlanner.IsInstalled(current))
            {
                var (previousInstance, previousContext) = Load(ResolvePath(current));
                try
                {
                    previousServices[package.Id] = _runtime.DescribeStartupCandidate(previousInstance, current.GetSettings()).Services;
                }
                finally
                {
                    previousContext.Unload();
                }
            }
            var archive = _pendingUpdates.ArchivePath(batch, package);
            var candidate = InspectPackage(archive, package.Id, token);
            if (GXReleaseVersionComparer.Instance.Compare(candidate.CompatibilityVersion, package.Version) != 0)
            {
                throw new InvalidDataException("The package version differs from the selected catalog version.");
            }
            GXModuleDependencyValidator.ValidateHostDependencies(candidate.Dependencies);
            var directory = _pendingUpdates.PackageDirectory(batch, package) + "-" + Guid.NewGuid().ToString("N");
            using (var zip = ZipFile.OpenRead(archive))
            {
                long expanded = 0;
                foreach (var entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    expanded = checked(expanded + entry.Length);
                    if (expanded > 512L * 1024 * 1024 || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                    {
                        throw new InvalidDataException("The module ZIP is too large or contains symbolic links.");
                    }
                }
                zip.ExtractToDirectory(directory);
            }
            PreparedModule? description = null;
            foreach (var file in Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    var (instance, context) = Load(file);
                    try
                    {
                        if (instance.Id != package.Id || description != null)
                        {
                            throw new InvalidDataException("Invalid module entry point in the prepared package.");
                        }
                        description = new(directory, Path.GetRelativePath(directory, file),
                            FileVersionInfo.GetVersionInfo(file).FileVersion ?? candidate.CompatibilityVersion,
                            instance.Name, instance.Description, instance.Icon, instance.Protocols, _runtime.GetModuleType(instance),
                            _runtime.DescribeStartupCandidate(instance, current.GetSettings()));
                    }
                    finally
                    {
                        context.Unload();
                    }
                }
                catch (ModuleAssemblyException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }
            prepared.Add(package.Id, description ?? throw new InvalidDataException("The prepared module has no entry point."));
        }
        var nodes = prepared.Values.ToDictionary(item => item.Candidate.Id, item => item.Candidate, StringComparer.OrdinalIgnoreCase);
        foreach (var current in stored.Where(item => item.Enabled == true && GXModuleInstallPlanner.IsInstalled(item) && !nodes.ContainsKey(item.Id)))
        {
            try
            {
                var (instance, context) = Load(ResolvePath(current));
                try
                {
                    nodes[current.Id] = _runtime.DescribeStartupCandidate(instance, current.GetSettings());
                }
                finally
                {
                    context.Unload();
                }
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                _logger.LogWarning(error, "Existing module {Id} could not be inspected while preparing an update", current.Id);
                // An unrelated pre-existing failed deployment must not prevent updates to healthy modules.
            }
        }
        var affected = prepared.Keys.Concat(batch.RequiredActive).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool changed;
        do
        {
            changed = false;
            foreach (var node in nodes.Values)
            {
                var affectedServices = nodes.Values.Where(candidate => affected.Contains(candidate.Id))
                    .SelectMany(candidate => candidate.Services.Concat(previousServices.GetValueOrDefault(candidate.Id) ?? []))
                    .ToHashSet(StringComparer.Ordinal);
                if (node.Dependencies.Any(dependency =>
                    (dependency.Kind == Enums.AmiModuleDependencyKind.Module && affected.Contains(dependency.Id)) ||
                    (dependency.Kind == Enums.AmiModuleDependencyKind.Service && affectedServices.Contains(dependency.Id))))
                {
                    changed |= affected.Add(node.Id);
                }
            }
        }
        while (changed);
        _ = GXModuleStartupPlanner.Plan([.. nodes.Values], [], affected);
        batch.RequiredActive = [.. affected];
        return prepared;
    }

    /// <inheritdoc />
    public async Task StartConfiguredModulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StartWithPendingUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // Do not expose a partially recovered graph as a successfully started host.
            throw new GXModuleUpdateRecoveryException("Module startup or update recovery could not complete safely.", error);
        }
    }

    /// <summary>Recovers interrupted module updates or applies a pending batch before starting configured module generations.</summary>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after pending update recovery and module startup have been processed.</returns>
    private async Task StartWithPendingUpdatesAsync(CancellationToken cancellationToken)
    {
        await _pendingUpdates.ExclusiveAsync(async token =>
        {
            var batch = await _pendingUpdates.ReadAsync(token);
            if (batch?.State == "Applying")
            {
                await RestorePredecessorsAsync(batch, token);
                batch.State = "Failed";
                batch.Error = "An interrupted module update was rolled back before startup. Previous versions were retained.";
                await _pendingUpdates.SaveAsync(batch, token);
            }
            if (batch?.State != "PendingRestart")
            {
                await StartConfiguredModulesCoreAsync(token);
                return true;
            }
            try
            {
                var prepared = await ValidatePreparedGraphAsync(batch, token);
                await using var connection = await _dbFactory.GetConnectionAsync(token);
                var updates = new Dictionary<string, GXModule>(StringComparer.OrdinalIgnoreCase);
                foreach (var package in batch.Packages)
                {
                    var previous = await AmiModuleSettingsStorage.ReadModuleAsync(connection, package.Id, token)
                        ?? throw new InvalidOperationException("A pending module no longer exists.");
                    package.Previous = Snapshot(previous);
                    updates.Add(package.Id, previous);
                }
                var configured = await connection.SelectAsync<GXModule>(GXSelectArgs.SelectAll<GXModule>(), token);
                foreach (var module in configured.Where(item => item.Enabled == true || updates.ContainsKey(item.Id)))
                {
                    var settings = await AmiModuleSettingsStorage.ReadModuleAsync(connection, module.Id, token);
                    batch.PreviousSettings[module.Id] = settings?.GetSettings();
                }
                batch.State = "Applying";
                await _pendingUpdates.SaveAsync(batch, token);
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var package in batch.Packages)
                    {
                        var candidate = prepared[package.Id];
                        var destination = ManagedPath(package.Id, candidate.BinaryVersion + ".prepared-" + batch.Id.ToString("N"));
                        ValidateManagedDirectory(destination);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        if (Directory.Exists(destination))
                        {
                            Directory.Move(destination, ManagedPath(package.Id, candidate.BinaryVersion + ".retained-" + Guid.NewGuid().ToString("N")));
                        }
                        Directory.Move(candidate.Directory, destination);
                        var current = updates[package.Id];
                        current.Name = candidate.Name;
                        current.Description = candidate.Description;
                        current.Icon = candidate.Icon;
                        current.Protocols = candidate.Protocols;
                        current.Type = candidate.Type;
                        current.Version = candidate.BinaryVersion;
                        current.ReleaseVersion = package.Version;
                        current.Path = destination;
                        current.FileName = candidate.FileName;
                        current.Enabled = true;
                        current.Status = ModuleStatus.Installed | ModuleStatus.Installable;
                        connection.Update(transaction, GXUpdateArgs.Update(current, item => new
                        {
                            item.Name, item.Description, item.Icon, item.Protocols, item.Type, item.Version,
                            item.ReleaseVersion, item.Path, item.FileName, item.Enabled, item.Status
                        }));
                    }
                    transaction.Commit();
                }
                _applyingUpdates = batch;
                await StartConfiguredModulesCoreAsync(token);
                foreach (var id in batch.RequiredActive)
                {
                    if (_runtime.GetManifest(id)?.State != Enums.AmiModuleState.Active)
                    {
                        throw new InvalidOperationException("A module or its dependent failed to start after the update.");
                    }
                }
                batch.State = "Succeeded";
                batch.Error = null;
                await _pendingUpdates.SaveAsync(batch, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Leave Applying durable so the next process restores predecessors before any activation.
                throw;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Prepared module batch {Id} failed; restoring predecessor packages", batch.Id);
                if (batch.State is "Applying" or "Succeeded")
                {
                    var manifests = _runtime.GetManifests(false);
                    foreach (var id in _runtime.GetRemovalOrder(manifests.Select(item => item.Id)))
                    {
                        if (await _runtime.StopAsync(id, cancellationToken: CancellationToken.None) == Enums.AmiModuleState.Stopping)
                        {
                            throw new InvalidOperationException("Module update recovery requires another server restart; the Applying journal is retained.", error);
                        }
                    }
                    await RestorePredecessorsAsync(batch, CancellationToken.None);
                }
                batch.State = "Failed";
                batch.Error = "The module update could not be applied. Previous versions were retained; see the server log for details.";
                await _pendingUpdates.SaveAsync(batch, CancellationToken.None);
                _applyingUpdates = null;
                await StartConfiguredModulesCoreAsync(token, retryFailed: true);
            }
            finally
            {
                _applyingUpdates = null;
            }
            return true;
        }, cancellationToken);
    }

    /// <summary>Copies persisted module metadata needed to restore the predecessor after an unsuccessful update.</summary>
    /// <param name="module">Persisted module record supplying identity, configuration, and assembly metadata.</param>
    /// <returns>The predecessor metadata snapshot.</returns>
    private static GXModulePredecessor Snapshot(GXModule module) => new()
    {
        Name = module.Name, Description = module.Description, Icon = module.Icon, Protocols = module.Protocols,
        Version = module.Version, ReleaseVersion = module.ReleaseVersion, Path = module.Path, FileName = module.FileName,
        Enabled = module.Enabled, Status = module.Status, Type = module.Type
    };
    /// <summary>Restores predecessor module metadata and settings in one database transaction.</summary>
    /// <param name="batch">Pending update journal whose packages and recovery metadata are selected.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after all predecessor data has been committed.</returns>
    private async Task RestorePredecessorsAsync(GXModulePendingBatch batch, CancellationToken token)
    {
        await using var connection = await _dbFactory.GetConnectionAsync(token);
        using var transaction = connection.BeginTransaction();
        foreach (var package in batch.Packages)
        {
            if (package.Previous is not { } previous)
            {
                continue;
            }
            var item = new GXModule(package.Id)
            {
                Name = previous.Name, Description = previous.Description, Icon = previous.Icon, Protocols = previous.Protocols,
                Version = previous.Version, ReleaseVersion = previous.ReleaseVersion, Path = previous.Path, FileName = previous.FileName,
                Enabled = previous.Enabled, Status = previous.Status, Type = previous.Type
            };
            connection.Update(transaction, GXUpdateArgs.Update(item, module => new
            {
                module.Name, module.Description, module.Icon, module.Protocols, module.Version,
                module.ReleaseVersion, module.Path, module.FileName, module.Enabled, module.Status, module.Type
            }));
        }
        foreach (var settings in batch.PreviousSettings)
        {
            var module = new GXModule(settings.Key);
            module.SetSettings(settings.Value);
            await AmiModuleSettingsStorage.SavePropertiesAsync(connection, module, token, transaction);
        }
        transaction.Commit();
    }
}
