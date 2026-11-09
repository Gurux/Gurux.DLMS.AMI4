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
    private readonly IDbConnectionFactory dbFactory;
    private readonly GXModuleRuntime runtime;
    private readonly ILogger<GXModuleService> logger;
    private readonly NotificationCache notifications;
    private readonly string modulesRoot;
    private readonly GXDevelopmentModuleResolver? development;
    private readonly bool isDevelopment;
    private readonly ConcurrentDictionary<string, (string Path, string Version)> developmentModules = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> announced = new(StringComparer.OrdinalIgnoreCase);

    public GXModuleService(IDbConnectionFactory dbFactory, GXModuleRuntime runtime, IHostEnvironment environment,
        ILogger<GXModuleService> logger, NotificationCache notifications, GXDevelopmentModuleResolver? development = null)
    {
        this.dbFactory = dbFactory;
        this.runtime = runtime;
        this.logger = logger;
        this.notifications = notifications;
        this.development = development;
        isDevelopment = environment.IsDevelopment();
        modulesRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Modules"));
        runtime.Changed += OnChanged;
    }

    public IReadOnlyList<string> GetRemovalOrder(IEnumerable<string> ids) => runtime.GetRemovalOrder(ids);
    public IReadOnlyList<AmiModuleManifest> GetManifests() => runtime.GetManifests(false);
    public AmiModuleManifest? GetManifest(string id) => runtime.GetManifest(id);
    public Task<byte[]> ReadAssetAsync(string id, string contentId, string fileName, CancellationToken cancellationToken = default)
        => runtime.ReadAssetAsync(id, contentId, fileName, cancellationToken);
    public Task ExecuteAsync(GXModule module, string? settings, CancellationToken cancellationToken = default)
        => runtime.ExecuteAsync(module.Id, settings, cancellationToken);

    public async Task StartConfiguredModulesAsync(CancellationToken cancellationToken = default)
    {
        var dev = development == null ? [] : await CollectDevelopmentCandidatesAsync(cancellationToken);
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
                        definitions.Add(candidate.Instance.Id, runtime.DescribeStartupCandidate(candidate.Instance, candidate.Settings));
                    }
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    failed.Add(candidate.Instance.Id);
                    await RecordStartupFailureAsync(candidate.Instance.Id, candidate.Instance.Name, error);
                }
            }
            await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
            var storedArgs = GXSelectArgs.SelectAll<GXModule>(m => m.Enabled == true);
            storedArgs.Columns.Add<GXModuleProperty>();
            storedArgs.Columns.Exclude<GXModuleProperty>(p => p.Module!);
            storedArgs.Joins.AddLeftJoin<GXModule, GXModuleProperty>(j => j.Id, j => j.Module!);
            var storedModules = await connection.SelectAsync<GXModule>(storedArgs, cancellationToken);
            foreach (var stored in storedModules)
            {
                if (runtime.GetManifest(stored.Id)?.State == AmiModuleState.Failed)
                {
                    continue;
                }
                bool oldDevelopment = !string.IsNullOrEmpty(stored.Path) && Path.GetFullPath(stored.Path).StartsWith(
                    Path.Combine(modulesRoot, ".development") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                if (devById.ContainsKey(stored.Id))
                {
                    // Development candidates are started below; keep the persisted activation choice intact.
                    continue;
                }
                if (oldDevelopment)
                {
                    runtime.RecordStartupFailure(stored.Id, stored.Name ?? stored.Id,
                        new InvalidOperationException("The configured development module source is unavailable."));
                    continue;
                }
                if (!isDevelopment && (stored.Status & ModuleStatus.Installed) == 0)
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

                    definitions.Add(stored.Id, runtime.DescribeStartupCandidate(loaded.Module, stored.GetSettings()));
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
                    var plan = GXModuleStartupPlanner.Plan([.. definitions.Values], runtime.GetStartupProviders(), [root]);
                    foreach (var item in plan)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (failed.Contains(item.Id))
                        {
                            throw new AmiModuleDependencyException($"Required module '{item.Id}' failed during startup.");
                        }

                        if (runtime.GetManifest(item.Id)?.State == AmiModuleState.Active)
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

    private Task RecordStartupFailureAsync(string id, string name, Exception error)
    {
        logger.LogError(error, "Module {Id} startup failed; deployment choice retained, host startup continues.", id);
        runtime.RecordStartupFailure(id, name, error);
        return Task.CompletedTask;
    }

    public async Task EnableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Save the explicit choice before loading; a startup failure must not undo it.
            await SetEnabledAsync(module.Id, true, cancellationToken);
            runtime.SetEnabled(module.Id, true);
            if (runtime.GetManifest(module.Id)?.State == AmiModuleState.Active)
            {
                await runtime.UpdateSettingsAsync(module.Id, module.GetSettings(), cancellationToken);
                return;
            }
            await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
            var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, module.Id, cancellationToken)
                ?? throw new InvalidOperationException("Module does not exist.");
            module.SetSettings(AmiModuleSettingsStorage.PreserveState(module.Properties == null ? stored.GetSettings() : module.GetSettings(), stored.GetSettings()));
            var selected = developmentModules.GetValueOrDefault(stored.Id);
            string selectedPath = selected.Path ?? ResolvePath(stored);
            var (instance, loadContext) = Load(selectedPath);
            if (instance.Id != stored.Id)
            {
                loadContext.Unload();
                throw new InvalidOperationException("Module package identity does not match the stored Id.");
            }
            try
            {
                await runtime.ActivateAsync(instance, selectedPath, selected.Version ?? stored.Version ?? "0", module.GetSettings(),
                    () =>
                    {
                        loadContext.Unload();
                        return ValueTask.CompletedTask;
                    }, cancellationToken,
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
                await runtime.StopAsync(module.Id, cancellationToken: CancellationToken.None);
                throw;
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            runtime.RecordStartupFailure(module.Id, module.Name ?? module.Id, error);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<DevelopmentCandidate>> CollectDevelopmentCandidatesAsync(CancellationToken cancellationToken)
    {
        var sources = await development!.ResolveAsync(cancellationToken, continueOnError: true);
        var candidates = new List<DevelopmentCandidate>();
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var run = Path.Combine(modulesRoot, ".development", Guid.NewGuid().ToString("N"));
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
                    runtime.RecordStartupFailure(local[0].Instance.Id, local[0].Instance.Name, error);
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
                logger.LogError(error, "Development module source {Source} disabled; startup continues.", source.Directory);
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
    private sealed record DevelopmentCandidate(IGXAmiModule Instance, PluginLoadContext Context, string Path, string? Settings);

    private async Task<bool> RegisterDevelopmentCandidateAsync(DevelopmentCandidate candidate, CancellationToken cancellationToken)
    {
        await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
        var instance = candidate.Instance;
        ValidateSegment(instance.Id);
        string version = FileVersionInfo.GetVersionInfo(candidate.Path).FileVersion ?? "0";
        var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, instance.Id, cancellationToken);
        bool activate = GXDevelopmentModuleRegistration.ShouldActivate(stored);
        if (stored == null)
        {
            string? creatorId = development?.Options.CreatorId;
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
                Type = runtime.GetModuleType(instance)
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
        stored.Type = runtime.GetModuleType(instance);
        await GXDevelopmentModuleRegistration.EnsureAsync(connection, stored, version, cancellationToken);
        // Selection is independent of activation, so disabled development modules can later be enabled
        // from the current build and cannot be replaced by an installed ZIP with the same identity.
        developmentModules[instance.Id] = (candidate.Path, version);
        return activate;
    }

    private async Task ActivateDevelopmentCandidateAsync(DevelopmentCandidate candidate, CancellationToken cancellationToken)
    {
        await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
        var instance = candidate.Instance;
        string version = FileVersionInfo.GetVersionInfo(candidate.Path).FileVersion ?? "0";
        var record = await AmiModuleSettingsStorage.ReadModuleAsync(connection, instance.Id, cancellationToken)
            ?? throw new InvalidOperationException("The development module has not been registered.");
        await runtime.ActivateAsync(instance, candidate.Path, version, candidate.Settings ?? record.GetSettings(),
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
                    Type = runtime.GetModuleType(instance)
                };
                connection.Update(GXUpdateArgs.Update(activated,
                    m => new { m.ReleaseVersion, m.Version, m.Status, m.Path, m.FileName, m.Type, m.Icon }));
                return Task.CompletedTask;
            });
        if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
        {
            logger.LogInformation("Development module {Id} loaded from local build snapshot {Path}. Restart AMI after rebuilding.", instance.Id, candidate.Path);
        }
    }
    public async Task<AmiModuleState> DisableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await SetEnabledAsync(module.Id, false, cancellationToken);
            runtime.SetEnabled(module.Id, false);
            return await runtime.StopAsync(module.Id, cancellationToken: cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpdateModuleSettingsAsync(GXModule module, CancellationToken cancellationToken = default)
    {
        var gate = gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (runtime.GetManifest(module.Id)?.State == AmiModuleState.Active)
            {
                await runtime.UpdateSettingsAsync(module.Id, module.GetSettings(), cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteModuleAsync(GXModule module, CancellationToken cancellationToken = default, AmiModuleUninstallOptions? uninstallOptions = null)
    {
        uninstallOptions ??= new();
        uninstallOptions.Validate();
        if (runtime.GetManifest(module.Id)?.State == AmiModuleState.Stopping)
        {
            throw new InvalidOperationException("Module removal is already pending.");
        }

        var gate = gates.GetOrAdd(module.Id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
            var stored = await AmiModuleSettingsStorage.ReadModuleAsync(connection, module.Id, cancellationToken)
                ?? throw new InvalidOperationException("Module does not exist.");
            await SetEnabledAsync(module.Id, false, cancellationToken);
            bool active = runtime.GetManifest(module.Id)?.State == AmiModuleState.Active;
            AmiModuleState state = active ? await runtime.RemoveAsync(module.Id, cancellationToken, uninstallOptions)
                : await runtime.StopAsync(module.Id, cancellationToken: cancellationToken);
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
                await runtime.ActivateAsync(instance, ResolvePath(stored), stored.Version ?? "0", stored.GetSettings(),
                    () =>
                    {
                        context.Unload();
                        return ValueTask.CompletedTask;
                    }, cancellationToken,
                    start: false, uninstallOptions: uninstallOptions);
            }
            // Development directories belong to the developer and are never deleted.
            if (string.IsNullOrEmpty(stored.Path))
            {
                string directory = ManagedPath(stored.Id, stored.Version ?? "0");
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
                logger.LogWarning("Temporary module package cleanup pending: {Message}", exception.Message);
            }
        }
    }

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
                logger.LogWarning("Development module {File} was not activated: {Message}", file, exception.Message);
            }
        }
        return added;
    }

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
            type = runtime.GetModuleType(descriptor);
            dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, descriptor.Dependencies);
            runtime.ValidateCandidateDependencies(id, dependencies);
        }
        finally
        {
            inspection.Unload();
        }
        var gate = gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            using var dependencyReservation = runtime.ReserveCandidateDependencies(id, dependencies);
            if (developmentModules.ContainsKey(id))
            {
                throw new InvalidOperationException($"Module '{id}' is selected by development settings. Remove that development entry and restart AMI before installing its ZIP package.");
            }

            await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
            var old = await AmiModuleSettingsStorage.ReadModuleAsync(connection, id, cancellationToken);
            if (old == null && user == null)
            {
                throw new InvalidOperationException("Installing a new module requires an authenticated installer.");
            }

            string destination = managed ? ManagedPath(id, version) : Path.GetDirectoryName(file)!;
            string relativeFile = managed ? Path.GetRelativePath(sourceRoot, file) : Path.GetFileName(file);
            string main = Path.GetFullPath(Path.Combine(destination, relativeFile));
            if (!managed && runtime.GetManifest(id) is { State: AmiModuleState.Active } active && old?.Version == version && ResolvePath(old) == main
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
            bool restoreActiveOnFailure = runtime.GetManifest(id)?.State == AmiModuleState.Active;
            if (await runtime.StopAsync(id, cancellationToken: cancellationToken) == AmiModuleState.Stopping)
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
                await runtime.ActivateAsync(instance, main, version, old?.GetSettings(),
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
                        logger.LogWarning("Previous module package cleanup pending: {Message}", exception.Message);
                    }
                }
                return item;
            }
            catch
            {
                await runtime.StopAsync(id, cancellationToken: CancellationToken.None);
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
                        await runtime.ActivateAsync(previousInstance, restoredPath, restored.Version ?? "0", restored.GetSettings(),
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

    private sealed class ModuleAssemblyException() : Exception("Assembly contains no module implementation.");
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
    private string ManagedPath(string id, string version)
    {
        ValidateSegment(id);
        ValidateSegment(version);
        string path = Path.GetFullPath(Path.Combine(modulesRoot, id, version));
        if (!IsWithin(path, modulesRoot))
        {
            throw new InvalidOperationException("Module directory is outside the managed root.");
        }

        return path;
    }
    private void ValidateManagedDirectory(string directory)
    {
        directory = Path.GetFullPath(directory);
        if (!IsWithin(directory, modulesRoot))
        {
            throw new InvalidOperationException("Module directory is outside the managed root.");
        }

        for (var item = new DirectoryInfo(directory); item != null && IsWithin(item.FullName, modulesRoot); item = item.Parent)
        {
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Refusing to modify a linked module directory.");
            }
        }

        if (Directory.Exists(modulesRoot) && (File.GetAttributes(modulesRoot) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Managed module root cannot be a linked directory.");
        }
    }
    private void DeleteManagedDirectory(string directory)
    {
        ValidateManagedDirectory(directory);
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
    private static bool IsWithin(string path, string directory) => path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void ValidateSegment(string value)
    {
        if (!MyRegex().IsMatch(value) || value.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid module identity/version path segment.");
        }
    }
    private async Task SetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
    {
        await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
        var value = new GXModule(id) { Enabled = enabled, Updated = DateTimeOffset.UtcNow };
        await connection.UpdateAsync(GXUpdateArgs.Update(value, m => new { m.Enabled, m.Updated }), cancellationToken);
    }
    private void OnChanged()
    {
        foreach (var manifest in runtime.GetManifests(false))
        {
            string state = manifest.ContentId + ":" + manifest.State;
            if (announced.GetValueOrDefault(manifest.Id) == state)
            {
                continue;
            }

            announced[manifest.Id] = state;
            _ = NotifyAsync(manifest);
        }
    }
    private async Task NotifyAsync(AmiModuleManifest manifest)
    {
        try
        {
            await using var connection = await dbFactory.GetConnectionAsync();
            var recipients = new HashSet<string>();
            foreach (var policy in manifest.Views.Select(t => t.Policy).Concat(manifest.Pages.Select(p => p.Policy)).Append(GXModulePolicies.View).Distinct())
            {
                var users = await connection.SelectAsync<GXUser>(GXQuery.GetUsersByPermission(NotificationAction.Edit, policy));
                foreach (var user in users)
                {
                    recipients.Add(user.Id!);
                }
            }
            notifications.Add(recipients, new GXTarget(GXModulePolicies.Edit, manifest.Id, manifest.Name));
        }
        catch (Exception exception)
        {
            logger.LogWarning("Module {Id} notification failed: {Message}", manifest.Id, exception.Message);
        }
    }
    public void Dispose()
    {
        runtime.Changed -= OnChanged;
    }

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}




