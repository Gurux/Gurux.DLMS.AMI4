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
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides shared assembly compatibility.</summary>
public static class GXSharedAssemblyCompatibility
{
    /// <summary>Requires a host assembly with the same major version and at least the requested version.</summary>
    /// <param name="reference">The shared assembly reference declared by the module.</param>
    /// <param name="offered">The assembly supplied by the host.</param>
    /// <exception cref="Gurux.DLMS.AMI.Module.AmiModuleDependencyException">The assembly versions are incompatible or unspecified.</exception>
    public static void Validate(AssemblyName reference, Assembly offered)
    {
        var actual = offered.GetName();
        if (reference.Version == null || actual.Version == null || reference.Version.Major != actual.Version.Major || actual.Version < reference.Version)
        {
            throw new Gurux.DLMS.AMI.Module.AmiModuleDependencyException($"Shared assembly '{reference.Name}' requires AssemblyVersion {reference.Version} with matching major; host offers {actual.Version}.");
        }
    }
    /// <summary>Performs the validate references operation.</summary>
    /// <param name="path">The path.</param>
    public static void ValidateReferences(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata)
        {
            throw new BadImageFormatException("Module dependency has no .NET metadata.");
        }

        var reader = pe.GetMetadataReader();
        foreach (var handle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(handle);
            var name = reader.GetString(reference.Name);
            if (!Gurux.DLMS.AMI.Module.PluginLoadContext.IsSharedAssembly(name))
            {
                continue;
            }

            Validate(new AssemblyName(name) { Version = reference.Version }, Gurux.DLMS.AMI.Module.PluginLoadContext.ResolveSharedAssembly(name));
        }
    }
}
