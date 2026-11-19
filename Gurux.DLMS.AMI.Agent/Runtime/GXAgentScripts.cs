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

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Threading.Channels;
using System.Text.Json;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Prepares authorized script versions without invoking user code, then supplies isolated ready contexts.</summary>
internal sealed class GXAgentScripts
{
    /// <summary>Protocol or HTTP client used for communication by this component.</summary>
    private readonly HttpClient _client;
    /// <summary>Coordinates script invocation timeouts and abandoned executions.</summary>
    private readonly GXScriptExecution _execution;
    /// <summary>Maximum duration allowed for an individual script invocation.</summary>
    private readonly TimeSpan _executionTimeout;
    /// <summary>Cancellation lifetime shared by this component's work.</summary>
    private readonly CancellationToken _lifetime;

    /// <summary>Initializes script caching and isolated execution using caller-owned HTTP access.</summary>
    /// <param name="client">Configured communication client used by the operation.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="lifetime">Cancellation lifetime shared by owned work.</param>
    internal GXAgentScripts(HttpClient client, GXScriptExecutionOptions? options = null, CancellationToken lifetime = default)
    {
        options ??= new GXScriptExecutionOptions();
        options.Validate();
        this._client = client;
        this._lifetime = lifetime;
        _executionTimeout = options.ExecutionTimeout;
        _execution = new GXScriptExecution(_executionTimeout, lifetime);
    }

    /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
    private readonly Lock _gate = new();
    /// <summary>Serializes publication and invalidation of script versions.</summary>
    private readonly SemaphoreSlim _updates = new(1, 1);
    /// <summary>Most recently prepared script version for each script identifier.</summary>
    private readonly Dictionary<Guid, Version> _latest = [];
    /// <summary>Script identifiers invalidated and unavailable for execution.</summary>
    private readonly HashSet<Guid> _denied = [];
    /// <summary>Associates prepared method instances with their loaded script version.</summary>
    private ConditionalWeakTable<GXScriptMethod, Version> _prepared = [];
    /// <summary>Invalidation generation used to reject stale compilation results.</summary>
    private int _generation;
    /// <summary>Caches a script compilation identity with its compiled assembly image.</summary>
    /// <param name="Key">Content identity used to reuse an existing script compilation.</param>
    /// <param name="Image">Compiled script assembly bytes loaded into isolated contexts.</param>
    private sealed record Compilation(string Key, byte[] Image);
    /// <summary>Compiled script images indexed by script identifier.</summary>
    private readonly Dictionary<Guid, Compilation> _compilations = [];

    /// <summary>Fetches and validates a configured method before its listener/worker is started.</summary>
    /// <param name="methodId">Server identifier of the script method to retrieve.</param>
    /// <param name="parameterTypes">Expected types of the script entry point parameters.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The prepared script method with verified parameter types.</returns>
    internal async Task<GXScriptMethod> LoadAsync(Guid methodId, Type?[] parameterTypes, CancellationToken token = default)
    {
        await _updates.WaitAsync(token);
        try
        {
            int initial;
            lock (_gate)
            {
                initial = _generation;
            }

            var candidate = await DownloadAsync(methodId, parameterTypes, initial, token);
            lock (_gate)
            {
                if (initial != _generation)
                {
                    candidate.Dispose();
                    throw new OperationCanceledException("Script catalog was reset.");
                }

                Publish(candidate);
                return _latest[methodId].Method;
            }
        }
        catch (HttpRequestException error) when (IsAuthorizationError(error))
        {
            lock (_gate)
            {
                _denied.Add(methodId);
            }

            throw;
        }
        finally
        {
            _updates.Release();
        }
    }

    /// <summary>Captures the latest validated snapshot at the beginning of a new connection.</summary>
    /// <param name="original">Previously prepared method whose current authorized version is requested.</param>
    /// <returns>The currently authorized prepared version of the original script method.</returns>
    internal GXScriptMethod? Current(GXScriptMethod? original)
    {
        if (original == null)
        {
            return null;
        }

        lock (_gate)
        {
            if (_denied.Contains(original.Id))
            {
                throw new UnauthorizedAccessException("Script access has been revoked.");
            }

            return _latest.TryGetValue(original.Id, out var version) ? version.Method : original;
        }
    }

    /// <summary>Uses only the explicitly pinned version; no script download or compilation occurs here.</summary>
    /// <param name="method">Script method prepared for isolated execution.</param>
    /// <param name="parameters">Values or receive settings supplied to the operation.</param>
    /// <returns>Arguments retaining the prepared script context for the invocation.</returns>
    internal GXScriptRunArgs CreateRunArgs(GXScriptMethod method, object?[] parameters)
        => CreateRunArgsAsync(method, parameters).GetAwaiter().GetResult();

    /// <summary>Rents an isolated script context and prepares arguments for a single invocation.</summary>
    /// <param name="method">Script method prepared for isolated execution.</param>
    /// <param name="parameters">Values or receive settings supplied to the operation.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Arguments owning a rented isolated script context for this invocation.</returns>
    internal async Task<GXScriptRunArgs> CreateRunArgsAsync(GXScriptMethod method, object?[] parameters,
        CancellationToken token = default)
    {
        Version version;
        lock (_gate)
        {
            if (!_prepared.TryGetValue(method, out version!))
            {
                throw new InvalidOperationException("Script was not prepared before use.");
            }
        }

        return new GXScriptRunArgs
        {
            ByteAssembly = version.Image,
            MethodName = method.Name!,
            Asyncronous = method.IsAsynchronous,
            AssemblyLoadContext = await version.RentAsync(token),
            Parameters = parameters
        };
    }

    /// <summary>Invokes a prepared snapshot with a bounded wait and retains its context until actual completion.</summary>
    /// <param name="runner">Loaded script instance retained for the raw stream.</param>
    /// <param name="method">Script method prepared for isolated execution.</param>
    /// <param name="parameters">Values or receive settings supplied to the operation.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The script's completed return value.</returns>
    internal async Task<object?> RunAsync(GXAmiScript runner, GXScriptMethod method, object?[] parameters,
        CancellationToken token = default)
    {
        _execution.CheckAdmission();
        using var preparation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime, token);
        preparation.CancelAfter(_executionTimeout);
        var args = await CreateRunArgsAsync(method, parameters, preparation.Token);
        try
        {
            return await RunPreparedAsync(runner, args, token);
        }
        finally
        {
            ReleaseRunArgs(args);
        }
    }

    /// <summary>Runs a pinned per-connection context without releasing its parser state between chunks.</summary>
    /// <param name="runner">Loaded script instance retained for the raw stream.</param>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The result returned by the prepared script invocation.</returns>
    internal Task<object?> RunPreparedAsync(GXAmiScript runner, GXScriptRunArgs args, CancellationToken token = default)
        => _execution.RunAsync(args, async cancellation => await AwaitResultAsync(await runner.RunAsync(args, cancellation)), token);

    /// <summary>Defers unloading when cancelled script code still owns this context.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    internal void ReleaseRunArgs(GXScriptRunArgs args) => _execution.Release(args);

    /// <summary>Gets whether capacity remains for another isolated script execution.</summary>
    internal bool CanExecute => _execution.CanExecute;

    /// <summary>Unwraps asynchronous script return values before returning their result.</summary>
    /// <param name="result">Script return value, which may be an asynchronous result.</param>
    /// <returns>The completed asynchronous script result, or the original synchronous return value.</returns>
    internal async Task<object?> AwaitResultAsync(object? result)
    {
        while (true)
        {
            if (result is Task task)
            {
                await task;
                var value = task.GetType().GetProperty("Result");
                result = value == null || value.PropertyType.Name == "VoidTaskResult" ? null : value.GetValue(task);
            }
            else if (result is ValueTask valueTask)
            { await valueTask; return null; }
            else if (result?.GetType() is Type type && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                result = type.GetMethod("AsTask")!.Invoke(result, null);
            }
            else
            {
                return result;
            }
        }
    }

    /// <summary>Prepares all affected candidates first. A bad candidate leaves the current code unchanged.</summary>
    /// <param name="scriptId">Identifier of the script invalidated by the server.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task completing after the changed script cache has been refreshed.</returns>
    internal async Task RefreshAsync(Guid scriptId, CancellationToken token = default)
    {
        await _updates.WaitAsync(token);
        var candidates = new List<Version>();
        Version[] previous = [];
        try
        {
            int initial;
            lock (_gate)
            {
                initial = _generation;
                previous = [.. _latest.Values.Where(v => v.ScriptId == scriptId)];
            }

            foreach (var version in previous)
            {
                candidates.Add(await DownloadAsync(version.Method.Id, version.Parameters, initial, token));
            }

            if (candidates.Select(v => v.Hash).Distinct().Skip(1).Any())
            {
                throw new InvalidOperationException("The script changed while its methods were being prepared; retry the update.");
            }

            lock (_gate)
            {
                if (initial != _generation)
                {
                    throw new OperationCanceledException("Script catalog was reset.");
                }

                foreach (var candidate in candidates)
                {
                    Publish(candidate);
                }

                candidates.Clear();
            }
        }
        catch (HttpRequestException error) when (IsAuthorizationError(error))
        {
            lock (_gate)
            {
                foreach (var version in previous)
                {
                    _denied.Add(version.Method.Id);
                }
            }

            throw;
        }
        finally
        {
            foreach (var candidate in candidates)
            {
                candidate.Dispose();
            }

            _updates.Release();
        }
    }

    /// <summary>Gets distinct identifiers of scripts whose prepared versions are cached.</summary>
    internal Guid[] ScriptIds { get { lock (_gate) { return [.. _latest.Values.Select(v => v.ScriptId).Distinct()]; } } }
    /// <summary>Identifies download failures caused by rejected or insufficient credentials.</summary>
    /// <param name="e">Failure inspected for an authorization status.</param>
    /// <returns>Whether the server returned an unauthorized or forbidden status.</returns>
    private bool IsAuthorizationError(HttpRequestException e) => e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    /// <summary>Publishes a prepared script version and releases any superseded version.</summary>
    /// <param name="candidate">Prepared script version to publish.</param>
    private void Publish(Version candidate)
    {
        if (_latest.TryGetValue(candidate.Method.Id, out var old) && old.Hash == candidate.Hash &&
            old.Method.Name == candidate.Method.Name && old.Parameters.SequenceEqual(candidate.Parameters))
        {
            candidate.Dispose();
        }
        else
        {
            _prepared.Add(candidate.Method, candidate);
            _latest[candidate.Method.Id] = candidate;
        }
        _denied.Remove(candidate.Method.Id);
    }

    /// <summary>Downloads and validates content required to prepare an application or script release.</summary>
    /// <param name="methodId">Server identifier of the script method to retrieve.</param>
    /// <param name="types">Expected parameter types for the script entry point.</param>
    /// <param name="expectedGeneration">Cache generation that must remain current before publishing results.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A prepared script version compiled from the current authorized source.</returns>
    private async Task<Version> DownloadAsync(Guid methodId, Type?[] types, int expectedGeneration, CancellationToken token)
    {
        var script = await FetchAsync(methodId, false, token);
        if (script.CompilationDependencies?.Count is not > 0)
        {
            throw new InvalidOperationException("Script dependency metadata is missing. Rebuild the script on the server.");
        }
        if (script.ByteAssembly?.Length is not > 0)
        {
            throw new InvalidOperationException("A compiled script assembly is required.");
        }
        var localVersions = GXScriptCompiler.GetAvailableVersions();
        // DLL and dependency rows can be read across a concurrent server rebuild.
        // Never trust separately read metadata to authorize an incompatible image.
        if (!GXScriptCompiler.Matches(script.CompilationDependencies, localVersions) ||
            !GXScriptCompiler.Matches(GXScriptCompiler.ReadDependencies(script.ByteAssembly), localVersions))
        {
            var source = await FetchAsync(methodId, true, token);
            if (source.Id != script.Id || string.IsNullOrEmpty(script.ConcurrencyStamp) ||
                source.ConcurrencyStamp != script.ConcurrencyStamp ||
                source.ByteAssembly == null || script.ByteAssembly == null ||
                !source.ByteAssembly.SequenceEqual(script.ByteAssembly) ||
                !SameDependencies(script.CompilationDependencies, source.CompilationDependencies))
            {
                throw new InvalidOperationException("The script changed while its source was downloaded. Retry preparation.");
            }
            if (string.IsNullOrWhiteSpace(source.SourceCode))
            {
                throw new InvalidOperationException("Script source is unavailable for local compilation. Rebuild the script or update the agent.");
            }
            var key = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
                source.Id, source.SourceCode, source.Namespaces, source.Language,
                Versions = localVersions.OrderBy(v => v.Key, StringComparer.Ordinal)
            })));
            byte[]? image = null;
            lock (_gate)
            {
                if (_compilations.TryGetValue(source.Id, out var cached) && cached.Key == key)
                {
                    image = cached.Image;
                }
            }
            if (image == null)
            {
                image = await Task.Run(() =>
                {
                    var errors = new List<GXScriptException>();
                    var bytes = GXScriptCompiler.Compile(source.Id.ToString(), source.Language,
                        source.SourceCode, source.Namespaces, errors, null);
                    if (bytes == null || errors.Count != 0)
                    {
                        throw new InvalidOperationException("Local script compilation failed. Update the agent if the source requires a newer API. " +
                            string.Join("; ", errors.Select(e => $"{e.Id} (line {e.Line}): {e.Message}")));
                    }
                    return bytes;
                }, token);
                token.ThrowIfCancellationRequested();
                lock (_gate)
                {
                    if (_generation != expectedGeneration)
                    {
                        throw new OperationCanceledException("Script catalog was reset.");
                    }
                    _compilations[source.Id] = new Compilation(key, image);
                }
            }
            script = source;
            script.ByteAssembly = image;
            script.CompilationDependencies = GXScriptCompiler.ReadDependencies(image);
        }
        var method = script.Methods!.Single(m => m.Id == methodId);
        if (script.ByteAssembly?.Length is not > 0 || string.IsNullOrWhiteSpace(method.Name))
        {
            throw new InvalidOperationException("A compiled script assembly and method name are required.");
        }

        method.Script = script;
        var candidate = new Version(method, types);
        try
        {
            // Loading/validation/JIT runs off the connection handling thread and never invokes script code.
            await Task.Run(candidate.FillInitial, token);
            token.ThrowIfCancellationRequested();
            return candidate;
        }
        catch
        {
            candidate.Dispose();
            throw;
        }
    }

    /// <summary>Fetches script metadata or source from the configured AMI server.</summary>
    /// <param name="methodId">Server identifier of the script method to retrieve.</param>
    /// <param name="source">Original value copied without sharing mutable state.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Script metadata or source returned for the requested method.</returns>
    private async Task<GXScript> FetchAsync(Guid methodId, bool source, CancellationToken token)
    {
        using var response = await _client.PostAsJsonAsync("/api/Script/List", new ListScripts
        {
            Select = source ? ["Assembly", "SourceCode", "CompilationDependencies"] : ["Assembly", "CompilationDependencies"],
            Filter = new GXScript { Methods = [new GXScriptMethod { Id = methodId }] }
        }, token);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListScriptsResponse>(cancellationToken: token);
        return result?.Scripts?.SingleOrDefault(s => s.Methods?.Any(m => m.Id == methodId) == true)
            ?? throw new HttpRequestException($"Script method {methodId} is no longer accessible.", null, HttpStatusCode.Forbidden);
    }

    /// <summary>Checks that both script versions reference the same dependency names and versions.</summary>
    /// <param name="first">Dependencies of the previously prepared script version.</param>
    /// <param name="second">Dependencies reported by the candidate script version.</param>
    /// <returns>Whether dependency identities and versions match for both scripts.</returns>
    private static bool SameDependencies(List<GXScriptDependency> first, List<GXScriptDependency>? second)
        => second != null && first.OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d => (d.Name, d.AssemblyVersion)).SequenceEqual(second.OrderBy(d => d.Name, StringComparer.Ordinal)
                .Select(d => (d.Name, d.AssemblyVersion)));

    /// <summary>Releases unused contexts on worker shutdown; running contexts retain their own lifetimes.</summary>
    internal void Clear()
    {
        lock (_gate)
        {
            ++_generation;
            foreach (var version in _prepared.Select(p => p.Value).Distinct())
            {
                version.Dispose();
            }

            _prepared = [];
            _latest.Clear();
            _compilations.Clear();
            _denied.Clear();
        }
    }

    /// <summary>Owns a compiled script version and its pool of isolated load contexts.</summary>
    private sealed class Version : IDisposable
    {
        /// <summary>Number of isolated script load contexts kept ready for execution.</summary>
        private const int WarmContexts = 2;
        /// <summary>Script method represented by this prepared version.</summary>
        internal readonly GXScriptMethod Method;
        /// <summary>Compiled assembly bytes loaded into isolated execution contexts.</summary>
        internal readonly byte[] Image;
        /// <summary>Expected parameter types for the prepared script method.</summary>
        internal readonly Type?[] Parameters;
        /// <summary>Content hash identifying the compiled script version.</summary>
        internal readonly string Hash;
        /// <summary>Gets the script identifier currently configured for notification processing.</summary>
        internal Guid ScriptId => Method.Script!.Id;
        /// <summary>Pool of isolated assembly contexts available for script invocation.</summary>
        private readonly Channel<AssemblyLoadContext> _ready = Channel.CreateUnbounded<AssemblyLoadContext>();
        /// <summary>Protects concurrent access to the owning object's mutable state.</summary>
        private readonly Lock _gate = new();
        /// <summary>Indicates that owned resources have been released.</summary>
        private bool _disposed;
        /// <summary>Indicates that replacement script load contexts are being prepared in the background.</summary>
        private bool _refilling;
        /// <summary>Failure encountered while preparing a replacement script context.</summary>
        private Exception? _failure;

        /// <summary>Owns a compiled script version and its pool of isolated load contexts.</summary>
        /// <param name="method">Script method prepared for isolated execution.</param>
        /// <param name="types">Expected parameter types for the script entry point.</param>
        internal Version(GXScriptMethod method, Type?[] types)
        { Method = method; Image = [.. method.Script!.ByteAssembly!]; Parameters = [.. types]; Hash = Convert.ToHexString(SHA256.HashData(Image)); }

        /// <summary>Prepares the initial pool of isolated script execution contexts.</summary>
        internal void FillInitial()
        {
            for (int i = 0; i < WarmContexts; ++i)
            {
                _ready.Writer.TryWrite(PrepareContext());
            }
        }

        /// <summary>Loads a compiled script image into an isolated collectible assembly context.</summary>
        /// <returns>A collectible load context containing the compiled script assembly.</returns>
        private AssemblyLoadContext PrepareContext()
        {
            var context = new AssemblyLoadContext($"AMI.{Method.Id}.{Hash[..12]}", true);
            try
            {
                using var stream = new MemoryStream(Image, false);
                var assembly = context.LoadFromStream(stream);
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    var dependency = context.LoadFromAssemblyName(reference).GetName();
                    if (reference.Version != null && dependency.Version < reference.Version)
                    {
                        throw new FileLoadException($"Script dependency {reference} is newer than the agent's {dependency}.");
                    }
                }

                var type = assembly.GetType("Gurux.DLMS.AMI.GeneratedScript.GXAmiScript", throwOnError: true)!;
                var constructor = type.GetConstructor([typeof(IGXAmi)]) ?? throw new InvalidOperationException("Script must have a public constructor accepting IGXAmi.");
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == Method.Name).ToArray();
                if (methods.Length != 1)
                {
                    throw new InvalidOperationException("Script entry point must be a unique public instance method.");
                }

                var entry = methods[0];
                var parameters = entry.GetParameters();
                if (entry.ContainsGenericParameters || entry.ReturnType.IsByRef || parameters.Length != Parameters.Length || parameters.Where((p, i) => p.ParameterType.IsByRef || p.ParameterType.IsPointer || Parameters[i] != null && !p.ParameterType.IsAssignableFrom(Parameters[i]!)).Any())
                {
                    throw new InvalidOperationException($"Script method {Method.Name} has an incompatible parameter signature.");
                }

                RuntimeHelpers.PrepareMethod(constructor.MethodHandle);
                RuntimeHelpers.PrepareMethod(entry.MethodHandle);
                return context;
            }
            catch
            {
                context.Unload();
                throw;
            }
        }

        /// <summary>Takes a prepared isolated script context and schedules a replacement for the pool.</summary>
        /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
        /// <returns>A prepared isolated assembly context ready for a single invocation.</returns>
        internal async Task<AssemblyLoadContext> RentAsync(CancellationToken token)
        {
            ScheduleRefill();
            var context = await _ready.Reader.ReadAsync(token);
            try
            {
                ScheduleRefill();
                return context;
            }
            catch
            {
                context.Unload();
                throw;
            }
        }

        /// <summary>Starts asynchronous replacement of consumed script load contexts.</summary>
        private void ScheduleRefill()
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, nameof(Version));

                if (_refilling || _failure != null || _ready.Reader.Count >= WarmContexts)
                {
                    return;
                }

                _refilling = true;
            }
            _ = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        lock (_gate)
                        {
                            if (_disposed || _ready.Reader.Count >= WarmContexts)
                            {
                                break;
                            }
                        }

                        var context = PrepareContext();
                        lock (_gate)
                        {
                            if (_disposed || !_ready.Writer.TryWrite(context))
                            {
                                context.Unload();
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    lock (_gate)
                    {
                        _failure = error;
                        _ready.Writer.TryComplete(error);
                    }
                }
                finally
                {
                    lock (_gate)
                    {
                        _refilling = false;
                        if (!_disposed && _failure == null && _ready.Reader.Count < WarmContexts)
                        {
                            ScheduleRefill();
                        }
                    }
                }
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _ready.Writer.TryComplete();
                while (_ready.Reader.TryRead(out var context))
                {
                    context.Unload();
                }
            }
            GC.SuppressFinalize(this);
        }
        /// <summary>Releases cached script load contexts if explicit disposal was omitted.</summary>
        ~Version() { Dispose(); }
    }
}
