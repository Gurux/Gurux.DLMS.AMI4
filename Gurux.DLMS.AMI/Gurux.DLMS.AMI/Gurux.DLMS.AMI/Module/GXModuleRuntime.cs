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

/// <summary>Provides ami published endpoint.</summary>
/// <param name="ModuleId">The module id.</param>
/// <param name="Generation">The generation.</param>
/// <param name="Index">The index.</param>
/// <param name="Pattern">The pattern.</param>
/// <param name="Methods">The methods.</param>
/// <param name="Policies">The policies.</param>
public sealed record AmiPublishedEndpoint(string ModuleId, string Generation, int Index, string Pattern, string[] Methods, string[] Policies)
{
    /// <summary>Whether this endpoint generation uses module authentication.</summary>
    public bool HasAuthenticator { get; init; }
    /// <summary>Whether every declared HTTP method is explicitly allowed anonymously by the host.</summary>
    public bool IsAnonymous { get; init; }
}

/// <summary>Owns module services and registrations; all module operations hold a cancellable lease.</summary>
public sealed partial class GXModuleRuntime(IServiceProvider host, IConfiguration configuration, ILogger<GXModuleRuntime> logger)
    : IGXAmiModuleServices, IGXModuleAuthenticationRuntime, IAsyncDisposable
{
    private readonly Lock sync = new();
    private readonly GXModuleDependencyGraph dependencyGraph = new();
    private readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AmiModuleManifest> manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, string> exports = [];
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly AsyncLocal<Invocation?> current = new();
    private bool disposed;
    private bool shuttingDown;
    private readonly Dictionary<string, TaskCompletionSource> pendingInstallations = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Performs the  operation.</summary>
    public event Action? Changed;
    /// <summary>Performs the  operation.</summary>
    public event Func<AmiModuleManifest, Task>? Faulted;

    private sealed record Invocation(IServiceProvider Host, CancellationToken Token, Entry Entry, CancellationToken RequestToken, Invocation? Parent);
    private sealed class LeaseUnavailableException(string message) : AmiModuleUnavailableException(message);
    private sealed class Entry(IGXAmiModule module, string path, AmiModuleSettings settings, AmiModuleManifest manifest, Func<ValueTask>? unload)
    {
        public IGXAmiModule? Module = module;
        public readonly string Generation = Guid.NewGuid().ToString("N");
        public bool Started;
        public DateTimeOffset? StartedAt;
        public DateTimeOffset? StopRequestedAt;
        public DateTimeOffset? StoppedAt;
        public bool ForcedCancellation;
        public Gurux.DLMS.AMI.Module.Health.AmiModuleBackgroundHealth[] BackgroundHealth = [];
        public bool UninstallRequested;
        public AmiModuleUninstallOptions UninstallOptions = new();
        public string? CleanupError;
        public bool Cleaned;
        public bool Faulted;
        public ServiceProvider? Provider;
        public string Path = path;
        public AmiModuleSettings Settings = settings;
        public JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        public AmiModuleManifest Manifest = manifest;
        public AmiModuleState State = AmiModuleState.Loading;
        public CancellationTokenSource Lifetime = new();
        public CancellationTokenSource Forced = new();
        public TaskCompletionSource Drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        public AmiEndpointDefinition[] Endpoints = [];
        public Dictionary<string, Endpoint> OwnedEndpoints = [];
        public readonly object SourceSync = new();
        public EndpointDataSource[] Sources = [];
        public AmiMiddlewareDefinition[] Middleware = [];
        public Type[] Exports = [];
        public Type? AuthenticatorType;
        public IHostedService[] Background = [];
        public ConcurrentBag<IDisposable> Owned = [];
        public AsyncServiceScope LifecycleScope;
        public AsyncServiceScope HostScope;
        public bool ScopesCreated;
        public Task? Cleanup;
        public Task? ForcedSignal;
        public Func<ValueTask>? Unload = unload;
    }

    private Dictionary<string, AmiModuleManifest> ActiveProviders() => entries.Where(e => e.Value.State == AmiModuleState.Active && !e.Value.UninstallRequested).ToDictionary(e => e.Key, e => e.Value.Manifest, StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> ServiceProviders() => exports.ToDictionary(e => e.Key.FullName!, e => e.Value, StringComparer.Ordinal);
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
    /// <summary>Performs the get startup providers operation.</summary>
    public IReadOnlyList<GXModuleStartupCandidate> GetStartupProviders()
    {
        lock (sync)
        {
            return [.. entries.Values.Where(e => e.State == AmiModuleState.Active && !e.UninstallRequested).Select(e => new GXModuleStartupCandidate(e.Manifest.Id, e.Manifest.CompatibilityVersion, e.Manifest.Dependencies, [.. e.Exports.Select(t => t.FullName!)]))];
        }
    }
    /// <summary>Performs the validate candidate dependencies operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="dependencies">The dependencies.</param>
    public void ValidateCandidateDependencies(string id, IEnumerable<AmiModuleDependency> dependencies)
    {
        var values = GXModuleDependencyValidator.ValidateDeclarations(id, dependencies);
        GXModuleDependencyValidator.ValidateHostDependencies(values);
        lock (sync)
        {
            GXModuleDependencyGraph.Resolve(id, values, ActiveProviders(), ServiceProviders());
        }
    }
    private sealed class CandidateReservation(Action release) : IDisposable
    {
        private Action? action = release; public void Dispose() => Interlocked.Exchange(ref action, null)?.Invoke();
    }
    /// <summary>Performs the reserve candidate dependencies operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="declarations">The declarations.</param>
    public IDisposable ReserveCandidateDependencies(string id, IEnumerable<AmiModuleDependency> declarations)
    {
        var dependencies = GXModuleDependencyValidator.ValidateDeclarations(id, declarations);
        GXModuleDependencyValidator.ValidateHostDependencies(dependencies);
        string key = id + " (installation)";
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed || shuttingDown, this);
            var suppliers = GXModuleDependencyGraph.Resolve(id, dependencies, ActiveProviders(), ServiceProviders());
            dependencyGraph.Reserve(key, suppliers);
            pendingInstallations.Add(key, new(TaskCreationOptions.RunContinuationsAsynchronously));
        }
        return new CandidateReservation(() =>
        {
            lock (sync)
            {
                dependencyGraph.Release(key);
                if (pendingInstallations.Remove(key, out var pending))
                {
                    pending.TrySetResult();
                }
            }
        });
    }
    /// <summary>Performs the get removal order operation.</summary>
    /// <param name="ids">The ids.</param>
    public string[] GetRemovalOrder(IEnumerable<string> ids)
    {
        lock (sync)
        {
            return dependencyGraph.GetStopOrder(ids);
        }
    }
    /// <summary>Performs the stop all operation.</summary>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task StopAllAsync(CancellationToken token = default)
    {
        Task[] installations;
        lock (sync)
        {
            shuttingDown = true;
            installations = [.. pendingInstallations.Values.Select(p => p.Task)];
        }
        if (installations.Length != 0)
        {
            await Task.WhenAll(installations).WaitAsync(token);
        }

        string[] ids;
        lock (sync)
        {
            ids = dependencyGraph.GetStopOrder(entries.Keys);
        }

        foreach (var id in ids)
        {
            await StopAsync(id, cancellationToken: token);
            Task? pending;
            lock (sync)
            {
                pending = entries.GetValueOrDefault(id)?.Cleanup;
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
        var gate = gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken);
        Entry? entry = null;
        try
        {
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed || shuttingDown, this);
                if (entries.ContainsKey(id))
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
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed || shuttingDown, this);
                var resolved = start ? GXModuleDependencyGraph.Resolve(id, dependencies, ActiveProviders(), ServiceProviders()) : [];
                dependencyGraph.Reserve(id, resolved);
                entry.Manifest = manifest = manifest with
                {
                    ResolvedDependencyModuleIds = resolved
                };
                entries.Add(id, entry);
                manifests[id] = manifest;
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
            lock (sync)
            {
                foreach (var contract in entry.Exports)
                {
                    if (exports.Keys.Any(existing => existing.FullName == contract.FullName))
                    {
                        throw new InvalidOperationException($"An active/loading module already exports {contract.FullName}.");
                    }
                }

                foreach (var contract in entry.Exports)
                {
                    exports.Add(contract, id);
                }

                manifests[id] = manifest;
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
                HostServices = current.Value?.Host ?? host,
                Settings = owned.Settings,
                Resources = new GXModuleResources(id, current.Value?.Host ?? host),
                UserSettings = new GXModuleUserSettings(id, current.Value?.Host ?? host),
                UninstallOptions = owned.UninstallOptions,
                CancellationToken = current.Value?.Token ?? owned.Lifetime.Token,
                RequestCancellationToken = current.Value?.RequestToken ?? default,
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
            var previous = current.Value;
            current.Value = new(entry.HostScope.ServiceProvider, entry.Lifetime.Token, entry, cancellationToken, previous);
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
                current.Value = previous;
            }
            if (!start)
            {
                lock (sync)
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
            lock (sync)
            {
                if (entry.State == AmiModuleState.Stopping)
                {
                    throw new OperationCanceledException("Module was stopped while loading.");
                }

                entry.StartedAt = DateTimeOffset.UtcNow;
                entry.State = AmiModuleState.Active;
                manifests[id] = entry.Manifest = manifest with
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
                lock (sync)
                {
                    entry.StopRequestedAt ??= DateTimeOffset.UtcNow;
                    entry.State = AmiModuleState.Stopping;
                    entry.Drained.TrySetResult();
                    foreach (var contract in entry.Exports)
                    {
                        if (exports.GetValueOrDefault(contract) == id)
                        {
                            exports.Remove(contract);
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
        lock (sync)
        {
            return [.. manifests.Values.Where(m => !activeOnly || m.State == AmiModuleState.Active).OrderBy(m => m.Id)];
        }
    }
    /// <summary>Performs the get manifest operation.</summary>
    /// <param name="id">The item identifier.</param>
    public AmiModuleManifest? GetManifest(string id)
    {
        lock (sync)
        {
            return manifests.GetValueOrDefault(id);
        }
    }
    /// <summary>Reads this host's current lifecycle state, including retained stopped and failed records.</summary>
    public AmiModuleState? GetState(string id)
    {
        lock (sync)
        {
            return manifests.GetValueOrDefault(id)?.State;
        }
    }

    /// <summary>Updates the persisted deployment choice included in runtime snapshots.</summary>
    public void SetEnabled(string id, bool enabled)
    {
        lock (sync)
        {
            if (manifests.TryGetValue(id, out var manifest))
            {
                manifests[id] = manifest with { Enabled = enabled };
            }
        }
        NotifyChanged();
    }
    /// <summary>Performs the get endpoints operation.</summary>
    public IReadOnlyList<AmiPublishedEndpoint> GetEndpoints()
    {
        lock (sync)
        {
            return [.. entries.Where(p => p.Value.State == AmiModuleState.Active).SelectMany(p => p.Value.Endpoints.Select((e, i)
        => new AmiPublishedEndpoint(p.Key, p.Value.Generation, i, "/api/modules/" + p.Key + "/" + e.Pattern.Trim('/'), [.. e.Methods], [.. e.Policies])
        {
            HasAuthenticator = p.Value.AuthenticatorType != null,
            IsAnonymous = e.Methods.Length != 0 && e.Methods.All(method => GXModuleAnonymousAccess.IsAllowed(configuration, p.Key, e, method))
        }))];
        }
    }

    private void RefreshOwnedEndpoints(string id, Entry entry)
    {
        lock (entry.SourceSync)
        {
            EndpointDataSource[] sources;
            lock (sync)
            {
                if (entry.State == AmiModuleState.Stopping || !ReferenceEquals(entries.GetValueOrDefault(id), entry))
                {
                    return;
                }

                sources = entry.Sources;
            }
            var snapshot = sources.SelectMany(s => s.Endpoints).ToDictionary(_ => Guid.NewGuid().ToString("N"), e => e);
            bool active;
            lock (sync)
            {
                if (entry.State == AmiModuleState.Stopping || !ReferenceEquals(entries.GetValueOrDefault(id), entry))
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
        lock (sync)
        {
            return [.. entries.Where(p => p.Value.State == AmiModuleState.Active).SelectMany(p => p.Value.OwnedEndpoints.Select(pair =>
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
                lock (sync)
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
    private Entry Acquire(string id, string? generation = null)
    {
        lock (sync)
        {
            if (!entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active
                || (generation != null && entry.Generation != generation))
            {
                throw new LeaseUnavailableException($"Module {id} is not active.");
            }

            ++entry.Calls;
            return entry;
        }
    }
    private void Release(Entry entry)
    {
        lock (sync)
        {
            if (--entry.Calls == 0 && entry.State == AmiModuleState.Stopping)
            {
                entry.Drained.TrySetResult();
            }
        }
    }

    private async Task<TResult> CallAsync<TResult>(string id, Func<Entry, AmiModuleContext, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken,
        string? generation = null, bool sanitizeFault = false)
    {
        var entry = Acquire(id, generation);
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, entry.Lifetime.Token);
            await using var hostScope = host.CreateAsyncScope();
            await using var scope = entry.Provider!.CreateAsyncScope();
            var previous = current.Value;
            current.Value = new(hostScope.ServiceProvider, linked.Token, entry, cancellationToken, previous);
            try
            {
                linked.Token.ThrowIfCancellationRequested();
                var context = scope.ServiceProvider.GetRequiredService<AmiModuleContext>();
                return await operation(entry, context, linked.Token);
            }
            finally
            {
                current.Value = previous;
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
        => Volatile.Read(ref availableServices).Contains(typeof(TService));

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
        lock (sync)
        {
            id = exports.GetValueOrDefault(typeof(TService)) ?? throw new AmiModuleUnavailableException($"No active provider for {typeof(TService).FullName}.");
            generation = entries[id].Generation;
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
        lock (sync)
        {
            if (!exports.TryGetValue(typeof(TService), out id!) || !entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active || entry.UninstallRequested)
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
        lock (sync)
        {
            id = exports.GetValueOrDefault(typeof(TService)) ?? throw new AmiModuleUnavailableException($"No active provider for {typeof(TService).FullName}.");
            generation = entries[id].Generation;
        }
        return CallAsync(id, (_, context, _) => operation(context.Services.GetRequiredService<TService>(), new(context.RequestCancellationToken, context.StoppingToken, context.ForcedCancellationToken)), cancellationToken, generation);
    }

    /// <inheritdoc />
    public async Task<bool> TryInvokeWithContextAsync<TService>(Func<TService, AmiModuleInvocationContext, Task> operation, CancellationToken cancellationToken = default) where TService : class
    {
        ArgumentNullException.ThrowIfNull(operation);
        string id;
        string generation;
        lock (sync)
        {
            if (!exports.TryGetValue(typeof(TService), out id!) || !entries.TryGetValue(id, out var entry) || entry.State != AmiModuleState.Active || entry.UninstallRequested)
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

    private bool IsCurrentProviderActive(string id, string generation)
    {
        lock (sync)
        {
            return entries.TryGetValue(id, out var entry) && entry.Generation == generation &&
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
        lock (sync)
        {
            snapshot = [.. entries.Where(p => p.Value.State == AmiModuleState.Active)
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
        lock (sync)
        {
            dependencyGraph.EnsureCanStop(id);
            entry = entries.GetValueOrDefault(id);
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

            lock (sync)
            {
                dependencyGraph.EnsureCanStop(id);
                if (!ReferenceEquals(entries.GetValueOrDefault(id), entry))
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
        lock (sync)
        {
            dependencyGraph.EnsureCanStop(id);
            if (!entries.TryGetValue(id, out entry))
            {
                return manifests.GetValueOrDefault(id)?.State ?? AmiModuleState.Stopped;
            }

            entry.StopRequestedAt ??= DateTimeOffset.UtcNow;
                    entry.State = AmiModuleState.Stopping;
            entry.OwnedEndpoints.Clear();
            entry.Sources = [];
            manifests[id] = entry.Manifest = entry.Manifest with
            {
                State = AmiModuleState.Stopping
            };
            foreach (var contract in entry.Exports)
            {
                if (exports.GetValueOrDefault(contract) == id)
                {
                    exports.Remove(contract);
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
        lock (sync)
        {
            if (!entries.TryGetValue(id, out var actual) || !ReferenceEquals(actual, entry))
            {
                return manifests.GetValueOrDefault(id)?.State ?? AmiModuleState.Stopped;
            }

            cleanup = entry.Cleanup ??= ScheduleCleanup(id, entry);
            entry.ForcedSignal ??= SignalForcedCancellationAsync(entry, cleanup, timeout, id);
        }
        for (var invocation = current.Value; invocation != null; invocation = invocation.Parent)
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
    private void SignalForcedCancellation(Entry entry, string id)
    {
        try
        {
            lock (sync)
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

    private Task ScheduleCleanup(string id, Entry entry)
    {
        async Task Complete()
        {
            var gate = gates.GetOrAdd(id, _ => new SemaphoreSlim(1));
            // Caller cancellation only stops waiting; it cannot abandon ownership cleanup.
            await gate.WaitAsync(CancellationToken.None);
            try
            {
                lock (sync)
                {
                    if (!entries.TryGetValue(id, out var currentEntry) || !ReferenceEquals(currentEntry, entry))
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
        lock (sync)
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

        lock (sync)
        {
            if (!entries.TryGetValue(id, out var actual) || ReferenceEquals(actual, entry))
            {
                entry.StoppedAt = DateTimeOffset.UtcNow;
                completedHealth[id] = SnapshotHealth(entry, failed || entry.Faulted ? AmiModuleState.Failed : AmiModuleState.Stopped);
                entries.Remove(id);
                dependencyGraph.Release(id);
                manifests[id] = entry.Manifest = entry.Manifest with
                {
                    State = failed || entry.Faulted ? AmiModuleState.Failed : AmiModuleState.Stopped
                };
            }
        }
        entry.Cleaned = true;
        NotifyChanged();
    }

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
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            _ = entries.Keys.ToArray();
        }
        await StopAllAsync();
        await DisposeOwnedEventsAsync();
    }

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}









