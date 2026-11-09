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
    private readonly HttpClient client;
    private readonly GXScriptExecution execution;
    private readonly TimeSpan executionTimeout;
    private readonly CancellationToken lifetime;

    internal GXAgentScripts(HttpClient client, GXScriptExecutionOptions? options = null, CancellationToken lifetime = default)
    {
        options ??= new GXScriptExecutionOptions();
        options.Validate();
        this.client = client;
        this.lifetime = lifetime;
        executionTimeout = options.ExecutionTimeout;
        execution = new GXScriptExecution(executionTimeout, lifetime);
    }

    private readonly Lock Gate = new();
    private readonly SemaphoreSlim Updates = new(1, 1);
    private readonly Dictionary<Guid, Version> Latest = [];
    private readonly HashSet<Guid> Denied = [];
    private ConditionalWeakTable<GXScriptMethod, Version> prepared = [];
    private int generation;
    private sealed record Compilation(string Key, byte[] Image);
    private readonly Dictionary<Guid, Compilation> compilations = [];

    /// <summary>Fetches and validates a configured method before its listener/worker is started.</summary>
    internal async Task<GXScriptMethod> LoadAsync(Guid methodId, Type?[] parameterTypes, CancellationToken token = default)
    {
        await Updates.WaitAsync(token);
        try
        {
            int initial;
            lock (Gate)
            {
                initial = generation;
            }

            var candidate = await DownloadAsync(methodId, parameterTypes, initial, token);
            lock (Gate)
            {
                if (initial != generation)
                {
                    candidate.Dispose();
                    throw new OperationCanceledException("Script catalog was reset.");
                }

                Publish(candidate);
                return Latest[methodId].Method;
            }
        }
        catch (HttpRequestException error) when (IsAuthorizationError(error))
        {
            lock (Gate)
            {
                Denied.Add(methodId);
            }

            throw;
        }
        finally
        {
            Updates.Release();
        }
    }

    /// <summary>Captures the latest validated snapshot at the beginning of a new connection.</summary>
    internal GXScriptMethod? Current(GXScriptMethod? original)
    {
        if (original == null)
        {
            return null;
        }

        lock (Gate)
        {
            if (Denied.Contains(original.Id))
            {
                throw new UnauthorizedAccessException("Script access has been revoked.");
            }

            return Latest.TryGetValue(original.Id, out var version) ? version.Method : original;
        }
    }

    /// <summary>Uses only the explicitly pinned version; no script download or compilation occurs here.</summary>
    internal GXScriptRunArgs CreateRunArgs(GXScriptMethod method, object?[] parameters)
        => CreateRunArgsAsync(method, parameters).GetAwaiter().GetResult();

    internal async Task<GXScriptRunArgs> CreateRunArgsAsync(GXScriptMethod method, object?[] parameters,
        CancellationToken token = default)
    {
        Version version;
        lock (Gate)
        {
            if (!prepared.TryGetValue(method, out version!))
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
    internal async Task<object?> RunAsync(GXAmiScript runner, GXScriptMethod method, object?[] parameters,
        CancellationToken token = default)
    {
        execution.CheckAdmission();
        using var preparation = CancellationTokenSource.CreateLinkedTokenSource(lifetime, token);
        preparation.CancelAfter(executionTimeout);
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
    internal Task<object?> RunPreparedAsync(GXAmiScript runner, GXScriptRunArgs args, CancellationToken token = default)
        => execution.RunAsync(args, async cancellation => await AwaitResultAsync(await runner.RunAsync(args, cancellation)), token);

    /// <summary>Defers unloading when cancelled script code still owns this context.</summary>
    internal void ReleaseRunArgs(GXScriptRunArgs args) => execution.Release(args);

    internal bool CanExecute => execution.CanExecute;

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
    internal async Task RefreshAsync(Guid scriptId, CancellationToken token = default)
    {
        await Updates.WaitAsync(token);
        var candidates = new List<Version>();
        Version[] previous = [];
        try
        {
            int initial;
            lock (Gate)
            {
                initial = generation;
                previous = [.. Latest.Values.Where(v => v.ScriptId == scriptId)];
            }

            foreach (var version in previous)
            {
                candidates.Add(await DownloadAsync(version.Method.Id, version.Parameters, initial, token));
            }

            if (candidates.Select(v => v.Hash).Distinct().Skip(1).Any())
            {
                throw new InvalidOperationException("The script changed while its methods were being prepared; retry the update.");
            }

            lock (Gate)
            {
                if (initial != generation)
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
            lock (Gate)
            {
                foreach (var version in previous)
                {
                    Denied.Add(version.Method.Id);
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

            Updates.Release();
        }
    }

    internal Guid[] ScriptIds { get { lock (Gate) { return [.. Latest.Values.Select(v => v.ScriptId).Distinct()]; } } }
    private bool IsAuthorizationError(HttpRequestException e) => e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    private void Publish(Version candidate)
    {
        if (Latest.TryGetValue(candidate.Method.Id, out var old) && old.Hash == candidate.Hash &&
            old.Method.Name == candidate.Method.Name && old.Parameters.SequenceEqual(candidate.Parameters))
        {
            candidate.Dispose();
        }
        else
        {
            prepared.Add(candidate.Method, candidate);
            Latest[candidate.Method.Id] = candidate;
        }
        Denied.Remove(candidate.Method.Id);
    }

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
            lock (Gate)
            {
                if (compilations.TryGetValue(source.Id, out var cached) && cached.Key == key)
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
                lock (Gate)
                {
                    if (generation != expectedGeneration)
                    {
                        throw new OperationCanceledException("Script catalog was reset.");
                    }
                    compilations[source.Id] = new Compilation(key, image);
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

    private async Task<GXScript> FetchAsync(Guid methodId, bool source, CancellationToken token)
    {
        using var response = await client.PostAsJsonAsync("/api/Script/List", new ListScripts
        {
            Select = source ? ["Assembly", "SourceCode", "CompilationDependencies"] : ["Assembly", "CompilationDependencies"],
            Filter = new GXScript { Methods = [new GXScriptMethod { Id = methodId }] }
        }, token);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListScriptsResponse>(cancellationToken: token);
        return result?.Scripts?.SingleOrDefault(s => s.Methods?.Any(m => m.Id == methodId) == true)
            ?? throw new HttpRequestException($"Script method {methodId} is no longer accessible.", null, HttpStatusCode.Forbidden);
    }

    private static bool SameDependencies(List<GXScriptDependency> first, List<GXScriptDependency>? second)
        => second != null && first.OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d => (d.Name, d.AssemblyVersion)).SequenceEqual(second.OrderBy(d => d.Name, StringComparer.Ordinal)
                .Select(d => (d.Name, d.AssemblyVersion)));

    /// <summary>Releases unused contexts on worker shutdown; running contexts retain their own lifetimes.</summary>
    internal void Clear()
    {
        lock (Gate)
        {
            ++generation;
            foreach (var version in prepared.Select(p => p.Value).Distinct())
            {
                version.Dispose();
            }

            prepared = [];
            Latest.Clear();
            compilations.Clear();
            Denied.Clear();
        }
    }

    private sealed class Version : IDisposable
    {
        private const int WarmContexts = 2;
        internal readonly GXScriptMethod Method;
        internal readonly byte[] Image;
        internal readonly Type?[] Parameters;
        internal readonly string Hash;
        internal Guid ScriptId => Method.Script!.Id;
        private readonly Channel<AssemblyLoadContext> ready = Channel.CreateUnbounded<AssemblyLoadContext>();
        private readonly Lock gate = new();
        private bool disposed, refilling;
        private Exception? failure;

        internal Version(GXScriptMethod method, Type?[] types)
        { Method = method; Image = [.. method.Script!.ByteAssembly!]; Parameters = [.. types]; Hash = Convert.ToHexString(SHA256.HashData(Image)); }

        internal void FillInitial()
        {
            for (int i = 0; i < WarmContexts; ++i)
            {
                ready.Writer.TryWrite(PrepareContext());
            }
        }

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

        internal async Task<AssemblyLoadContext> RentAsync(CancellationToken token)
        {
            ScheduleRefill();
            var context = await ready.Reader.ReadAsync(token);
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

        private void ScheduleRefill()
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, nameof(Version));

                if (refilling || failure != null || ready.Reader.Count >= WarmContexts)
                {
                    return;
                }

                refilling = true;
            }
            _ = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        lock (gate)
                        {
                            if (disposed || ready.Reader.Count >= WarmContexts)
                            {
                                break;
                            }
                        }

                        var context = PrepareContext();
                        lock (gate)
                        {
                            if (disposed || !ready.Writer.TryWrite(context))
                            {
                                context.Unload();
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    lock (gate)
                    {
                        failure = error;
                        ready.Writer.TryComplete(error);
                    }
                }
                finally
                {
                    lock (gate)
                    {
                        refilling = false;
                        if (!disposed && failure == null && ready.Reader.Count < WarmContexts)
                        {
                            ScheduleRefill();
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                ready.Writer.TryComplete();
                while (ready.Reader.TryRead(out var context))
                {
                    context.Unload();
                }
            }
            GC.SuppressFinalize(this);
        }
        ~Version() { Dispose(); }
    }
}
