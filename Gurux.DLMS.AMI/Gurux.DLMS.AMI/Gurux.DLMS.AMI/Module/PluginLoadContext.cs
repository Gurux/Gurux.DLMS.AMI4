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

using Gurux.DLMS.AMI.Services;
using System.Reflection;
using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Collectible implementation/dependency context sharing only host contracts and framework assemblies.</summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly string directory;
    private readonly string mainPath;
    private static readonly HashSet<string> HostAssemblies = new(
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => Path.GetFileNameWithoutExtension(path)),
        StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Shared = new(StringComparer.OrdinalIgnoreCase)
    {
        typeof(IGXAmiModule).Assembly.GetName().Name!, typeof(IGXAmiEmailSender).Assembly.GetName().Name!,
        typeof(Gurux.DLMS.AMI.Client.Shared.KeyManagement.IKeyService).Assembly.GetName().Name!,
        typeof(Gurux.DLMS.AMI.Shared.DTOs.KeyManagement.GXKeyManagement).Assembly.GetName().Name!,
        typeof(Gurux.DLMS.AMI.Http.GXMaintenanceException).Assembly.GetName().Name!,
        "Gurux.Service.Common", "Gurux.Service", "Gurux.UI.Components", "Gurux.DLMS.AMI.Components"
    };
    /// <summary>Performs the is shared assembly operation.</summary>
    /// <param name="name">The name.</param>
    public static bool IsSharedAssembly(string name) => Shared.Contains(name);
    /// <summary>Performs the resolve shared assembly operation.</summary>
    /// <param name="name">The name.</param>
    public static Assembly ResolveSharedAssembly(string name)
    {
        if (!Shared.Contains(name))
        {
            throw new AmiModuleDependencyException($"Assembly '{name}' is not a shared host contract.");
        }

        return Default.Assemblies.FirstOrDefault(a => a.GetName().Name!.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Default.LoadFromAssemblyName(new AssemblyName(name));
    }
    /// <summary>Initializes a new instance of <see cref="PluginLoadContext"/>.</summary>
    /// <param name="pluginMainAssemblyPath">The plugin main assembly path.</param>
    public PluginLoadContext(string pluginMainAssemblyPath) : base(isCollectible: true)
    {
        pluginMainAssemblyPath = Path.GetFullPath(pluginMainAssemblyPath);
        resolver = new AssemblyDependencyResolver(pluginMainAssemblyPath);
        directory = Path.GetDirectoryName(pluginMainAssemblyPath)!;
        mainPath = pluginMainAssemblyPath;
    }
    /// <summary>Performs the load main assembly operation.</summary>
    public Assembly LoadMainAssembly()
    {
        return LoadWithSymbols(mainPath);
    }
    private Assembly LoadWithSymbols(string path)
    {
        Gurux.DLMS.AMI.Server.Module.GXSharedAssemblyCompatibility.ValidateReferences(path);
        using var stream = File.OpenRead(path);
        string pdb = Path.ChangeExtension(path, ".pdb");
        using var symbols = File.Exists(pdb) ? File.OpenRead(pdb) : null;
        return symbols == null ? LoadFromStream(stream) : LoadFromStream(stream, symbols);
    }
    /// <summary>Performs the validate dependencies operation.</summary>
    /// <param name="main">The main.</param>
    public void ValidateDependencies(Assembly main)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<Assembly>();
        pending.Enqueue(main);
        while (pending.TryDequeue(out var assembly))
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!visited.Add(reference.FullName))
                {
                    continue;
                }

                Assembly dependency;
                try
                {
                    dependency = LoadFromAssemblyName(reference);
                }
                catch (Exception exception) when (exception is FileNotFoundException or FileLoadException)
                {
                    throw new FileNotFoundException($"Module dependency '{reference.FullName}' could not be loaded from '{directory}'. Build the module and its ProjectReference dependencies with CopyLocal enabled before starting AMI.", reference.Name + ".dll", exception);
                }
                if (GetLoadContext(dependency) == this)
                {
                    pending.Enqueue(dependency);
                }
            }
        }
    }
    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string name = assemblyName.Name!;
        if (Shared.Contains(name))
        {
            var offered = ResolveSharedAssembly(name);
            Gurux.DLMS.AMI.Server.Module.GXSharedAssemblyCompatibility.Validate(assemblyName, offered);
            return offered;
        }
        if (name is "netstandard" or "mscorlib" || name.StartsWith("System.", StringComparison.Ordinal) || name.StartsWith("Microsoft.", StringComparison.Ordinal))
        {
            var loaded = Default.Assemblies.FirstOrDefault(a => a.GetName().Name == name);
            if (loaded != null)
            {
                return loaded;
            }
            if (HostAssemblies.Contains(name))
            {
                return Default.LoadFromAssemblyName(assemblyName);
            }
        }
        string? path = resolver.ResolveAssemblyToPath(assemblyName);
        bool satellite = name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(assemblyName.CultureName);
        if (path == null)
        {
            string local = satellite
                ? Path.Combine(directory, assemblyName.CultureName!, name + ".dll")
                : Path.Combine(directory, name + ".dll");
            if (File.Exists(local))
            {
                path = local;
            }
        }
        if (path == null)
        {
            if (satellite)
            {
                // ResourceManager probes parent cultures and neutral resources when a satellite is absent.
                return null;
            }
            throw new FileNotFoundException($"Module dependency '{assemblyName.FullName}' is missing from '{directory}'. Build the module and its ProjectReference dependencies with CopyLocal enabled before starting AMI.", name + ".dll");
        }

        return LoadWithSymbols(path);
    }
    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
