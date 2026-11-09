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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Loads module UI descriptors while restricting executable asset loading to active modules.</summary>
public sealed class ModuleLoader : IDisposable
{
    private readonly HttpClient _http;
    private readonly IGXNotifier2? _notifier;
    private readonly IGXProgress? _progress;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<string, AmiModuleManifest> _modules = [];
    private readonly Dictionary<(string Id, string Content), Assembly[]> _assemblies = [];
    private readonly Dictionary<(string Id, string Content, string Descriptor), Type> _components = [];
    // Browser assemblies cannot be unloaded. Retain hashes, never stale component references.
    private readonly Dictionary<string, string> _loadedIdentities = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _failedContent = new(StringComparer.OrdinalIgnoreCase);
    private long _generation;

    public ModuleLoader(HttpClient http, IGXNotifier2? notifier = null, IGXProgress? progress = null)
    {
        _http = http;
        _progress = progress;
        _notifier = notifier;
        _ = PollMetadataAsync(_lifetime.Token);
        notifier?.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), items =>
        {
            if (items.Any(it => it.Type == GXModulePolicies.Add || it.Type == GXModulePolicies.Edit || it.Type == GXModulePolicies.Delete))
            {
                Interlocked.Increment(ref _generation);
                _modules.Clear();
                _assemblies.Clear();
                _components.Clear();
                Changed?.Invoke();
                _ = RefreshFromNotificationAsync();
            }
        });
    }

    public IReadOnlyCollection<AmiModuleManifest> AddIns => [.. _modules.Values];
    public event Action? Changed;
    public Exception? RefreshError { get; private set; }
    public bool ReloadRequired { get; private set; }
    public bool IsInitialized { get; private set; }
    private async Task PollMetadataAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                if (IsInitialized)
                {
                    await RefreshFromNotificationAsync();
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshFromNotificationAsync()
    {
        try
        {
            await RefreshAsync(_lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            RefreshError = ex;
            Changed?.Invoke();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        bool changed = false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var progress = !IsInitialized ? _progress?.ProgressStart("Loading modules...") : null;
            var generation = _generation;
            var manifests = await _http.GetFromJsonAsync<AmiModuleManifest[]>("api/Module/Runtime", cancellationToken) ?? [];
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != _generation)
            {
                return;
            }

            var next = manifests.Where(m => m.State != AmiModuleState.Active ||
                            !_failedContent.TryGetValue(m.Id, out var failed) || failed != m.ContentId).ToDictionary(m => m.Id);
            changed = !IsInitialized || RefreshError != null ||
                System.Text.Json.JsonSerializer.Serialize(_modules.Values.OrderBy(m => m.Id)) != System.Text.Json.JsonSerializer.Serialize(next.Values.OrderBy(m => m.Id));
            foreach (var old in _modules.Values)
            {
                if (!next.TryGetValue(old.Id, out var current) || current.ContentId != old.ContentId || current.Version != old.Version)
                {
                    foreach (var key in _components.Keys.Where(k => k.Id == old.Id).ToArray())
                    {
                        _components.Remove(key);
                    }

                    foreach (var key in _assemblies.Keys.Where(k => k.Id == old.Id).ToArray())
                    {
                        _assemblies.Remove(key);
                    }
                }
            }
            _modules.Clear();
            foreach (var manifest in next.Values)
            {
                _modules.Add(manifest.Id, manifest);
            }

            RefreshError = null;
            IsInitialized = true;
        }
        finally
        {
            _gate.Release();
        }
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public Task<Type?> GetViewTypeAsync(string id, AmiViewArea area, string? key = null, CancellationToken cancellationToken = default)
        => GetTypeAsync(id, m => m.Views.FirstOrDefault(v => v.Area == area &&
            string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase))?.Component, cancellationToken);
    public Task<Type?> GetConfigurationTypeAsync(string id, CancellationToken cancellationToken = default)
        => GetViewTypeAsync(id, AmiViewArea.Configuration, cancellationToken: cancellationToken);
    public Task<Type?> GetExtensionTypeAsync(string id, CancellationToken cancellationToken = default)
        => GetViewTypeAsync(id, AmiViewArea.Extension, cancellationToken: cancellationToken);
    public Task<Type?> GetScheduleTypeAsync(string id, CancellationToken cancellationToken = default)
        => GetViewTypeAsync(id, AmiViewArea.ScheduleSettings, cancellationToken: cancellationToken);
    public Task<Type?> GetPageTypeAsync(string id, string route, CancellationToken cancellationToken = default)
        => GetTypeAsync(id, m => m.Pages.FirstOrDefault(p => p.Route.Equals(route, StringComparison.OrdinalIgnoreCase))?.Component, cancellationToken);

    private async Task<Type?> GetTypeAsync(string id, Func<AmiModuleManifest, string?> select, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_modules.TryGetValue(id, out var manifest) || manifest.State != AmiModuleState.Active)
            {
                return null;
            }

            var descriptor = select(manifest);
            if (string.IsNullOrWhiteSpace(descriptor))
            {
                return null;
            }

            var key = (id, manifest.ContentId, descriptor);
            if (_components.TryGetValue(key, out var cached))
            {
                return cached;
            }

            using var progress = _progress?.ProgressStart("Loading module " + manifest.Name + "...");
            var generation = _generation;
            var assemblyKey = (id, manifest.ContentId);
            if (!_assemblies.TryGetValue(assemblyKey, out var assemblies))
            {
                var downloaded = new List<(byte[] Bytes, string Identity, string Hash)>();
                foreach (var file in manifest.Assemblies)
                {
                    var path = string.Join("/", file.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
                    var bytes = await _http.GetByteArrayAsync($"api/Module/Assets/{Uri.EscapeDataString(id)}/{Uri.EscapeDataString(manifest.ContentId)}/{path}", cancellationToken);
                    using var pe = new PEReader(new MemoryStream(bytes));
                    var metadata = pe.GetMetadataReader();
                    var definition = metadata.GetAssemblyDefinition();
                    var name = new AssemblyName { Name = metadata.GetString(definition.Name), Version = definition.Version, CultureName = metadata.GetString(definition.Culture) };
                    if (!definition.PublicKey.IsNil)
                    {
                        name.SetPublicKey(metadata.GetBlobBytes(definition.PublicKey));
                    }
                    else
                    {
                        name.SetPublicKeyToken([]);
                    }

                    var identity = name.FullName!;
                    var hash = Convert.ToHexString(SHA256.HashData(bytes));
                    if ((_loadedIdentities.TryGetValue(identity, out var previous) && previous != hash) ||
                        (!_loadedIdentities.ContainsKey(identity) && AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == identity)))
                    {
                        ReloadRequired = true;
                        throw new InvalidOperationException("This module assembly changed. Reload the page to load the current module version.");
                    }
                    downloaded.Add((bytes, identity, hash));
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != _generation)
                {
                    return null;
                }

                assemblies = [.. downloaded.Select(item =>
                                {
                                    if (_loadedIdentities.ContainsKey(item.Identity))
                                    {
                                        return AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName == item.Identity);
                                    }

                                    var assembly = Assembly.Load(item.Bytes);
                                    _loadedIdentities[item.Identity] = item.Hash;
                                    return assembly;
                                })];
                _assemblies[assemblyKey] = assemblies;
            }
            var type = assemblies.Select(a => a.GetType(descriptor, false)).FirstOrDefault(t => t != null);
            if (type != null && !typeof(IComponent).IsAssignableFrom(type))
            {
                throw new InvalidOperationException("Module UI descriptor must name a Blazor component.");
            }

            if (type != null)
            {
                _components[key] = type;
            }

            return type;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            DisableClientModule(id, error);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void DisableClientModule(string id, Exception error)
    {
        if (!_modules.Remove(id, out var module))
        {
            return;
        }

        _failedContent[id] = module.ContentId;
        foreach (var key in _assemblies.Keys.Where(k => k.Id == id).ToArray())
        {
            _assemblies.Remove(key);
        }

        foreach (var key in _components.Keys.Where(k => k.Id == id).ToArray())
        {
            _components.Remove(key);
        }

        RefreshError = error;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _notifier?.RemoveListener(this);
        _lifetime.Cancel();
        _lifetime.Dispose();
        _modules.Clear();
        _assemblies.Clear();
        _components.Clear();
    }
}
