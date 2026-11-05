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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Page component exposed by a module.</summary>
/// <param name="Component">Fully qualified component type name.</param>
/// <param name="Policy">Required access policy.</param>
/// <param name="Route">Unique module page URL segment.</param>
public sealed record AmiModulePage(string Route, string Component, string Policy)
{
    /// <summary>Optional page title; <see langword="null"/> when no title was supplied.</summary>
    public string? Title
    {
        get; init;
    }
    /// <summary>Whether the page appears in module navigation; defaults to <see langword="false"/>.</summary>
    public bool Navigation
    {
        get; init;
    }
}
