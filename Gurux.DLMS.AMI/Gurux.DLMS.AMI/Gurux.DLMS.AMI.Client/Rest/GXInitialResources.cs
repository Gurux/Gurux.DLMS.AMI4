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

namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>Versions and keys already held by the client for its first view.</summary>
public sealed class GXInitialResources
{
    /// <summary>Include the server-configured resources marked for preloading.</summary>
    public bool IncludeDefaults { get; set; } = true;
    public string? Language { get; set; }
    public string[] MenuNames { get; set; } = [];
    public string[] Texts { get; set; } = [];
    public string[] Images { get; set; } = [];
    public Dictionary<string, string> Versions { get; set; } = [];
    public Dictionary<string, string[]> Keys { get; set; } = [];
}

public sealed class GXInitialResourceGroup
{
    public required string Version { get; set; }
    public Dictionary<string, string?> Values { get; set; } = [];
}

public sealed class GXInitialResourcesResponse
{
    public string Language { get; set; } = "en";
    /// <summary>Profile or browser culture used for dates and numbers, independent of installed translations.</summary>
    public string? Culture { get; set; }
    public Dictionary<string, GXInitialResourceGroup> Resources { get; set; } = [];
    public List<string> Unavailable { get; set; } = [];
}
