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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Client;

/// <summary>Serializable runtime metadata, containing no plugin Type or delegate references.</summary>
public sealed record AmiModuleManifest(string Id, string Name, string Version, string ContentId,
    AmiModuleState State, IReadOnlyList<string> Assemblies)
{
    /// <summary>The saved deployment choice projected by the runtime host.</summary>
    public bool Enabled { get; init; } = true;

    public string CompatibilityVersion { get; init; } = "";
    public string? Error
    {
        get; init;
    }
    public IReadOnlyList<AmiModuleDependency> Dependencies { get; init; } = [];
    public IReadOnlyList<string> ResolvedDependencyModuleIds { get; init; } = [];
    public IReadOnlyList<AmiModuleView> Views { get; init; } = [];
    public IReadOnlyList<AmiModulePage> Pages { get; init; } = [];
}

