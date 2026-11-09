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

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Configures loading modules from local development projects.</summary>
public sealed class DevelopmentModuleOptions
{
    /// <summary>Gets or sets the enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>Gets or sets the configuration.</summary>
    public string Configuration { get; set; } = "Debug";
    /// <summary>Gets or sets the target framework.</summary>
    public string? TargetFramework { get; set; }
    /// <summary>Gets or sets the runtime identifier.</summary>
    public string? RuntimeIdentifier { get; set; }
    /// <summary>Gets or sets the creator id.</summary>
    public string? CreatorId { get; set; }
    /// <summary>Gets or sets the modules.</summary>
    public List<DevelopmentModuleSource> Modules { get; set; } = [];
}

/// <summary>Identifies a development module project and its build settings.</summary>
public sealed class DevelopmentModuleSource
{
    /// <summary>Gets or sets the project.</summary>
    public string? Project { get; set; }
    /// <summary>Gets or sets the directory.</summary>
    public string? Directory { get; set; }
    /// <summary>Gets or sets the assembly file.</summary>
    public string? AssemblyFile { get; set; }
    /// <summary>Gets or sets the configuration.</summary>
    public string? Configuration { get; set; }
    /// <summary>Gets or sets the target framework.</summary>
    public string? TargetFramework { get; set; }
    /// <summary>Gets or sets the runtime identifier.</summary>
    public string? RuntimeIdentifier { get; set; }
    /// <summary>Gets or sets the settings.</summary>
    public string? Settings { get; set; }
}

/// <summary>The resolved paths and settings for a development module.</summary>
/// <param name="Directory">The directory.</param>
/// <param name="AssemblyFile">The assembly file.</param>
/// <param name="Settings">The settings.</param>
public sealed record ResolvedDevelopmentModule(string Directory, string? AssemblyFile, string? Settings);
