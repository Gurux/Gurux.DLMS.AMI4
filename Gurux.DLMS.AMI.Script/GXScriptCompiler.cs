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

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gurux.DLMS.AMI.Script;

/// <summary>Shared script wrapper, compiler and assembly dependency inspection.</summary>
public static class GXScriptCompiler
{
    private static readonly string[] DefaultNamespaces =
    [
        "System", "System.Linq", "System.Linq.Expressions", "System.Collections.Generic", "System.Threading.Tasks",
        "Gurux.Common", "Gurux.DLMS.Enums", "Gurux.DLMS.AMI.Script", "Gurux.DLMS.AMI.Shared.DIs",
        "Gurux.DLMS.AMI.Shared.DTOs", "Gurux.DLMS.AMI.Shared.DTOs.Enums", "Gurux.DLMS.AMI.Shared.DTOs.Agent",
        "Gurux.DLMS.AMI.Shared.DTOs.Authentication", "Gurux.DLMS.AMI.Shared.DTOs.ComponentView",
        "Gurux.DLMS.AMI.Shared.DTOs.Device", "Gurux.DLMS.AMI.Shared.DTOs.Gateway",
        "Gurux.DLMS.AMI.Shared.DTOs.KeyManagement", "Gurux.DLMS.AMI.Shared.DTOs.Manufacturer",
        "Gurux.DLMS.AMI.Shared.DTOs.Module", "Gurux.DLMS.AMI.Shared.DTOs.Schedule",
        "Gurux.DLMS.AMI.Shared.DTOs.Script", "Gurux.DLMS.AMI.Shared.DTOs.Trigger",
        "Gurux.DLMS.AMI.Shared.DTOs.User", "Gurux.DLMS.AMI.Shared.DTOs.Workflow"
    ];

    /// <summary>Compiles a script body using the same generated type on server and agent.</summary>
    public static byte[]? Compile(string name, ScriptLanguage language, string source, string? namespaces,
        List<GXScriptException> errors, List<MethodDeclarationSyntax>? methods)
    {
        if (language != ScriptLanguage.CSharp)
        {
            throw new NotSupportedException("Only C# script bodies are supported.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        // Default namespaces must be available even before the first meter operation.
        Assembly.Load("Gurux.DLMS");
        Assembly.Load("Gurux.Common");
        var text = new StringBuilder();
        foreach (var item in DefaultNamespaces.Concat((namespaces ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)).Distinct())
        {
            text.Append("using ").Append(item.Trim()).AppendLine(";");
        }
        text.AppendLine("namespace Gurux.DLMS.AMI.GeneratedScript {");
        text.AppendLine("public class GXAmiScript {");
        text.AppendLine("public GXAmiScript(IGXAmi ami) { Ami = ami; }");
        text.AppendLine("public IGXAmi Ami { get; private set; }");
        text.AppendLine(source);
        text.AppendLine("} }");
        return GXAmiScript.Generate(language, name, text.ToString(), errors, methods);
    }

    /// <summary>Reads the emitted DLL references without loading or running script code.</summary>
    public static List<GXScriptDependency> ReadDependencies(byte[] image)
    {
        using var stream = new MemoryStream(image, false);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        return metadata.AssemblyReferences.Select(handle => metadata.GetAssemblyReference(handle))
            .Select(reference => new GXScriptDependency {
                Name = metadata.GetString(reference.Name), AssemblyVersion = reference.Version.ToString()
            }).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
    }

    internal static string[] GetReferencePaths()
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in trusted.Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")).Distinct())
        {
            try
            {
                paths[AssemblyName.GetAssemblyName(path).Name!] = path;
            }
            catch (BadImageFormatException)
            {
                // Ignore native libraries.
            }
        }
        foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                paths[assembly.GetName().Name!] = assembly.Location;
            }
        }
        return paths.Values.ToArray();
    }

    /// <summary>Returns assembly versions available to the current host without loading script assemblies.</summary>
    public static Dictionary<string, string> GetAvailableVersions()
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in GetReferencePaths())
        {
            var assembly = AssemblyName.GetAssemblyName(path);
            versions[assembly.Name!] = assembly.Version!.ToString();
        }
        foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
        {
            if (!assembly.IsDynamic)
            {
                var name = assembly.GetName();
                versions[name.Name!] = name.Version!.ToString();
            }
        }
        return versions;
    }

    /// <summary>Requires complete dependency metadata and exact local assembly versions.</summary>
    public static bool Matches(IReadOnlyCollection<GXScriptDependency>? dependencies, IReadOnlyDictionary<string, string> versions)
        => dependencies?.Count > 0 && dependencies.All(d => versions.TryGetValue(d.Name, out var version) && version == d.AssemblyVersion);
}
