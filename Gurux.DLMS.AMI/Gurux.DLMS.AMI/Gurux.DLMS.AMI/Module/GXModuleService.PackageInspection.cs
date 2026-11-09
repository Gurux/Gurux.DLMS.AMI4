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

using System.IO.Compression;
using Gurux.DLMS.AMI.Server.Module;

namespace Gurux.DLMS.AMI.Module;

internal sealed partial class GXModuleService
{
    internal static GXModuleInstallCandidate InspectPackage(string zip, string expectedId, CancellationToken cancellationToken)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GuruxModuleInspection", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            ZipFile.ExtractToDirectory(zip, directory);
            var candidates = new List<GXModuleInstallCandidate>();
            foreach (string file in Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var (module, context) = Load(file);
                    try
                    {
                        candidates.Add(new(module.Id, GXModuleDependencyValidator.ReadCompatibilityVersion(module.GetType().Assembly),
                            GXModuleDependencyValidator.ValidateDeclarations(module.Id, module.Dependencies)));
                    }
                    finally { context.Unload(); }
                }
                catch (BadImageFormatException) { }
                catch (ModuleAssemblyException) { }
            }
            if (candidates.Count != 1)
                {
                throw new InvalidOperationException("A module package must contain exactly one concrete IGXAmiModule implementation.");
            }
            var candidate = candidates[0];
            if (candidate.Id != expectedId)
                {
                throw new InvalidOperationException($"The downloaded package does not match the requested module. Expected '{expectedId}', package declares '{candidate.Id}'.");
            }
            return candidate;
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
