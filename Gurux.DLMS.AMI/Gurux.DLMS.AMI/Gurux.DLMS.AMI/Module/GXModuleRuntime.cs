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

using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Owns module services and registrations; all module operations hold a cancellable lease.</summary>
public sealed partial class GXModuleRuntime(IServiceProvider host, IConfiguration configuration, ILogger<GXModuleRuntime> logger)
    : IGXAmiModuleServices, IGXModuleAuthenticationRuntime, IAsyncDisposable
{
    /// <summary>Protects shared in-memory state against concurrent access.</summary>
    private readonly Lock _sync = new();
    /// <summary>Tracks dependencies and exported services between loaded modules.</summary>
    private readonly GXModuleDependencyGraph _dependencyGraph = new();
    /// <summary>Registered entries indexed for lookup by this component.</summary>
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Validated module manifests indexed by module identifier.</summary>
    private readonly Dictionary<string, AmiModuleManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Maps exported service types to their providing module identifiers.</summary>
    private readonly Dictionary<Type, string> _exports = [];
    /// <summary>Per-key synchronization that prevents concurrent mutations of the same resource.</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Async-local operation state restored when the enclosing operation finishes.</summary>
    private readonly AsyncLocal<Invocation?> _current = new();
    /// <summary>Indicates that disposal has begun and this instance can no longer accept work.</summary>
    private bool _disposed;
    /// <summary>Prevents new module work while runtime shutdown is in progress.</summary>
    private bool _shuttingDown;
    /// <summary>Completion signals for module installations currently in progress.</summary>
    private readonly Dictionary<string, TaskCompletionSource> _pendingInstallations = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Performs the  operation.</summary>
    public event Action? Changed;
    /// <summary>Performs the  operation.</summary>
    public event Func<AmiModuleManifest, Task>? Faulted;

    /// <summary>Retains a module invocation's host scope, cancellation, and calling chain.</summary>
    /// <param name="Host">Host service scope available to the module invocation.</param>
    /// <param name="Token">Cancellation token bounding the invocation lifetime.</param>
    /// <param name="Entry">Module generation whose execution lease is held.</param>
    /// <param name="RequestToken">Original request cancellation token supplied by the caller.</param>
    /// <param name="Parent">Enclosing module invocation, if the call is nested.</param>
    private sealed record Invocation(IServiceProvider Host, CancellationToken Token, Entry Entry, CancellationToken RequestToken, Invocation? Parent);
    /// <summary>Indicates that a module generation could not provide a valid execution lease.</summary>
    /// <param name="message">Explanation of the unavailable module lease.</param>
    private sealed class LeaseUnavailableException(string message) : AmiModuleUnavailableException(message);
    /// <summary>Retains the resources and lifecycle state belonging to one loaded module generation.</summary>
    /// <param name="module">Loaded module instance retained by this generation.</param>
    /// <param name="path">Resolved directory or assembly path belonging to the module.</param>
    /// <param name="settings">Settings applied when the generation was created.</param>
    /// <param name="manifest">Validated manifest describing the module and its exports.</param>
    /// <param name="unload">Optional callback that releases the module load context.</param>
    private sealed class Entry(IGXAmiModule module, string path, AmiModuleSettings settings, AmiModuleManifest manifest, Func<ValueTask>? unload)
    {
        /// <summary>Loaded module instance owned by this generation.</summary>
        public IGXAmiModule? Module = module;
        /// <summary>Unique identifier distinguishing this module generation from replacements.</summary>
        public readonly string Generation = Guid.NewGuid().ToString("N");
        /// <summary>Indicates that module startup completed.</summary>
        public bool Started;
        /// <summary>UTC time at which module startup completed, when available.</summary>
        public DateTimeOffset? StartedAt;
        /// <summary>UTC time at which graceful module shutdown was requested.</summary>
        public DateTimeOffset? StopRequestedAt;
        /// <summary>UTC time at which module shutdown completed, when available.</summary>
        public DateTimeOffset? StoppedAt;
        /// <summary>Indicates that shutdown required forced cancellation.</summary>
        public bool ForcedCancellation;
        /// <summary>Latest health reports from module background services.</summary>
        public Gurux.DLMS.AMI.Module.Health.AmiModuleBackgroundHealth[] BackgroundHealth = [];
        /// <summary>Indicates that the module should be removed after shutdown.</summary>
        public bool UninstallRequested;
        /// <summary>Options controlling cleanup when this module is uninstalled.</summary>
        public AmiModuleUninstallOptions UninstallOptions = new();
        /// <summary>Failure details retained if module cleanup cannot finish.</summary>
        public string? CleanupError;
        /// <summary>Indicates that module cleanup has completed.</summary>
        public bool Cleaned;
        /// <summary>Indicates that module execution failed.</summary>
        public bool Faulted;
        /// <summary>Dependency injection provider dedicated to the module generation.</summary>
        public ServiceProvider? Provider;
        /// <summary>Filesystem path associated with the retained resource.</summary>
        public string Path = path;
        /// <summary>Module configuration applied to this generation.</summary>
        public AmiModuleSettings Settings = settings;
        /// <summary>Serializer settings used for module-owned configuration payloads.</summary>
        public JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        /// <summary>Validated manifest describing this module generation.</summary>
        public AmiModuleManifest Manifest = manifest;
        /// <summary>Current lifecycle state of the module generation.</summary>
        public AmiModuleState State = AmiModuleState.Loading;
        /// <summary>Cancellation source linked to the lifetime of the module generation.</summary>
        public CancellationTokenSource Lifetime = new();
        /// <summary>Cancellation source triggered when graceful module shutdown expires.</summary>
        public CancellationTokenSource Forced = new();
        /// <summary>Signals that all calls to the module generation have completed.</summary>
        public TaskCompletionSource Drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>Number of module calls currently in progress.</summary>
        public int Calls;
        /// <summary>Endpoint definitions registered by this module generation.</summary>
        public AmiEndpointDefinition[] Endpoints = [];
        /// <summary>Endpoints owned by this module generation, indexed by endpoint key.</summary>
        public Dictionary<string, Endpoint> OwnedEndpoints = [];
        /// <summary>Protects changes to endpoint source registrations.</summary>
        public readonly object SourceSync = new();
        /// <summary>Endpoint data sources registered by this module generation.</summary>
        public EndpointDataSource[] Sources = [];
        /// <summary>Middleware definitions registered by this module generation.</summary>
        public AmiMiddlewareDefinition[] Middleware = [];
        /// <summary>Service types exported by this module generation.</summary>
        public Type[] Exports = [];
        /// <summary>Module type used to create the authentication provider.</summary>
        public Type? AuthenticatorType;
        /// <summary>Hosted services owned by this module generation.</summary>
        public IHostedService[] Background = [];
        /// <summary>Disposable resources registered for cleanup with this generation.</summary>
        public ConcurrentBag<IDisposable> Owned = [];
        /// <summary>Dependency injection scope used during module lifecycle callbacks.</summary>
        public AsyncServiceScope LifecycleScope;
        /// <summary>Dependency injection scope used for module host services.</summary>
        public AsyncServiceScope HostScope;
        /// <summary>Indicates that module dependency injection scopes were initialized.</summary>
        public bool ScopesCreated;
        /// <summary>Task representing asynchronous module cleanup, when started.</summary>
        public Task? Cleanup;
        /// <summary>Task waiting for forced cancellation during module shutdown.</summary>
        public Task? ForcedSignal;
        /// <summary>Callback that releases the module load context during cleanup.</summary>
        public Func<ValueTask>? Unload = unload;
    }

    /// <summary>Returns active module manifests that are not awaiting uninstallation.</summary>
    /// <returns>Active provider manifests indexed by module identifier.</returns>
    private Dictionary<string, AmiModuleManifest> ActiveProviders() => _entries.Where(e => e.Value.State == AmiModuleState.Active && !e.Value.UninstallRequested).ToDictionary(e => e.Key, e => e.Value.Manifest, StringComparer.OrdinalIgnoreCase);
    /// <summary>Maps exported service names to their currently registered module providers.</summary>
    /// <returns>Exported service names mapped to module identifiers.</returns>
    private Dictionary<string, string> ServiceProviders() => _exports.ToDictionary(e => e.Key.FullName!, e => e.Value, StringComparer.Ordinal);
    /// <summary>Returns the view areas and scheduling capabilities declared by a module.</summary>
    /// <param name="module">The module.</param>
    public ModuleType GetModuleType(IGXAmiModule module)
    {
        var builder = new AmiModuleBuilder(module.Id, configuration, new AmiModuleSettings(null));
        module.Configure(builder);
        return (builder.Views.Any(v => v.Area == AmiViewArea.Configuration) ? ModuleType.Settings : 0)
            | (builder.Views.Any(v => v.Area == AmiViewArea.Extension) ? ModuleType.Extension : 0)
            | (module.CanSchedule ? ModuleType.Schedule : 0);
    }
    /// <summary>Validates the module and describes its startup dependencies and exports.</summary>
    /// <param name="module">The module to inspect.</param>
    /// <param name="settings">The saved module settings.</param>
    public GXModuleStartupCandidate DescribeStartupCandidate(IGXAmiModule module, string? settings)
    {
        var declarations = GXModuleDependencyValidator.ValidateDeclarations(module.Id, module.Dependencies);
        GXModuleDependencyValidator.ValidateHostDependencies(declarations);
        var builder = new AmiModuleBuilder(module.Id, configuration, new AmiModuleSettings(settings));
        module.Configure(builder);
        Validate(builder);
        return new(module.Id, GXModuleDependencyValidator.ReadCompatibilityVersion(module.GetType().Assembly), declarations, [.. builder.Exports.Select(t => t.FullName!)]);
    }
    /// <summary>Snapshots active modules and their compatibility versions, dependencies, and exported service names.</summary>
    /// <returns>Startup provider candidates excluding modules awaiting uninstallation.</returns>
    public IReadOnlyList<GXModuleStartupCandidate> GetStartupProviders()
    {
        lock (_sync)
        {
            return [.. _entries.Values.Where(e => e.State == AmiModuleState.Active && !e.UninstallRequested).Select(e => new GXModuleStartupCandidate(e.Manifest.Id, e.Manifest.CompatibilityVersion, e.Manifest.Dependencies, [.. e.Exports.Select(t => t.FullName!)]))];
        }
    }
    /// <summary>Validates dependency declarations and checks that active modules and host services can satisfy them.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="dependencies">The dependencies.</param>
    public void ValidateCandidateDependencies(string id, IEnumerable<AmiModuleDependency> dependencies)
    {
        var values = GXModuleDependencyValidator.ValidateDeclarations(id, dependencies);
        GXModuleDependencyValidator.ValidateHostDependencies(values);
        lock (_sync)
        {
            GXModuleDependencyGraph.Resolve(id, values, ActiveProviders(), ServiceProviders());
        }
    }
    /// <summary>Releases a reserved module candidate exactly once when its scope ends.</summary>
    /// <param name="release">Callback that releases the reserved resource.</param>
    private sealed class CandidateReservation(Action release) : IDisposable
    {
        /// <summary>Cleanup callback atomically cleared to ensure it executes at most once.</summary>
        private Action? _action = release;

        /// <inheritdoc />
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
    /// <summary>Reserves the resolved provider modules while a candidate installation is in progress.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="declarations">The declarations.</param>
    public IDisposable ReserveCandidateDependencies(string id, IEnumerable<AmiModuleDependency> declarations)
    {
        var dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, declarations);
        GXModuleDependencyValidator.ValidateHostDependencies(dependencies);
        string key = id + " (installation)";
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed || _shuttingDown, this);
            var suppliers = GXModuleDependencyGraph.Resolve(id, dependencies, ActiveProviders(), ServiceProviders());
            _dependencyGraph.Reserve(key, suppliers);
            _pendingInstallations.Add(key, new(TaskCreationOptions.RunContinuationsAsynchronously));
        }
        return new CandidateReservation(() =>
        {
            lock (_sync)
            {
                _dependencyGraph.Release(key);
                if (_pendingInstallations.Remove(key, out var pending))
                {
                    pending.TrySetResult();
                }
            }
        });
    }
    /// <summary>Orders requested module removals so dependents are stopped before their providers.</summary>
    /// <param name="ids">The ids.</param>
    public string[] GetRemovalOrder(IEnumerable<string> ids)
    {
        lock (_sync)
        {
            return _dependencyGraph.GetStopOrder(ids);
        }
    }
    /// <summary>Performs the stop all operation.</summary>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task StopAllAsync(CancellationToken token = default)
    {
        Task[] installations;
        lock (_sync)
        {
            _shuttingDown = true;
            installations = [.. _pendingInstallations.Values.Select(p => p.Task)];
        }
        if (installations.Length != 0)
        {
            await Task.WhenAll(installations).WaitAsync(token);
        }

        string[] ids;
        lock (_sync)
        {
            ids = _dependencyGraph.GetStopOrder(_entries.Keys);
        }

        foreach (var id in ids)
        {
            await StopAsync(id, cancellationToken: token);
            Task? pending;
            lock (_sync)
            {
                pending = _entries.GetValueOrDefault(id)?.Cleanup;
            }

            if (pending != null)
            {
                await pending.WaitAsync(token);
            }
        }
    }
    /// <summary>Performs the activate operation.</summary>
    /// <param name="module">The module.</param>
    /// <param name="path">The path.</param>
    /// <param name="version">The version.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="unload">The unload.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <param name="beforeStart">The before start.</param>
    /// <param name="beforePublish">The before publish.</param>
    /// <param name="start">The start.</param>
    /// <param name="uninstallOptions">The uninstall options.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    public async Task<AmiModuleManifest> ActivateAsync(IGXAmiModule module, string path, string version, string? settings,
        Func<ValueTask>? unload = null, CancellationToken cancellationToken = default,
        Func<AmiModuleContext, CancellationToken, Task>? beforeStart = null,
        Func<CancellationToken, Task>? beforePublish = null, bool start = true, AmiModuleUninstallOptions? uninstallOptions = null)
    {
        ArgumentNullException.ThrowIfNull(module);
        uninstallOptions?.Validate();
        if (!MyRegex().IsMatch(module.Id))
        {
            throw new ArgumentException("Module Id must be a stable URL-safe identifier.");
        }

        string id = module.Id;
        var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        Entry? entry = null;
        try
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed || _shuttingDown, this);
                if (_entries.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Module {id} is already loaded or stopping.");
                }
            }
            var dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, module.Dependencies);
            GXModuleDependencyValidator.ValidateHostDependencies(dependencies);
            path = System.IO.Path.GetFullPath(path);
            string contentId = await ComputeContentIdAsync(path, cancellationToken);
            var assets = GetAssets(path);
            var manifest = new AmiModuleManifest(id, module.Name, version, contentId, AmiModuleState.Loading,
                assets)
            {
                Enabled = start,
                Dependencies = dependencies,
                CompatibilityVersion = GXModuleDependencyValidator.ReadCompatibilityVersion(module.GetType().Assembly)
            };
            entry = new Entry(module, path, new AmiModuleSettings(settings, host.GetService<IDbConnectionFactory>(), id), manifest, unload)
            {
                UninstallOptions = uninstallOptions ?? new(),
                UninstallRequested = !start && uninstallOptions != null
            };
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed || _shuttingDown, this);
                var resolved = start ? GXModuleDependencyGraph.Resolve(id, dependencies, ActiveProviders(), ServiceProviders()) : [];
                _dependencyGraph.Reserve(id, resolved);
                entry.Manifest = manifest = manifest with
                {
                    ResolvedDependencyModuleIds = resolved
                };
                _entries.Add(id, entry);
                _manifests[id] = manifest;
            }
            NotifyChanged();
            var builder = new AmiModuleBuilder(id, configuration, entry.Settings);
            module.Configure(builder);
            await entry.Settings.RegisterSchemaAsync(builder.SettingsSchema, cancellationToken);
            entry.Manifest = manifest = manifest with
            {
                Views = [.. builder.Views],
                Pages = [.. builder.Pages]
            };
            Validate(builder);
            entry.Endpoints = [.. builder.Endpoints];
            entry.Middleware = [.. builder.Middleware];
            entry.AuthenticatorType = builder.AuthenticatorType;
            entry.Exports = start ? [.. builder.Exports] : [];
            lock (_sync)
            {
                foreach (var contract in entry.Exports)
                {
                    if (_exports.Keys.Any(existing => existing.FullName == contract.FullName))
                    {
                        throw new InvalidOperationException($"An active/loading module already exports {contract.FullName}.");
                    }
                }

                foreach (var contract in entry.Exports)
                {
                    _exports.Add(contract, id);
                }

                _manifests[id] = manifest;
            }
            foreach (var contract in builder.HostSingletonTypes)
            {
                builder.Services.Add(ServiceDescriptor.Singleton(contract, host.GetRequiredService(contract)));
            }

            builder.Services.AddSingleton<IConfiguration>(configuration);
            builder.Services.AddSingleton<ILoggerFactory>(host.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance);
            builder.Services.AddLogging();
            builder.Services.AddSingleton<IGXAmiModuleServices>(this);
            builder.Services.AddSingleton<IGXAmiModuleSettingsSnapshot>(entry.Settings);
            builder.Services.AddScoped<IGXAmiModuleUserSettings>(sp => sp.GetRequiredService<AmiModuleContext>().UserSettings);
            builder.Services.AddScoped<IGXAmiModuleResources>(sp => sp.GetRequiredService<AmiModuleContext>().Resources);
            if (host.GetService<IHttpClientFactory>() is { } httpFactory)
            {
                builder.Services.AddSingleton(httpFactory);
            }

            if (host.GetService<IHttpContextAccessor>() is { } accessor)
            {
                builder.Services.AddSingleton(accessor);
            }

            var owned = entry;
            builder.Services.AddScoped<AmiModuleContext>(sp => new AmiModuleContext
            {
                ModuleId = id,
                Services = sp,
                HostServices = _current.Value?.Host ?? host,
                Settings = owned.Settings,
                Resources = new GXModuleResources(id, _current.Value?.Host ?? host),
                UserSettings = new GXModuleUserSettings(id, _current.Value?.Host ?? host),
                UninstallOptions = owned.UninstallOptions,
                CancellationToken = _current.Value?.Token ?? owned.Lifetime.Token,
                RequestCancellationToken = _current.Value?.RequestToken ?? default,
                StoppingToken = owned.Lifetime.Token,
                ForcedCancellationToken = owned.Forced.Token,
                Events = CreateModuleEvents(owned),
                JsonOptions = owned.Json,
                Track = resource => owned.Owned.Add(resource),
                PackageDirectory = System.IO.Path.GetDirectoryName(owned.Path)
            });
            builder.Services.AddScoped<IGXAmiEvents>(sp => sp.GetRequiredService<AmiModuleContext>().Events);
            entry.Provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            entry.HostScope = host.CreateAsyncScope();
            entry.LifecycleScope = entry.Provider.CreateAsyncScope();
            entry.ScopesCreated = true;
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, entry.Lifetime.Token);
            var previous = _current.Value;
            _current.Value = new(entry.HostScope.ServiceProvider, entry.Lifetime.Token, entry, cancellationToken, previous);
            try
            {
                startup.Token.ThrowIfCancellationRequested();
                var context = entry.LifecycleScope.ServiceProvider.GetRequiredService<AmiModuleContext>();
                foreach (var contract in builder.Exports)
                {
                    _ = entry.LifecycleScope.ServiceProvider.GetRequiredService(contract);
                }

                if (entry.AuthenticatorType != null)
                {
                    _ = entry.LifecycleScope.ServiceProvider.GetRequiredService(entry.AuthenticatorType);
                }

                if (builder.OwnedTables.Count != 0)
                {
                    await new GXModuleTableRegistry(context.HostServices.GetRequiredService<IGXDatabase>()).RegisterAsync(id, builder.OwnedTables, startup.Token);
                }

                if (beforeStart != null)
                {
                    await beforeStart(context, startup.Token);
                }

                if (start)
                {
                    entry.Started = true;
                    entry.Background = [.. entry.LifecycleScope.ServiceProvider.GetServices<IHostedService>()];
                    foreach (var background in entry.Background)
                    {
                        await background.StartAsync(entry.Lifetime.Token);
                    }

                    await module.StartAsync(context, startup.Token);
                    entry.Sources = [.. builder.EndpointSourceTypes.Select(type => context.Services.GetRequiredService(type) as EndpointDataSource ?? throw new InvalidOperationException("Module endpoint source must derive from EndpointDataSource."))];
                    foreach (var source in entry.Sources)
                    {
                        entry.Owned.Add(ChangeToken.OnChange(() =>
                    {
                        try
                        {
                            return source.GetChangeToken();
                        }
                        catch (Exception error)
                        {
                            _ = FaultAsync(id, owned, error);
                            return new CancellationChangeToken(CancellationToken.None);
                        }
                    }, () =>
                    {
                        try
                        {
                            RefreshOwnedEndpoints(id, owned);
                        }
                        catch (Exception error)
                        {
                            _ = FaultAsync(id, owned, error);
                        }
                    }));
                    }

                    RefreshOwnedEndpoints(id, entry);
                }
                startup.Token.ThrowIfCancellationRequested();
                if (beforePublish != null)
                {
                    await beforePublish(startup.Token);
                }
            }
            finally
            {
                _current.Value = previous;
            }
            if (!start)
            {
                lock (_sync)
                {
                    entry.StopRequestedAt ??= DateTimeOffset.UtcNow;
                    entry.State = AmiModuleState.Stopping;
                    entry.Drained.TrySetResult();
                }
                RefreshServiceAvailability();
                Cancel(entry, id);
                await CleanupAsync(id, entry, false);
                if (entry.CleanupError != null)
                {
                    throw new InvalidOperationException("Module uninstall failed: " + entry.CleanupError);
                }

                return entry.Manifest;
            }
            lock (_sync)
            {
                if (entry.State == AmiModuleState.Stopping)
                {
                    throw new OperationCanceledException("Module was stopped while loading.");
                }

                entry.StartedAt = DateTimeOffset.UtcNow;
                entry.State = AmiModuleState.Active;
                _manifests[id] = entry.Manifest = manifest with
                {
                    State = AmiModuleState.Active
                };
            }
            NotifyChanged();
            foreach (var background in entry.Background.OfType<BackgroundService>())
            {
                if (background.ExecuteTask != null)
                {
                    _ = ObserveBackgroundAsync(id, entry, background.ExecuteTask);
                }
            }

            return entry.Manifest;
        }
        catch (Exception error)
        {
            entry?.Manifest = entry.Manifest with
                {
                    Error = entry.Manifest.Error ?? error.Message
                };

            if (entry != null && !entry.Cleaned)
            {
                lock (_sync)
                {
                    entry.StopRequestedAt ??= DateTimeOffset.UtcNow;
                    entry.State = AmiModuleState.Stopping;
                    entry.Drained.TrySetResult();
                    foreach (var contract in entry.Exports)
                    {
                        if (_exports.GetValueOrDefault(contract) == id)
                        {
                            _exports.Remove(contract);
                        }
                    }
                }
                RefreshServiceAvailability();
                Cancel(entry, id);
                await CleanupAsync(id, entry, failed: true);
            }
            else if (entry == null && unload != null)
            {
                await unload();
            }

            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Validates authenticator lifetime, endpoint authorization, route syntax, HTTP methods, and duplicate module routes.</summary>
    /// <param name="builder">Registration builder whose authenticator, endpoint, and export contracts are checked.</param>
    /// <exception cref="InvalidOperationException">Authenticator scope or endpoint authorization contracts are inconsistent.</exception>
    /// <exception cref="ArgumentException">A route, HTTP method declaration, or duplicate endpoint registration is invalid.</exception>
    private static void Validate(AmiModuleBuilder builder)
    {
        if (builder.AuthenticatorType is { } authenticator)
        {
            var registrations = builder.Services.Where(service => service.ServiceType == authenticator).ToArray();
            if (registrations.Length == 0 || registrations.Any(service => service.Lifetime != ServiceLifetime.Scoped))
            {
                throw new InvalidOperationException("A module authenticator must have a scoped lifetime.");
            }
        }
        var routes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in builder.Endpoints)
        {
            if (endpoint.IsAnonymous && endpoint.Policies.Count != 0)
            {
                throw new InvalidOperationException("An anonymous endpoint cannot require authorization policies.");
            }
            string pattern = endpoint.Pattern.Trim('/');
            if (pattern.Contains("..", StringComparison.Ordinal) || pattern.Contains('?') || pattern.Contains('#'))
            {
                throw new ArgumentException("Module routes must be relative route patterns.");
            }

            _ = RoutePatternFactory.Parse("/api/modules/" + builder.ModuleId + "/" + pattern);
            if (endpoint.Methods.Length == 0)
            {
                throw new ArgumentException("Endpoint must specify an HTTP method.");
            }

            foreach (string method in endpoint.Methods)
            {
                if (!routes.Add(method.ToUpperInvariant() + " " + pattern))
                {
                    throw new InvalidOperationException("Duplicate module endpoint.");
                }
            }
        }
        foreach (var contract in builder.Exports)
        {
            if (!contract.IsInterface || AssemblyLoadContext.GetLoadContext(contract.Assembly)?.IsCollectible == true)
            {
                throw new InvalidOperationException("Export interfaces must come from a shared contract assembly.");
            }
        }
    }

    /// <summary>Lists the main module assembly and local managed dependencies while excluding host-shared framework assemblies.</summary>
    /// <param name="path">Absolute path of the main module assembly.</param>
    /// <returns>Assembly filenames exposed as module assets.</returns>
    private static string[] GetAssets(string path)
    {
        var result = new List<string> { System.IO.Path.GetFileName(path) };
        foreach (string file in Directory.GetFiles(System.IO.Path.GetDirectoryName(path)!, "*.dll"))
        {
            string name = System.IO.Path.GetFileName(file);
            if (name == result[0] || name.StartsWith("System.", StringComparison.Ordinal) || name.StartsWith("Microsoft.", StringComparison.Ordinal)
                || PluginLoadContext.IsSharedAssembly(System.IO.Path.GetFileNameWithoutExtension(name)))
            {
                continue;
            }

            try
            {
                _ = AssemblyName.GetAssemblyName(file);
                result.Add(name);
            }
            catch (BadImageFormatException)
            {
            }
        }
        return [.. result];
    }

    /// <summary>Performs the compute content id operation.</summary>
    /// <param name="mainPath">The main path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public static async Task<string> ComputeContentIdAsync(string mainPath, CancellationToken cancellationToken = default)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(mainPath))!;
        foreach (string fullPath in Directory.EnumerateFiles(directory, "*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false
        })
            .OrderBy(n => System.IO.Path.GetRelativePath(directory, n), StringComparer.Ordinal))
        {
            string file = System.IO.Path.GetRelativePath(directory, fullPath).Replace('\\', '/');
            byte[] name = System.Text.Encoding.UTF8.GetBytes(file);
            byte[] content = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            hash.AppendData(BitConverter.GetBytes(name.Length));
            hash.AppendData(name);
            hash.AppendData(BitConverter.GetBytes(content.Length));
            hash.AppendData(content);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>Performs the get manifests operation.</summary>
    /// <param name="activeOnly">The active only.</param>
    public IReadOnlyList<AmiModuleManifest> GetManifests(bool activeOnly = true)
    {
        lock (_sync)
        {
            return [.. _manifests.Values.Where(m => !activeOnly || m.State == AmiModuleState.Active).OrderBy(m => m.Id)];
        }
    }
    /// <summary>Performs the get manifest operation.</summary>
    /// <param name="id">The item identifier.</param>
    public AmiModuleManifest? GetManifest(string id)
    {
        lock (_sync)
        {
            return _manifests.GetValueOrDefault(id);
        }
    }
    /// <summary>Reads this host's current lifecycle state, including retained stopped and failed records.</summary>
    public AmiModuleState? GetState(string id)
    {
        lock (_sync)
        {
            return _manifests.GetValueOrDefault(id)?.State;
        }
    }

    /// <summary>Updates the persisted deployment choice included in runtime snapshots.</summary>
    public void SetEnabled(string id, bool enabled)
    {
        lock (_sync)
        {
            if (_manifests.TryGetValue(id, out var manifest))
            {
                _manifests[id] = manifest with { Enabled = enabled };
            }
        }
        NotifyChanged();
    }
    /// <summary>Performs the get endpoints operation.</summary>
    public IReadOnlyList<AmiPublishedEndpoint> GetEndpoints()
    {
        lock (_sync)
        {
            return [.. _entries.Where(p => p.Value.State == AmiModuleState.Active).SelectMany(p => p.Value.Endpoints.Select((e, i)
        => new AmiPublishedEndpoint(p.Key, p.Value.Generation, i, "/api/modules/" + p.Key + "/" + e.Pattern.Trim('/'), [.. e.Methods], [.. e.Policies])
        {
            HasAuthenticator = p.Value.AuthenticatorType != null,
            IsAnonymous = e.Methods.Length != 0 && e.Methods.All(method => GXModuleAnonymousAccess.IsAllowed(configuration, p.Key, e, method))
        }))];
        }
    }

    /// <summary>Refreshes source-owned endpoints only while the entry still represents the current module generation.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    private void RefreshOwnedEndpoints(string id, Entry entry)
    {
        lock (entry.SourceSync)
        {
            EndpointDataSource[] sources;
            lock (_sync)
            {
                if (entry.State == AmiModuleState.Stopping || !ReferenceEquals(_entries.GetValueOrDefault(id), entry))
                {
                    return;
                }

                sources = entry.Sources;
            }
            var snapshot = sources.SelectMany(s => s.Endpoints).ToDictionary(_ => Guid.NewGuid().ToString("N"), e => e);
            bool active;
            lock (_sync)
            {
                if (entry.State == AmiModuleState.Stopping || !ReferenceEquals(_entries.GetValueOrDefault(id), entry))
                {
                    return;
                }

                entry.OwnedEndpoints = snapshot;
                active = entry.State == AmiModuleState.Active;
            }
            if (active)
            {
                NotifyChanged();
            }
        }
    }
    /// <summary>Performs the get owned endpoints operation.</summary>
    public IReadOnlyList<Endpoint> GetOwnedEndpoints()
    {
        lock (_sync)
        {
            return [.. _entries.Where(p => p.Value.State == AmiModuleState.Active).SelectMany(p => p.Value.OwnedEndpoints.Select(pair =>
            {
                string id = p.Key, generation = p.Value.Generation, key = pair.Key;
                Task dispatch(HttpContext http) => DispatchOwnedEndpointAsync(id, generation, key, http);
                if (pair.Value is RouteEndpoint route)
                {
                    var builder = new RouteEndpointBuilder(dispatch, route.RoutePattern, route.Order) { DisplayName = route.DisplayName };
                    foreach (var metadata in route.Metadata)
                    {
                        builder.Metadata.Add(metadata);
                    }

                    builder.Metadata.Add(new AmiModuleRouteOwner(id));
                    builder.Metadata.Add(new GXModuleEndpointGeneration(generation, p.Value.AuthenticatorType != null));
                    return (Endpoint)builder.Build();
                }
                return new Endpoint(dispatch, new EndpointMetadataCollection(pair.Value.Metadata.Concat([new AmiModuleRouteOwner(id), new GXModuleEndpointGeneration(generation, p.Value.AuthenticatorType != null)])), pair.Value.DisplayName);
            }))];
        }
    }
    /// <summary>Performs the dispatch owned endpoint operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="generation">The generation.</param>
    /// <param name="key">The key.</param>
    /// <param name="http">The http.</param>
    public async Task DispatchOwnedEndpointAsync(string id, string generation, string key, HttpContext http)
    {
        if (!await GXModulePolicyAccess.AuthorizeRequestAsync(http, id))
        {
            return;
        }
        bool dispatched = false;
        try
        {
            await CallAsync(id, async (entry, context, ct) =>
            {
                Endpoint? endpoint;
                lock (_sync)
                {
                    entry.OwnedEndpoints.TryGetValue(key, out endpoint);
                }

                if (endpoint?.RequestDelegate == null)
                {
                    http.Response.StatusCode = 404;
                    return false;
                }
                dispatched = true;
                var services = http.RequestServices;
                var aborted = http.RequestAborted;
                http.RequestServices = context.Services;
                http.RequestAborted = ct;
                try
                {
                    await endpoint.RequestDelegate(http);
                }
                finally
                {
                    http.RequestServices = services;
                    http.RequestAborted = aborted;
                }
                return true;
            }, http.RequestAborted, generation);
        }
        catch (LeaseUnavailableException) when (!dispatched)
        {
            http.Response.StatusCode = 404;
        }
    }
    /// <summary>Acquires an execution lease on the active module generation and increments its in-flight call count.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="generation">Optional expected generation identity used to reject stale dispatch.</param>
    /// <returns>The leased active module entry.</returns>
    /// <exception cref="AmiModuleUnavailableException">The requested module generation is unavailable or inactive.</exception>
    private Entry Acquire(string id, string? generation = null)
    {
        lock (_sync)
        {
            if (!_entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active
                || (generation != null && entry.Generation != generation))
            {
                throw new LeaseUnavailableException($"Module {id} is not active.");
            }

            ++entry.Calls;
            return entry;
        }
    }
    /// <summary>Releases an execution lease and signals drainage when the last call leaves a stopping module.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    private void Release(Entry entry)
    {
        lock (_sync)
        {
            if (--entry.Calls == 0 && entry.State == AmiModuleState.Stopping)
            {
                entry.Drained.TrySetResult();
            }
        }
    }

    /// <summary>Runs a module operation in isolated host and module scopes with linked cancellation and generation-bound lifetime tracking.</summary>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="operation">Operation receiving the leased generation, scoped module context, and linked cancellation token.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="generation">Optional expected generation identity used to reject stale dispatch.</param>
    /// <param name="sanitizeFault">Whether module faults are replaced with a safe public failure at the call boundary.</param>
    /// <returns>The result produced by the module operation.</returns>
    private async Task<TResult> CallAsync<TResult>(string id, Func<Entry, AmiModuleContext, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken,
        string? generation = null, bool sanitizeFault = false)
    {
        var entry = Acquire(id, generation);
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, entry.Lifetime.Token);
            await using var hostScope = host.CreateAsyncScope();
            await using var scope = entry.Provider!.CreateAsyncScope();
            var previous = _current.Value;
            _current.Value = new(hostScope.ServiceProvider, linked.Token, entry, cancellationToken, previous);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                var context = scope.ServiceProvider.GetRequiredService<AmiModuleContext>();
                return await operation(entry, context, linked.Token);
            }
            finally
            {
                _current.Value = previous;
            }
        }
        catch (AmiModuleDownstreamException error)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException!).Throw();
            throw;
        }
        catch (DownstreamFailure error)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException!).Throw();
            throw;
        }
        catch (Exception error) when (IsModuleFault(error) && !cancellationToken.IsCancellationRequested)
        {
            // Authentication scopes can also fail during async disposal. Never log
            // provider exception details from any part of that invocation.
            await FaultAsync(id, entry, sanitizeFault
                ? new InvalidOperationException("Module authentication failed.") : error);
            throw;
        }
        finally
        {
            Release(entry);
        }
    }

    /// <inheritdoc/>
    public bool IsAvailable<TService>() where TService : class
        => Volatile.Read(ref _availableServices).Contains(typeof(TService));

    /// <summary>Performs the invoke operation.</summary>
    /// <param name="operation">The operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="TService">The type used by the operation.</typeparam>
    public Task InvokeAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : class
        => InvokeAsync<TService, bool>(async (service, ct) =>
        {
            await operation(service, ct);
            return true;
        }, cancellationToken);
    /// <summary>Performs the invoke operation.</summary>
    /// <param name="operation">The operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="TService">The type used by the operation.</typeparam>
    /// <typeparam name="TResult">The type used by the operation.</typeparam>
    public Task<TResult> InvokeAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class
    {
        string id;
        string generation;
        lock (_sync)
        {
            id = _exports.GetValueOrDefault(typeof(TService)) ?? throw new AmiModuleUnavailableException($"No active provider for {typeof(TService).FullName}.");
            generation = _entries[id].Generation;
        }
        return CallAsync(id, (_, context, ct) => operation(context.Services.GetRequiredService<TService>(), ct), cancellationToken, generation);
    }

    /// <summary>Invokes an available provider; false means callback never started.</summary>
    public async Task<bool> TryInvokeAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : class
    {
        var result = await TryInvokeAsync<TService, bool>(async (service, token) =>
        {
            await operation(service, token);
            return true;
        }, cancellationToken);
        return result.Invoked;
    }
    /// <summary>Invokes an available provider with an optional result.</summary>
    public async Task<AmiModuleInvocationResult<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class
    {
        ArgumentNullException.ThrowIfNull(operation);
        string id;
        string generation;
        lock (_sync)
        {
            if (!_exports.TryGetValue(typeof(TService), out id!) || !_entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active || entry.UninstallRequested)
            {
                return new(false, default);
            }
            generation = entry.Generation;
        }
        bool started = false;
        try
        {
            var result = await CallAsync(id, (_, context, token) =>
            {
                var service = context.Services.GetRequiredService<TService>();
                started = true;
                return operation(service, token);
            }, cancellationToken, generation);
            return new(true, result);
        }
        catch (LeaseUnavailableException) when (!started)
        {
            return new(false, default);
        }
        catch (OperationCanceledException) when (!started && !cancellationToken.IsCancellationRequested && !IsCurrentProviderActive(id, generation))
        {
            // Stop can cancel a prepared scope after acquisition but before callback admission.
            return new(false, default);
        }
    }

    /// <summary>Invokes with independent cancellation signals.</summary>
    public Task InvokeWithContextAsync<TService>(Func<TService, AmiModuleInvocationContext, Task> operation, CancellationToken cancellationToken = default) where TService : class
        => InvokeWithContextAsync<TService, bool>(async (service, context) =>
        {
            await operation(service, context);
            return true;
        }, cancellationToken);
    /// <summary>Invokes with independent cancellation signals and returns a result.</summary>
    public Task<TResult> InvokeWithContextAsync<TService, TResult>(Func<TService, AmiModuleInvocationContext, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class
        {
        string id;
        string generation;
        lock (_sync)
        {
            id = _exports.GetValueOrDefault(typeof(TService)) ?? throw new AmiModuleUnavailableException($"No active provider for {typeof(TService).FullName}.");
            generation = _entries[id].Generation;
        }
        return CallAsync(id, (_, context, _) => operation(context.Services.GetRequiredService<TService>(), new(context.RequestCancellationToken, context.StoppingToken, context.ForcedCancellationToken)), cancellationToken, generation);
    }

    /// <inheritdoc />
    public async Task<bool> TryInvokeWithContextAsync<TService>(Func<TService, AmiModuleInvocationContext, Task> operation, CancellationToken cancellationToken = default) where TService : class
    {
        ArgumentNullException.ThrowIfNull(operation);
        string id;
        string generation;
        lock (_sync)
        {
            if (!_exports.TryGetValue(typeof(TService), out id!) || !_entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active || entry.UninstallRequested)
            {
                return false;
            }
            generation = entry.Generation;
        }
        bool started = false;
        try
        {
            await CallAsync(id, async (_, context, _) =>
            {
                var service = context.Services.GetRequiredService<TService>();
                started = true;
                await operation(service, new(context.RequestCancellationToken, context.StoppingToken, context.ForcedCancellationToken));
                return true;
            }, cancellationToken, generation);
            return true;
        }
        catch (LeaseUnavailableException) when (!started)
        {
            return false;
        }
        catch (OperationCanceledException) when (!started && !cancellationToken.IsCancellationRequested && !IsCurrentProviderActive(id, generation))
        {
            return false;
        }
    }

    /// <summary>Checks whether the named generation remains active and is not awaiting uninstallation.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="generation">Optional expected generation identity used to reject stale dispatch.</param>
    /// <returns>True when that exact generation is still available for new calls.</returns>
    private bool IsCurrentProviderActive(string id, string generation)
    {
        lock (_sync)
        {
            return _entries.TryGetValue(id, out var entry) && entry.Generation == generation &&
                entry.State == AmiModuleState.Active && !entry.UninstallRequested;
        }
    }

    /// <summary>Performs the dispatch endpoint operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="index">The index.</param>
    /// <param name="context">The context.</param>
    /// <param name="generation">The generation.</param>
    public async Task DispatchEndpointAsync(string id, int index, HttpContext context, string? generation = null)
    {
        bool dispatched = false;
        try
        {
            await CallAsync(id, async (entry, scope, ct) =>
            {
                dispatched = true;
                // Check the leased generation's declaration, never caller-supplied endpoint metadata.
                if (!GXModuleAnonymousAccess.IsAllowed(configuration, id, entry.Endpoints[index], context.Request.Method))
                {
                    if (context.User.Identity?.IsAuthenticated != true)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return false;
                    }
                    if (!await GXModulePolicyAccess.AuthorizeRequestAsync(context, id))
                    {
                        return false;
                    }
                }
                await entry.Endpoints[index].Handler(new(context, scope), ct);
                return true;
            }, context.RequestAborted, generation);
        }
        catch (LeaseUnavailableException) when (!dispatched)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }

    /// <summary>Performs the invoke middleware operation.</summary>
    /// <param name="context">The context.</param>
    /// <param name="next">The next.</param>
    public async Task InvokeMiddlewareAsync(HttpContext context, RequestDelegate next)
    {
        (string Id, string Generation, int Index, int Order)[] snapshot;
        lock (_sync)
        {
            snapshot = [.. _entries.Where(p => p.Value.State == AmiModuleState.Active)
        .SelectMany(p => p.Value.Middleware.Select((m, i) => (p.Key, p.Value.Generation, i, m.Order))).OrderBy(m => m.Order).ThenBy(m => m.Key, StringComparer.Ordinal)];
        }

        async Task Invoke(int position, HttpContext http)
        {
            if (position == snapshot.Length)
            {
                await next(http);
                return;
            }
            var item = snapshot[position];
            bool dispatched = false;
            bool nextCalled = false;
            try
            {
                await CallAsync(item.Id, async (entry, scope, ct) =>
                {
                    dispatched = true;
                    var aborted = http.RequestAborted;
                    http.RequestAborted = ct;
                    try
                    {
                        await entry.Middleware[item.Index].Handler(new(http, scope), async h =>
                    {
                        nextCalled = true;
                        try
                        {
                            await Invoke(position + 1, h);
                        }
                        catch (Exception error)
                        {
                            throw new DownstreamFailure(error);
                        }
                    }, ct);
                    }
                    finally
                    {
                        http.RequestAborted = aborted;
                    }
                    return true;
                }, http.RequestAborted, item.Generation);
            }
            catch (LeaseUnavailableException) when (!dispatched)
            {
                await Invoke(position + 1, http);
            }
            catch (Exception error) when (dispatched && !nextCalled && !http.Response.HasStarted && IsModuleFault(error))
            {
                await Invoke(position + 1, http);
            }
        }
        await Invoke(0, context);
    }

    /// <summary>Performs the execute operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="instanceSettings">The instance settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task ExecuteAsync(string id, string? instanceSettings, CancellationToken cancellationToken = default)
        => CallAsync(id, async (entry, context, ct) =>
        {
            await entry.Module!.ExecuteAsync(context, instanceSettings, ct);
            return true;
        }, cancellationToken);
    /// <summary>Performs the remove operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <param name="options">The options.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    public async Task<AmiModuleState> RemoveAsync(string id, CancellationToken cancellationToken = default, AmiModuleUninstallOptions? options = null)
    {
        options ??= new();
        options.Validate();
        Entry? entry;
        lock (_sync)
        {
            _dependencyGraph.EnsureCanStop(id);
            entry = _entries.GetValueOrDefault(id);
            if (entry?.State == AmiModuleState.Loading)
            {
                throw new InvalidOperationException("Module is still loading; wait before removing it.");
            }

            if (entry?.UninstallRequested == true || entry?.State == AmiModuleState.Stopping)
            {
                throw new InvalidOperationException("Module removal is already pending.");
            }
        }
        if (entry != null)
        {
            using var validationScope = host.CreateScope();
            var database = validationScope.ServiceProvider.GetService<IGXDatabase>();
            if (database != null)
            {
                await new GXModuleDataCleanup(database).ValidateAsync(id, options, cancellationToken);
            }

            lock (_sync)
            {
                _dependencyGraph.EnsureCanStop(id);
                if (!ReferenceEquals(_entries.GetValueOrDefault(id), entry))
                {
                    throw new InvalidOperationException("Module changed while removal was being prepared.");
                }

                if (entry.State == AmiModuleState.Stopping || entry.UninstallRequested)
                {
                    throw new InvalidOperationException("Module removal is already pending.");
                }

                entry.UninstallOptions = options;
                entry.UninstallRequested = true;
                RefreshServiceAvailability();
            }
        }
        var state = await StopAsync(id, cancellationToken: cancellationToken);
        if (entry?.CleanupError != null)
        {
            throw new InvalidOperationException("Module uninstall failed: " + entry.CleanupError);
        }

        return state;
    }
    /// <summary>Performs the update settings operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task UpdateSettingsAsync(string id, string? settings, CancellationToken cancellationToken = default)
    {
        // Reject invalid edits before entering module code: bad configuration must not fault a running module.
        var prepared = PrepareSettings(id, settings);
        return CallAsync(id, async (entry, context, ct) =>
        {
            string? old = entry.Settings.Value;
            entry.Settings.SetValidatedValue(prepared.Value);
            try
            {
                await entry.Module!.SettingsChangedAsync(context, ct);
            }
            catch
            {
                entry.Settings.SetValidatedValue(old);
                throw;
            }
            return true;
        }, cancellationToken, generation: prepared.Generation);
    }
    /// <summary>Performs the read asset operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="contentId">The content id.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task<byte[]> ReadAssetAsync(string id, string contentId, string fileName, CancellationToken cancellationToken = default)
        => CallAsync(id, (entry, _, ct) =>
        {
            if (contentId != entry.Manifest.ContentId || !entry.Manifest.Assemblies.Contains(fileName, StringComparer.Ordinal)
                || System.IO.Path.GetFileName(fileName) != fileName)
            {
                throw new AmiModuleUnavailableException("Module asset is unavailable.");
            }

            return File.ReadAllBytesAsync(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(entry.Path)!, fileName), ct);
        }, cancellationToken);

    /// <summary>Performs the stop operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="waitTimeout">The wait timeout.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<AmiModuleState> StopAsync(string id, TimeSpan? waitTimeout = null, CancellationToken cancellationToken = default)
    {
        Entry? entry;
        lock (_sync)
        {
            _dependencyGraph.EnsureCanStop(id);
            if (!_entries.TryGetValue(id, out entry))
            {
                return _manifests.GetValueOrDefault(id)?.State ?? AmiModuleState.Stopped;
            }

            entry.StopRequestedAt ??= DateTimeOffset.UtcNow;
                    entry.State = AmiModuleState.Stopping;
            entry.OwnedEndpoints.Clear();
            entry.Sources = [];
            _manifests[id] = entry.Manifest = entry.Manifest with
            {
                State = AmiModuleState.Stopping
            };
            foreach (var contract in entry.Exports)
            {
                if (_exports.GetValueOrDefault(contract) == id)
                {
                    _exports.Remove(contract);
                }
            }

            if (entry.Calls == 0)
            {
                entry.Drained.TrySetResult();
            }
        }
        RefreshServiceAvailability();
        Cancel(entry, id);
        NotifyChanged();
        var timeout = waitTimeout ?? TimeSpan.FromSeconds(configuration.GetValue("Modules:StopTimeoutSeconds", 30));
        Task cleanup;
        lock (_sync)
        {
            if (!_entries.TryGetValue(id, out var actual) || !ReferenceEquals(actual, entry))
            {
                return _manifests.GetValueOrDefault(id)?.State ?? AmiModuleState.Stopped;
            }

            cleanup = entry.Cleanup ??= ScheduleCleanup(id, entry);
            entry.ForcedSignal ??= SignalForcedCancellationAsync(entry, cleanup, timeout, id);
        }
        for (var invocation = _current.Value; invocation != null; invocation = invocation.Parent)
        {
            if (ReferenceEquals(invocation.Entry, entry))
            {
                return AmiModuleState.Stopping;
            }
        }
        try
        {
            await cleanup.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            SignalForcedCancellation(entry, id);
            logger.LogWarning("Module {Id} has pending calls/cleanup and remains stopping.", id);
            return AmiModuleState.Stopping;
        }
        return GetManifest(id)?.State ?? AmiModuleState.Stopped;
    }

    /// <summary>Waits for graceful cleanup and requests forced cancellation if the configured timeout expires.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <param name="cleanup">Cleanup task whose graceful completion is awaited before forced cancellation.</param>
    /// <param name="timeout">Grace period allowed before forced cancellation is requested.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <returns>A task that completes when cleanup finishes or forced cancellation has been requested.</returns>
    private async Task SignalForcedCancellationAsync(Entry entry, Task cleanup, TimeSpan timeout, string id)
    {
        try
        {
            await cleanup.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            SignalForcedCancellation(entry, id);
        }
    }
    /// <summary>Marks shutdown as forced and signals the generation's forced-cancellation source while isolating callback failures.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    private void SignalForcedCancellation(Entry entry, string id)
    {
        try
        {
            lock (_sync)
            {
                entry.ForcedCancellation = true;
            }
            entry.Forced.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception error)
        {
            logger.LogError("Module {Id} forced cancellation callback failed: {Message}", id, error.Message);
        }
    }

    /// <summary>Schedules generation cleanup outside the caller's execution context without allowing caller cancellation to abandon ownership release.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <returns>The scheduled cleanup task.</returns>
    private Task ScheduleCleanup(string id, Entry entry)
    {
        async Task Complete()
        {
            var gate = _gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
            // Caller cancellation only stops waiting; it cannot abandon ownership cleanup.
            await gate.WaitAsync(CancellationToken.None);
            try
            {
                lock (_sync)
                {
                    if (!_entries.TryGetValue(id, out var currentEntry) || !ReferenceEquals(currentEntry, entry))
                    {
                        return;
                    }
                }
            }
            finally
            {
                gate.Release();
            }
            await CleanupAsync(id, entry, false);
        }
        if (ExecutionContext.IsFlowSuppressed())
        {
            return Task.Run(Complete);
        }

        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(Complete);
        }
    }

    /// <summary>Drains module calls, stops background work, performs eligible uninstall cleanup, and releases generation-owned resources.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <param name="failed">Whether cleanup already follows a failure rather than a successful shutdown.</param>
    /// <returns>A task that completes after all attempted cleanup and final lifecycle publication.</returns>
    private async Task CleanupAsync(string id, Entry entry, bool failed)
    {
        async Task Attempt(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                failed = true;
                entry.CleanupError ??= exception.Message;
                logger.LogError("Module {Id} cleanup operation failed: {Message}", id, exception.Message);
            }
        }
        // Request background shutdown before draining calls; one failing service cannot skip other cleanup.
        await Task.WhenAll(entry.Drained.Task,
            Task.WhenAll(entry.Background.Select(service => Attempt(() => service.StopAsync(CancellationToken.None)))));
        if (entry.ScopesCreated && entry.Module != null && entry.Started)
        {
            await Attempt(() => entry.Module.StopAsync(entry.LifecycleScope.ServiceProvider.GetRequiredService<AmiModuleContext>(), CancellationToken.None));
        }

        if (entry.ScopesCreated && entry.Module != null && entry.UninstallRequested && !failed)
        {
            await Attempt(async () =>
            {
                var context = entry.LifecycleScope.ServiceProvider.GetRequiredService<AmiModuleContext>();
                context.UninstallOptions = entry.UninstallOptions;
                var database = context.HostServices.GetService<IGXDatabase>();
                if (database != null)
                {
                    await new GXModuleDataCleanup(database).ValidateAsync(id, entry.UninstallOptions, CancellationToken.None);
                }

                await entry.Module.UninstallAsync(context, CancellationToken.None);
                if (database != null)
                {
                    await new GXModuleDataCleanup(database).ExecuteAsync(id, entry.UninstallOptions, CancellationToken.None);
                }
            });
        }
        foreach (var resource in entry.Owned)
        {
            await Attempt(() =>
            {
                resource.Dispose();
                return Task.CompletedTask;
            });
        }

        if (entry.ScopesCreated)
        {
            await Attempt(() => entry.LifecycleScope.DisposeAsync().AsTask());
            await Attempt(() => entry.HostScope.DisposeAsync().AsTask());
        }
        if (entry.Provider != null)
        {
            await Attempt(() => entry.Provider.DisposeAsync().AsTask());
        }

        if (entry.Module is IAsyncDisposable asyncDisposable)
        {
            await Attempt(() => asyncDisposable.DisposeAsync().AsTask());
        }
        else if (entry.Module is IDisposable disposable)
        {
            await Attempt(() =>
            {
                disposable.Dispose();
                return Task.CompletedTask;
            });
        }

        entry.OwnedEndpoints.Clear();
        entry.Sources = [];
        entry.Module = null;
        entry.Provider = null;
        entry.Endpoints = [];
        entry.Middleware = [];
        entry.Exports = [];
        entry.AuthenticatorType = null;
        lock (_sync)
        {
            entry.BackgroundHealth = SnapshotBackground(entry);
            entry.Background = [];
        }
        entry.LifecycleScope = default;
        entry.HostScope = default;
        entry.Owned.Clear();
        entry.Settings = new AmiModuleSettings();
        entry.Json = new JsonSerializerOptions();
        entry.Lifetime.Dispose();
        entry.Forced.Dispose();
        var unload = entry.Unload;
        entry.Unload = null;
        if (unload != null)
        {
            await Attempt(() => unload().AsTask());
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(id, out var actual) || ReferenceEquals(actual, entry))
            {
                entry.StoppedAt = DateTimeOffset.UtcNow;
                _completedHealth[id] = SnapshotHealth(entry, failed || entry.Faulted ? AmiModuleState.Failed : AmiModuleState.Stopped);
                _entries.Remove(id);
                _dependencyGraph.Release(id);
                _manifests[id] = entry.Manifest = entry.Manifest with
                {
                    State = failed || entry.Faulted ? AmiModuleState.Failed : AmiModuleState.Stopped
                };
            }
        }
        entry.Cleaned = true;
        NotifyChanged();
    }

    /// <summary>Cancels the module generation lifetime and logs cancellation callback failures without interrupting shutdown.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    private void Cancel(Entry entry, string id)
    {
        try
        {
            entry.Lifetime.Cancel();
        }
        catch (Exception exception)
        {
            logger.LogError("Module {Id} cancellation callback failed: {Message}", id, exception.Message);
        }
    }

    /// <summary>Refreshes available services and notifies every availability subscriber while isolating subscriber failures.</summary>
    private void NotifyChanged()
    {
        RefreshServiceAvailability();
        foreach (Action subscriber in (Changed?.GetInvocationList() ?? []).Cast<Action>())
        {
            try
            {
                subscriber();
            }
            catch (Exception ex)
            {
                logger.LogError("Module availability notification failed: {Message}", ex.Message);
            }
        }
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _ = _entries.Keys.ToArray();
        }
        await StopAllAsync();
        await DisposeOwnedEventsAsync();
    }

    /// <summary>Matches supported module identity and version path segments.</summary>
    /// <returns>The generated regular expression accepting alphanumeric identifiers with dots, underscores, and hyphens.</returns>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}









