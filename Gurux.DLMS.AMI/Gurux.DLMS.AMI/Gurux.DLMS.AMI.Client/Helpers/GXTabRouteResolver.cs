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

using Gurux.UI.Components.Tab;
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Connects generic tab URL persistence to AMI's canonical page routes.</summary>
public sealed class GXTabRouteResolver : ITabRouteResolver
{
    /// <inheritdoc />
    public bool IsTabNavigation(string source, string target) => GXTabRoutes.IsTabNavigation(source, target);

    /// <inheritdoc />
    public string? GetActive(NavigationManager navigation, string? routeGroup, int depth, string? fallback)
        => GXTabRoutes.Active(navigation, routeGroup, depth, fallback);
    /// <inheritdoc />
    public string GetSelectionUrl(NavigationManager navigation, string? routeGroup, int depth, string tab)
        => GXTabRoutes.SelectionUrl(navigation, routeGroup, depth, tab);
}
