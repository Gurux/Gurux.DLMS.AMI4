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

using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Module;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.IO.Compression;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Package/persistence facade. Runtime owns all loaded instances and services.</summary>
internal sealed partial class GXModuleService : IGXModuleService, IDisposable
{
    /// <summary>Creates database connections for repository queries and mutations.</summary>
    private readonly IDbConnectionFactory _dbFactory;
    /// <summary>Coordinates loaded module generations and their published services.</summary>
    private readonly GXModuleRuntime _runtime;
    /// <summary>Records operational failures and diagnostic information.</summary>
    private readonly ILogger<GXModuleService> _logger;
    /// <summary>Publishes change notifications after successful mutations.</summary>
    private readonly NotificationCache _notifications;
    /// <summary>Absolute root directory containing installed module packages.</summary>
    private readonly string _modulesRoot;
    /// <summary>Optional resolver for development module assemblies.</summary>
    private readonly GXDevelopmentModuleResolver? _development;
    /// <summary>Indicates whether development module resolution is enabled.</summary>
    private readonly bool _isDevelopment;
    /// <summary>Development assembly paths and versions indexed by module identifier.</summary>
    private readonly ConcurrentDictionary<string, (string Path, string Version)> _developmentModules = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Per-key synchronization that prevents concurrent mutations of the same resource.</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Versions already announced to prevent duplicate update notifications.</summary>
    private readonly ConcurrentDictionary<string, string> _announced = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Coordinates module discovery, installation, persistent configuration, and pending update recovery.</summary>
    /// <param name="dbFactory">Creates database connections for persistence operations.</param>
    /// <param name="runtime">Runtime coordinating module generations and execution leases.</param>
    /// <param name="environment">Host environment providing module storage and development settings.</param>
    /// <param name="logger">Records operational diagnostics and failures.</param>
    /// <param name="notifications">Publishes change notifications after successful persistence.</param>
    /// <param name="development">Optional resolver for development module assemblies.</param>
    /// <param name="pendingUpdates">Optional persistence boundary for pending module updates.</param>
    public GXModuleService(IDbConnectionFactory dbFactory, GXModuleRuntime runtime, IHostEnvironment environment,
        ILogger<GXModuleService> logger, NotificationCache notifications, GXDevelopmentModuleResolver? development = null, GXModulePendingUpdates? pendingUpdates = null)
    {
        this._dbFactory = dbFactory;
        this._runtime = runtime;
        this._logger = logger;
        this._notifications = notifications;
        this._development = development;
        this._pendingUpdates = pendingUpdates ?? new GXModulePendingUpdates(environment);
        _isDevelopment = environment.IsDevelopment();
        _modulesRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Modules"));
        runtime.Changed += OnChanged;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRemovalOrder(IEnumerable<string> ids) => _runtime.GetRemovalOrder(ids);
    /// <inheritdoc />
    public IReadOnlyList<AmiModuleManifest> GetManifests() => _runtime.GetManifests(false);
    /// <inheritdoc />
    public AmiModuleManifest? GetManifest(string id) => _runtime.GetManifest(id);
    /// <inheritdoc />
    public Task<byte[]> ReadAssetAsync(string id, string contentId, string fileName, CancellationToken cancellationToken = default)
        => _runtime.ReadAssetAsync(id, contentId, fileName, cancellationToken);
    /// <inheritdoc />
    public Task ExecuteAsync(GXModule module, string? settings, CancellationToken cancellationToken = default)
        => _runtime.ExecuteAsync(module.Id, settings, cancellationToken);

    /// <summary>Discovers development and persisted modules, resolves startup dependencies, and activates eligible modules while retaining individual failures.</summary>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="retryFailed">Whether previously failed configured candidates may be retried during startup.</param>
    /// <returns>A task that completes after configured startup candidates have been processed.</returns>
    private async Task StartConfiguredModulesCoreAsync(CancellationToken cancellationToken, bool retryFailed = false)
    {
        var dev = _development == null ? [] : await CollectDevelopmentCandidatesAsync(cancellationToken);
        var installed = new Dictionary<string, GXModule>(StringComparer.OrdinalIgnoreCase);
        var definitions = new Dictionary<string, GXModuleStartupCandidate>(StringComparer.OrdinalIgnoreCase);
        var devById = dev.ToDictionary(c => c.Instance.Id, StringComparer.OrdinalIgnoreCase);
        var failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activatedDevelopmentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var candidate in dev)
            {
                try
                {
                    if (await RegisterDevelopmentCandidateAsync(candidate, cancellationToken))
                    {
                        definitions.Add(candidate.Instance.Id, _runtime.DescribeStartupCandidate(candidate.Instance, candidate.Settings));
                    }
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    failed.Add(candidate.Instance.Id);
                    await RecordStartupFailureAsync(candidate.Instance.Id, candidate.Instance.Name, error);
                }
            }
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var storedArgs = GXSelectArgs.SelectAll<GXModule>(m => m.Enabled == true);
            storedArgs.Columns.Add<GXModuleProperty>();
            storedArgs.Columns.Exclude<GXModuleProperty>(p => p.Module!);
            storedArgs.Joins.AddLeftJoin<GXModule, GXModuleProperty>(j => j.Id, j => j.Module!);
            var storedModules = await connection.SelectAsync<GXModule>(storedArgs, cancellationToken);
            foreach (var stored in storedModules)
            {
                if (!retryFailed && _runtime.GetManifest(stored.Id)?.State == AmiModuleState.Failed)
                {
                    continue;
                }
                bool oldDevelopment = !string.IsNullOrEmpty(stored.Path) && Path.GetFullPath(stored.Path).StartsWith(
                    Path.Combine(_modulesRoot, ".development") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                if (devById.ContainsKey(stored.Id))
                {
                    // Development candidates are started below; keep the persisted activation choice intact.
                    continue;
                }
                if (oldDevelopment)
                {
                    _runtime.RecordStartupFailure(stored.Id, stored.Name ?? stored.Id,
                        new InvalidOperationException("The configured development module source is unavailable."));
                    continue;
                }
                if (!_isDevelopment && (stored.Status & ModuleStatus.Installed) == 0)
                {
                    continue;
                }

                PluginLoadContext? context = null;
                try
                {
                    var loaded = Load(ResolvePath(stored));
                    context = loaded.Context;
                    if (!loaded.Module.Id.Equals(stored.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Stored module identity differs from package.");
                    }

                    definitions.Add(stored.Id, _runtime.DescribeStartupCandidate(loaded.Module, stored.GetSettings()));
                    installed.Add(stored.Id, stored);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    failed.Add(stored.Id);
                    await RecordStartupFailureAsync(stored.Id, stored.Name ?? stored.Id, error);
                }
                finally
                {
                    context?.Unload();
                }
            }
            foreach (var root in definitions.Keys.ToArray())
            {
                try
                {
                    var plan = GXModuleStartupPlanner.Plan([.. definitions.Values], _runtime.GetStartupProviders(), [root]);
                    foreach (var item in plan)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (failed.Contains(item.Id))
                        {
                            throw new AmiModuleDependencyException($"Required module '{item.Id}' failed during startup.");
                        }

                        if (_runtime.GetManifest(item.Id)?.State == AmiModuleState.Active)
                        {
                            continue;
                        }

                        try
                        {
                            if (devById.TryGetValue(item.Id, out var local))
                            {
                                await ActivateDevelopmentCandidateAsync(local, cancellationToken);
                                activatedDevelopmentPaths.Add(local.Path);
                            }
                            else
                            {
                                await EnableModuleAsync(null, installed[item.Id], cancellationToken);
                            }
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception error)
                        {
                            failed.Add(item.Id);
                            await RecordStartupFailureAsync(item.Id, item.Id, error);
                            throw;
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    failed.Add(root);
                    await RecordStartupFailureAsync(root, root, error);
                }
            }
        }
        finally
        {
            foreach (var candidate in dev)
            {
                if (!activatedDevelopmentPaths.Contains(candidate.Path))
                {
                    candidate.Context.Unload();
                }
            }
        }
    }

    /// <summary>Logs a startup failure and retains its runtime manifest without aborting host startup.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="name">Stable name used to select or register the definition described by this operation.</param>
    /// <param name="error">Failure that determines module fault or startup status.</param>
    /// <returns>A completed task after the failure has been recorded.</returns>
    private Task RecordStartupFailureAsync(string id, string name, Exception error)
    {
        _logger.LogError(error, "Module {Id} startup failed; deployment choice retained, host startup continues.", id);
        _runtime.RecordStartupFailure(id, name, error);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task EnableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = _gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Save the explicit choice before loading; a startup failure must not undo it.
            await SetEnabledAsync(module.Id, true, cancellationToken);
            _runtime.SetEnabled(module.Id, true);
            if (_runtime.GetManifest(module.Id)?.State == AmiModuleState.Active)
            {
                await _runtime.UpdateSettingsAsync(module.Id, module.GetSettings(), cancellationToken);
                return;
            }
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, module.Id, cancellationToken)
                ?? throw new InvalidOperationException("Module does not exist.");
            module.SetSettings(AmiModuleSettingsStorage.PreserveState(module.Properties == null ? stored.GetSettings() : module.GetSettings(), stored.GetSettings()));
            var selected = _developmentModules.GetValueOrDefault(stored.Id);
            string selectedPath = selected.Path ?? ResolvePath(stored);
            var (instance, loadContext) = Load(selectedPath);
            if (instance.Id != stored.Id)
            {
                loadContext.Unload();
                throw new InvalidOperationException("Module package identity does not match the stored Id.");
            }
            try
            {
                await _runtime.ActivateAsync(instance, selectedPath, selected.Version ?? stored.Version ?? "0", module.GetSettings(),
                    () =>
                    {
                        loadContext.Unload();
                        return ValueTask.CompletedTask;
                    }, cancellationToken,
                    beforeStart: _applyingUpdates?.Packages.FirstOrDefault(item => item.Id == stored.Id)?.Previous is { } predecessor
                        ? (context, ct) => GXModuleInstallPlanner.IsInstalled(new GXModule(stored.Id) { Status = predecessor.Status, FileName = predecessor.FileName })
                            ? instance.UpdateAsync(context, predecessor.Version ?? "0", ct)
                            : instance.InstallAsync(context, ct)
                        : null,
                    beforePublish: ct =>
                    {
                        ct.ThrowIfCancellationRequested();
                        stored.Icon = instance.Icon;
                        connection.Update(GXUpdateArgs.Update(stored, m => m.Icon!));
                        return Task.CompletedTask;
                    });
            }
            catch
            {
                await _runtime.StopAsync(module.Id, cancellationToken: CancellationToken.None);
                throw;
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            _runtime.RecordStartupFailure(module.Id, module.Name ?? module.Id, error);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Copies development outputs into isolated run directories and inspects candidates while rejecting duplicate module identities.</summary>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>Prepared development candidates whose assemblies were loaded from isolated snapshots.</returns>
    private async Task<List<DevelopmentCandidate>> CollectDevelopmentCandidatesAsync(CancellationToken cancellationToken)
    {
        var sources = await _development!.ResolveAsync(cancellationToken, continueOnError: true);
        var candidates = new List<DevelopmentCandidate>();
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var run = Path.Combine(_modulesRoot, ".development", Guid.NewGuid().ToString("N"));
        int sourceIndex = 0;
        foreach (var source in sources)
        {
            var local = new List<DevelopmentCandidate>();
            try
            {
                var snapshot = Path.Combine(run, (sourceIndex++).ToString("D4"));
                if (IsWithin(snapshot, source.Directory))
                {
                    throw new InvalidOperationException("Development output must not contain the module run directory.");
                }

                Directory.CreateDirectory(snapshot);
                foreach (var file in Directory.EnumerateFiles(source.Directory, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    IgnoreInaccessible = false
                }))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var target = Path.GetFullPath(Path.Combine(snapshot, Path.GetRelativePath(source.Directory, file)));
                    if (!IsWithin(target, snapshot))
                    {
                        throw new InvalidOperationException("Development content is outside its run directory.");
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target);
                }
                var files = source.AssemblyFile == null ? Directory.GetFiles(snapshot, "*.dll") : [Path.Combine(snapshot, source.AssemblyFile)];
                foreach (var file in files)
                {
                    try
                    {
                        var loaded = Load(file);
                        local.Add(new(loaded.Module, loaded.Context, file, source.Settings));
                    }
                    catch (BadImageFormatException) when (source.AssemblyFile == null)
                    {
                    }
                    catch (ModuleAssemblyException) when (source.AssemblyFile == null)
                    {
                    }
                }
                if (local.Count != 1)
                {
                    throw new InvalidOperationException($"Development directory '{source.Directory}' must contain one module implementation.");
                }

                var previous = candidates.FirstOrDefault(c => c.Instance.Id.Equals(local[0].Instance.Id, StringComparison.OrdinalIgnoreCase));
                if (previous != null || duplicates.Contains(local[0].Instance.Id))
                {
                    var error = new InvalidOperationException($"Development module '{local[0].Instance.Id}' is configured more than once; module disabled.");
                    duplicates.Add(local[0].Instance.Id);
                    if (previous != null)
                    {
                        candidates.Remove(previous);
                        previous.Context.Unload();
                    }
                    _runtime.RecordStartupFailure(local[0].Instance.Id, local[0].Instance.Name, error);
                    throw error;
                }
                candidates.Add(local[0]);
                local.Clear();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                foreach (var candidate in local.Concat(candidates))
                {
                    candidate.Context.Unload();
                }

                throw;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Development module source {Source} disabled; startup continues.", source.Directory);
            }
            finally
            {
                foreach (var candidate in local)
                {
                    candidate.Context.Unload();
                }
            }
        }
        return candidates;
    }
    /// <summary>Prepared development module instance and load context awaiting registration.</summary>
    /// <param name="Instance">Constructed development module awaiting activation.</param>
    /// <param name="Context">Collectible load context owning the candidate assembly.</param>
    /// <param name="Path">Path of the candidate module assembly.</param>
    /// <param name="Settings">Serialized settings proposed for the candidate.</param>
    private sealed record DevelopmentCandidate(IGXAmiModule Instance, PluginLoadContext Context, string Path, string? Settings);

    /// <summary>Creates or refreshes a development module's persistent registration while preserving the stored deployment choice.</summary>
    /// <param name="candidate">Prepared development module and its collectible assembly context.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>True when the registration permits activation of the development candidate.</returns>
    private async Task<bool> RegisterDevelopmentCandidateAsync(DevelopmentCandidate candidate, CancellationToken cancellationToken)
    {
        await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
        var instance = candidate.Instance;
        ValidateSegment(instance.Id);
        string version = FileVersionInfo.GetVersionInfo(candidate.Path).FileVersion ?? "0";
        var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, instance.Id, cancellationToken);
        bool activate = GXDevelopmentModuleRegistration.ShouldActivate(stored);
        if (stored == null)
        {
            string? creatorId = _development?.Options.CreatorId;
            GXUser? creator;
            if (!string.IsNullOrWhiteSpace(creatorId))
            {
                creator = await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.SelectAll<GXUser>(u => u.Id == creatorId && u.Removed == null), cancellationToken);
            }
            else
            {
                var admin = GXSelectArgs.SelectAll<GXUser>(u => u.Removed == null);
                admin.Joins.AddInnerJoin<GXUser, GXUserRole>(u => u.Id!, r => r.UserId!);
                admin.Joins.AddInnerJoin<GXUserRole, GXRole>(u => u.RoleId!, r => r.Id);
                admin.Where.And<GXRole>(r => r.Name == GXRoles.Admin && r.Removed == null);
                creator = (await connection.SelectAsync<GXUser>(admin, cancellationToken)).FirstOrDefault();
            }
            if (creator == null)
            {
                throw new InvalidOperationException($"Development module '{instance.Id}' needs an existing test user. Set Modules.Development.CreatorId or create a test administrator.");
            }

            stored = new GXModule(instance.Id)
            {
                Name = instance.Name,
                Icon = instance.Icon,
                Description = instance.Description,
                Protocols = instance.Protocols,
                Path = Path.GetDirectoryName(candidate.Path),
                FileName = Path.GetFileName(candidate.Path),
                Version = version,
                Properties = GXModuleSettings.FromSettings(candidate.Settings),
                Creator = creator,
                Enabled = activate,
                CreationTime = DateTimeOffset.UtcNow,
                Status = ModuleStatus.CustomBuild,
                Type = _runtime.GetModuleType(instance)
            };
        }
        // Refresh the selected development build without changing the saved deployment choice or settings.
        stored.Name = instance.Name;
        stored.Icon = instance.Icon;
        stored.Description = instance.Description;
        stored.Protocols = instance.Protocols;
        stored.Path = Path.GetDirectoryName(candidate.Path);
        stored.FileName = Path.GetFileName(candidate.Path);
        stored.Version = version;
        stored.Status = ModuleStatus.CustomBuild;
        stored.Type = _runtime.GetModuleType(instance);
        await GXDevelopmentModuleRegistration.EnsureAsync(connection, stored, version, cancellationToken);
        // Selection is independent of activation, so disabled development modules can later be enabled
        // from the current build and cannot be replaced by an installed ZIP with the same identity.
        _developmentModules[instance.Id] = (candidate.Path, version);
        return activate;
    }

    /// <summary>Activates a registered development candidate and persists its custom-build metadata before publication.</summary>
    /// <param name="candidate">Prepared development module and its collectible assembly context.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes when the development generation has been activated.</returns>
    private async Task ActivateDevelopmentCandidateAsync(DevelopmentCandidate candidate, CancellationToken cancellationToken)
    {
        await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
        var instance = candidate.Instance;
        string version = FileVersionInfo.GetVersionInfo(candidate.Path).FileVersion ?? "0";
        var record = await AmiModuleSettingsStorage.ReadModuleAsync(connection, instance.Id, cancellationToken)
            ?? throw new InvalidOperationException("The development module has not been registered.");
        await _runtime.ActivateAsync(instance, candidate.Path, version, candidate.Settings ?? record.GetSettings(),
            () =>
            {
                candidate.Context.Unload();
                return ValueTask.CompletedTask;
            }, cancellationToken,
            beforePublish: ct =>
            {
                ct.ThrowIfCancellationRequested();
                var activated = new GXModule(instance.Id)
                {
                    ReleaseVersion = null,
                    Version = version,
                    Icon = instance.Icon,
                    Status = (record.Status.GetValueOrDefault() & ModuleStatus.Installable) | ModuleStatus.CustomBuild,
                    Path = Path.GetDirectoryName(candidate.Path),
                    FileName = Path.GetFileName(candidate.Path),
                    Type = _runtime.GetModuleType(instance)
                };
                connection.Update(GXUpdateArgs.Update(activated,
                    m => new { m.ReleaseVersion, m.Version, m.Status, m.Path, m.FileName, m.Type, m.Icon }));
                return Task.CompletedTask;
            });
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
        {
            _logger.LogInformation("Development module {Id} loaded from local build snapshot {Path}. Restart AMI after rebuilding.", instance.Id, candidate.Path);
        }
    }
    /// <inheritdoc />
    public async Task<AmiModuleState> DisableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = _gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await SetEnabledAsync(module.Id, false, cancellationToken);
            _runtime.SetEnabled(module.Id, false);
            return await _runtime.StopAsync(module.Id, cancellationToken: cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateModuleSettingsAsync(GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = _gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_runtime.GetManifest(module.Id)?.State == AmiModuleState.Active)
            {
                await _runtime.UpdateSettingsAsync(module.Id, module.GetSettings(), cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteModuleAsync(GXModule module, CancellationToken cancellationToken = default, AmiModuleUninstallOptions? uninstallOptions = null)
    {
        uninstallOptions ??= new();
        uninstallOptions.Validate();
        if (_runtime.GetManifest(module.Id)?.State == AmiModuleState.Stopping)
        {
            throw new InvalidOperationException("Module removal is already pending.");
        }

        var gate = _gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, module.Id, cancellationToken)
                ?? throw new InvalidOperationException("Module does not exist.");
            await SetEnabledAsync(module.Id, false, cancellationToken);
            bool active = _runtime.GetManifest(module.Id)?.State == AmiModuleState.Active;
            AmiModuleState state = active ? await _runtime.RemoveAsync(module.Id, cancellationToken, uninstallOptions)
                : await _runtime.StopAsync(module.Id, cancellationToken: cancellationToken);
            if (state == AmiModuleState.Stopping)
            {
                throw new InvalidOperationException("Module is still stopping; delete it after active calls finish.");
            }

            if (!active)
            {
                // Catalog entries have no installed binary or uninstall lifecycle to run.
                // The repository removes their metadata after this operation returns.
                if (!GXModuleInstallPlanner.IsInstalled(stored))
                {
                    return;
                }
                var (instance, context) = Load(ResolvePath(stored));
                await _runtime.ActivateAsync(instance, ResolvePath(stored), stored.Version ?? "0", stored.GetSettings(),
                    () =>
                    {
                        context.Unload();
                        return ValueTask.CompletedTask;
                    }, cancellationToken,
                    start: false, uninstallOptions: uninstallOptions);
            }
            // Development directories belong to the developer and are never deleted.
            var preparedPrefix = ManagedPath(stored.Id, (stored.Version ?? "0") + ".prepared-");
            bool preparedPackage = stored.Path != null && Path.GetFullPath(stored.Path).StartsWith(preparedPrefix,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (string.IsNullOrEmpty(stored.Path) || preparedPackage)
            {
                string directory = preparedPackage ? Path.GetFullPath(stored.Path!) : ManagedPath(stored.Id, stored.Version ?? "0");
                if (Directory.Exists(directory))
                {
                    DeleteManagedDirectory(directory);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<GXModule>> AddModuleAsync(ClaimsPrincipal? user, string compressedFile, CancellationToken cancellationToken = default, string? expectedId = null)
    {
        string temporary = Path.Combine(Path.GetTempPath(), "GuruxModules", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            ZipFile.ExtractToDirectory(compressedFile, temporary);
            var candidates = new List<(string File, IGXAmiModule Module, PluginLoadContext Context)>();
            foreach (string file in Directory.GetFiles(temporary, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var loaded = Load(file);
                    if (loaded.Module != null)
                    {
                        candidates.Add((file, loaded.Module, loaded.Context));
                    }
                }
                catch (BadImageFormatException)
                {
                }
                catch (ModuleAssemblyException)
                {
                }
            }
            if (candidates.Count != 1)
            {
                foreach (var candidate in candidates)
                {
                    candidate.Context.Unload();
                }

                throw new InvalidOperationException("A module package must contain exactly one concrete IGXAmiModule implementation.");
            }
            var selected = candidates[0];
            selected.Context.Unload();
            if (expectedId != null && selected.Module.Id != expectedId)
            {
                throw new InvalidOperationException($"The downloaded package does not match the requested module. Expected '{expectedId}', package declares '{selected.Module.Id}'.");
            }

            await using (var connection = await _dbFactory.GetConnectionAsync(cancellationToken))
            {
                var current = await AmiModuleSettingsStorage.ReadModuleAsync(connection, selected.Module.Id, cancellationToken);
                if (user != null && current != null && GXModuleInstallPlanner.IsInstalled(current))
                {
                    await QueueModuleUpdatesAsync([new(selected.Module.Id,
                        GXModuleDependencyValidator.ReadCompatibilityVersion(selected.Module.GetType().Assembly), compressedFile,
                        current.ReleaseVersion ?? current.Version, current.Name)], cancellationToken);
                    return [current];
                }
            }
            return [await InstallCandidateAsync(user, selected.File, temporary, true, cancellationToken)];
        }
        finally
        {
            try
            {
                if (Directory.Exists(temporary))
                {
                    Directory.Delete(temporary, recursive: true);
                }
            }
            catch (IOException exception)
            {
                _logger.LogWarning("Temporary module package cleanup pending: {Message}", exception.Message);
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<GXModule>> RefreshAsync(ClaimsPrincipal? user, string developmentPath, CancellationToken cancellationToken = default)
    {
        var added = new List<GXModule>();
        foreach (string file in Directory.GetFiles(Path.GetFullPath(developmentPath), "*.dll", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await InstallCandidateAsync(user, file, Path.GetDirectoryName(file)!, false, cancellationToken) is { } item)
                {
                    added.Add(item);
                }
            }
            catch (BadImageFormatException)
            {
            }
            catch (ModuleAssemblyException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogWarning("Development module {File} was not activated: {Message}", file, exception.Message);
            }
        }
        return added;
    }

    /// <summary>Inspects a module assembly, reserves its dependencies, and installs its persisted generation under a per-module gate.</summary>
    /// <param name="user">Principal requesting installation, or null for host-owned installation.</param>
    /// <param name="file">Path to the candidate module assembly being inspected and installed.</param>
    /// <param name="sourceRoot">Root directory containing the candidate's package files.</param>
    /// <param name="managed">Whether installation copies and owns files under managed module storage.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The persisted module record produced by installation.</returns>
    private async Task<GXModule> InstallCandidateAsync(ClaimsPrincipal? user, string file, string sourceRoot, bool managed, CancellationToken cancellationToken)
    {
        var (descriptor, inspection) = Load(file);
        string id, version, name, description;
        string? protocols, icon;
        ModuleType type;
        IReadOnlyList<AmiModuleDependency> dependencies;
        try
        {
            ValidateSegment(descriptor.Id);
            id = descriptor.Id;
            GXModuleDependencyValidator.ReadCompatibilityVersion(descriptor.GetType().Assembly);
            version = FileVersionInfo.GetVersionInfo(file).FileVersion ?? descriptor.GetType().Assembly.GetName().Version?.ToString() ?? "0";
            ValidateSegment(version);
            name = descriptor.Name;
            description = descriptor.Description;
            protocols = descriptor.Protocols;
            icon = descriptor.Icon;
            type = _runtime.GetModuleType(descriptor);
            dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, descriptor.Dependencies);
            _runtime.ValidateCandidateDependencies(id, dependencies);
        }
        finally
        {
            inspection.Unload();
        }
        var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var dependencyReservation = _runtime.ReserveCandidateDependencies(id, dependencies);
            if (_developmentModules.ContainsKey(id))
            {
                throw new InvalidOperationException($"Module '{id}' is selected by development settings. Remove that development entry and restart AMI before installing its ZIP package.");
            }

            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var old = await AmiModuleSettingsStorage.ReadModuleAsync(connection, id, cancellationToken);
            if (old == null && user == null)
            {
                throw new InvalidOperationException("Installing a new module requires an authenticated installer.");
            }

            string destination = managed ? ManagedPath(id, version) : Path.GetDirectoryName(file)!;
            string relativeFile = managed ? Path.GetRelativePath(sourceRoot, file) : Path.GetFileName(file);
            string main = Path.GetFullPath(Path.Combine(destination, relativeFile));
            if (!managed && _runtime.GetManifest(id) is { State: AmiModuleState.Active } active && old?.Version == version && ResolvePath(old) == main
                && active.ContentId == await GXModuleRuntime.ComputeContentIdAsync(file, cancellationToken))
            {
                return old;
            }

            string? previousVersion = old?.Version;
            var previous = old == null ? null : new GXModule(id)
            {
                Name = old.Name,
                Icon = old.Icon,
                Description = old.Description,
                Protocols = old.Protocols,
                Version = old.Version,
                ReleaseVersion = old.ReleaseVersion,
                FileName = old.FileName,
                Path = old.Path,
                Type = old.Type,
                Status = old.Status,
                Enabled = old.Enabled
            };
            bool restoreActiveOnFailure = _runtime.GetManifest(id)?.State == AmiModuleState.Active;
            if (await _runtime.StopAsync(id, cancellationToken: cancellationToken) == AmiModuleState.Stopping)
            {
                throw new InvalidOperationException("The previous module version is still stopping.");
            }
            string? backup = null;
            try
            {
                if (managed)
                {
                    string staging = ManagedPath(id, version + ".staging-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(staging);
                    try
                    {
                        foreach (string source in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
                        {
                            string target = Path.GetFullPath(Path.Combine(staging, Path.GetRelativePath(sourceRoot, source)));
                            if (!IsWithin(target, staging))
                            {
                                throw new InvalidOperationException("Invalid module package path.");
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                            File.Copy(source, target, overwrite: true);
                        }
                        ValidateManagedDirectory(destination);
                        if (Directory.Exists(destination))
                        {
                            string previousPackage = ManagedPath(id, version + ".backup-" + Guid.NewGuid().ToString("N"));
                            Directory.Move(destination, previousPackage);
                            backup = previousPackage;
                        }
                        Directory.Move(staging, destination);
                    }
                    finally
                    {
                        if (Directory.Exists(staging))
                        {
                            DeleteManagedDirectory(staging);
                        }
                    }
                }
                bool wasInstalled = old != null && GXModuleInstallPlanner.IsInstalled(old);
                bool enabledAfterInstall = !wasInstalled || old!.Enabled != false;
                var item = old ?? new GXModule(id)
                {
                    Creator = new GXUser { Id = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException() },
                    CreationTime = DateTimeOffset.UtcNow
                };
                item.Name = name;
                item.Icon = icon;
                item.Description = description;
                item.Protocols = protocols;
                item.ReleaseVersion = null;
                item.Version = version;
                item.FileName = relativeFile;
                item.Path = managed ? null : destination;
                item.Type = type;
                item.Enabled = enabledAfterInstall;
                bool hasCatalogVersions = managed && await connection.SingleOrDefaultAsync<int>(
                    GXSelectArgs.Select<GXModuleVersion>(v => GXSql.Count(1), v => v.Module == item), cancellationToken) > 0;
                item.Status = GXModuleInstallationPolicy.InstalledStatus(hasCatalogVersions);
                var (instance, context) = Load(main);
                await _runtime.ActivateAsync(instance, main, version, old?.GetSettings(),
                    () =>
                    {
                        context.Unload();
                        return ValueTask.CompletedTask;
                    }, cancellationToken,
                    (scope, ct) => !wasInstalled ? instance.InstallAsync(scope, ct) : instance.UpdateAsync(scope, previousVersion ?? "0", ct),
                    beforePublish: ct =>
                    {
                        ct.ThrowIfCancellationRequested();
                        using var transaction = connection.BeginTransaction();
                        if (old == null)
                        {
                            var insert = GXInsertArgs.Insert(item);
                            insert.Exclude<GXModule>(m => new { m.UserGroups, m.ModuleGroups, m.Logs, m.Versions, m.DeviceProperties, m.ObjectProperties, m.AttributeProperties });
                            connection.Insert(transaction, insert);
                            foreach (var group in connection.Select<GXModuleGroup>(transaction, GXSelectArgs.SelectAll<GXModuleGroup>(g => g.Default == true && g.Removed == null), ct))
                            {
                                connection.Insert(transaction, GXInsertArgs.Insert(new GXModuleGroupModule { ModuleId = id, ModuleGroupId = group.Id, CreationTime = DateTimeOffset.UtcNow }));
                            }
                        }
                        else
                        {
                            connection.Update(transaction, GXUpdateArgs.Update(item, m => new { m.Name, m.Icon, m.Description, m.Protocols, m.Version, m.ReleaseVersion, m.FileName, m.Path, m.Type, m.Enabled, m.Status }));
                        }

                        transaction.Commit();
                        return Task.CompletedTask;
                    }, start: item.Enabled == true);
                if (backup != null)
                {
                    try
                    {
                        DeleteManagedDirectory(backup);
                    }
                    catch (IOException exception)
                    {
                        _logger.LogWarning("Previous module package cleanup pending: {Message}", exception.Message);
                    }
                }
                return item;
            }
            catch
            {
                await _runtime.StopAsync(id, cancellationToken: CancellationToken.None);
                if (backup != null)
                {
                    DeleteManagedDirectory(destination);
                    Directory.Move(backup, destination);
                }
                if (previous != null)
                {
                    connection.Update(GXUpdateArgs.Update(previous, m => new { m.Name, m.Icon, m.Description, m.Protocols, m.Version, m.ReleaseVersion, m.FileName, m.Path, m.Type, m.Status, m.Enabled }));
                    if (restoreActiveOnFailure)
                    {
                        // Restoring the persisted release alone leaves a previously running module offline.
                        // Restart its original binary without rerunning Install/Update or taking this gate again.
                        var restored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, id, CancellationToken.None)
                            ?? throw new InvalidOperationException("The previous module record could not be restored.");
                        string restoredPath = ResolvePath(restored);
                        var (previousInstance, previousContext) = Load(restoredPath);
                        await _runtime.ActivateAsync(previousInstance, restoredPath, restored.Version ?? "0", restored.GetSettings(),
                            () =>
                            {
                                previousContext.Unload();
                                return ValueTask.CompletedTask;
                            }, CancellationToken.None);
                    }
                }

                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Indicates that a loaded assembly contains no supported AMI module implementation.</summary>
    private sealed class ModuleAssemblyException() : Exception("Assembly contains no module implementation.");
    /// <summary>Loads an assembly in a collectible context and constructs its supported module implementation.</summary>
    /// <param name="path">Filesystem path of the file or assembly to inspect.</param>
    /// <returns>The constructed module and the load context that owns its assemblies.</returns>
    private static (IGXAmiModule Module, PluginLoadContext Context) Load(string path)
    {
        var context = new PluginLoadContext(path);
        try
        {
            var assembly = context.LoadMainAssembly();
            var assemblyName = assembly.GetName().Name!;
            bool referencesModuleContract = assembly.GetReferencedAssemblies()
                .Any(r => r.Name == typeof(IGXAmiModule).Assembly.GetName().Name);
            // Framework/browser dependencies are not module entry points. Enumerating their
            // types on the server can raise handled loader errors for platform-only types.
            if (!referencesModuleContract && (PluginLoadContext.IsSharedAssembly(assemblyName) ||
                assemblyName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                assemblyName.StartsWith("System.", StringComparison.Ordinal) ||
                assemblyName is "netstandard" or "mscorlib"))
            {
                throw new ModuleAssemblyException();
            }
            Type[] available;
            try
            {
                available = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception) when (!assembly.GetReferencedAssemblies().Any(r => r.Name == typeof(IGXAmiModule).Assembly.GetName().Name))
            {
                // Packages may contain compatibility libraries whose unused types cannot load against this host.
                // Recover valid module types, but do not treat an unrelated dependency DLL as an installation candidate.
                available = [.. exception.Types.OfType<Type>()];
            }
            var types = available.Where(t => !t.IsAbstract && typeof(IGXAmiModule).IsAssignableFrom(t)).ToArray();
            if (types.Length == 0)
            {
                throw new ModuleAssemblyException();
            }

            if (types.Length != 1)
            {
                throw new InvalidOperationException("Module assembly contains multiple implementations.");
            }

            context.ValidateDependencies(assembly);
            return ((IGXAmiModule)Activator.CreateInstance(types[0])!, context);
        }
        catch
        {
            context.Unload();
            throw;
        }
    }

    /// <summary>Resolves the persisted module assembly path and rejects filenames that escape the selected package directory.</summary>
    /// <param name="module">Persisted module record supplying identity, configuration, and assembly metadata.</param>
    /// <returns>The confined absolute path to the module assembly.</returns>
    /// <exception cref="InvalidOperationException">The assembly filename is absent or escapes the package directory.</exception>
    private string ResolvePath(GXModule module)
    {
        string directory = string.IsNullOrWhiteSpace(module.Path) ? ManagedPath(module.Id, module.Version ?? "0") : Path.GetFullPath(module.Path);
        string path = Path.GetFullPath(Path.Combine(directory, module.FileName ?? throw new InvalidOperationException("Module file name is missing.")));
        if (!IsWithin(path, directory))
        {
            throw new InvalidOperationException("Module file is outside its package directory.");
        }

        return path;
    }
    /// <summary>Validates module identity and version segments and resolves their directory under managed module storage.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="version">Version identifying the managed package directory.</param>
    /// <returns>The confined absolute managed package directory.</returns>
    /// <exception cref="InvalidOperationException">An identity segment is invalid or the directory escapes managed storage.</exception>
    private string ManagedPath(string id, string version)
    {
        ValidateSegment(id);
        ValidateSegment(version);
        string path = Path.GetFullPath(Path.Combine(_modulesRoot, id, version));
        if (!IsWithin(path, _modulesRoot))
        {
            throw new InvalidOperationException("Module directory is outside the managed root.");
        }

        return path;
    }
    /// <summary>Rejects managed module directories outside the configured root or containing symbolic-link components.</summary>
    /// <param name="directory">Managed module directory whose confinement and symbolic-link safety must be verified.</param>
    /// <exception cref="InvalidOperationException">The directory is outside managed storage or contains a linked path component.</exception>
    private void ValidateManagedDirectory(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (!IsWithin(directory, _modulesRoot))
        {
            throw new InvalidOperationException("Module directory is outside the managed root.");
        }

        for (var item = new DirectoryInfo(directory); item != null && IsWithin(item.FullName, _modulesRoot); item = item.Parent)
        {
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Refusing to modify a linked module directory.");
            }
        }

        if (Directory.Exists(_modulesRoot) && (File.GetAttributes(_modulesRoot) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Managed module root cannot be a linked directory.");
        }
    }
    /// <summary>Validates ownership of a managed module directory and recursively deletes it when present.</summary>
    /// <param name="directory">Managed module directory whose confinement and symbolic-link safety must be verified.</param>
    private void DeleteManagedDirectory(string directory)
    {
        ValidateManagedDirectory(directory);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
    /// <summary>Checks whether a resolved path is located below the required directory.</summary>
    /// <param name="path">Filesystem path to resolve or inspect.</param>
    /// <param name="directory">Directory that must contain the resolved path.</param>
    /// <returns>True when the path is below the required directory.</returns>
    private static bool IsWithin(string path, string directory) => path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    /// <summary>Rejects module identity or version path segments containing traversal or unsupported characters.</summary>
    /// <param name="value">Module identity or version used as a managed directory segment.</param>
    /// <exception cref="InvalidOperationException">The path segment contains unsupported characters or traversal.</exception>
    private static void ValidateSegment(string value)
    {
        if (!MyRegex().IsMatch(value) || value.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid module identity/version path segment.");
        }
    }
    /// <summary>Persists a module's enabled flag together with its modification timestamp.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="enabled">Enabled flag to persist without changing other module metadata.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the module flag has been updated.</returns>
    private async Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
        var value = new GXModule(id) { Enabled = enabled, Updated = DateTimeOffset.UtcNow };
        await connection.UpdateAsync(GXUpdateArgs.Update(value, m => new { m.Enabled, m.Updated }), cancellationToken);
    }
    /// <summary>Publishes notifications only when a module's content identity or lifecycle state differs from its last announcement.</summary>
    private void OnChanged()
    {
        foreach (var manifest in _runtime.GetManifests(false))
        {
            string state = manifest.ContentId + ":" + manifest.State;
            if (_announced.GetValueOrDefault(manifest.Id) == state)
            {
                continue;
            }

            _announced[manifest.Id] = state;
            _ = NotifyAsync(manifest);
        }
    }
    /// <summary>Notifies users authorized for the module's views, pages, or general module access while logging delivery failures.</summary>
    /// <param name="manifest">Module manifest whose visible destinations determine notification recipients.</param>
    /// <returns>A task that completes after notification recipients have been resolved and queued.</returns>
    private async Task NotifyAsync(AmiModuleManifest manifest)
    {
        try
        {
            await using var connection = await _dbFactory.GetConnectionAsync();
            var recipients = new HashSet<string>();
            foreach (var policy in manifest.Views.Select(t => t.Policy).Concat(manifest.Pages.Select(p => p.Policy)).Append(GXModulePolicies.View).Distinct())
            {
                var users = await connection.SelectAsync<GXUser>(GXQuery.GetUsersByPermission(NotificationAction.Edit, policy));
                foreach (var user in users)
                {
                    recipients.Add(user.Id!);
                }
            }
            _notifications.Add(recipients, new GXTarget(GXModulePolicies.Edit, manifest.Id, manifest.Name));
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Module {Id} notification failed: {Message}", manifest.Id, exception.Message);
        }
    }
    /// <inheritdoc />
    public void Dispose()
    {
        _runtime.Changed -= OnChanged;
    }

    /// <summary>Matches the permitted characters in managed module identity and version path segments.</summary>
    /// <returns>The generated module path-segment validation expression.</returns>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}




