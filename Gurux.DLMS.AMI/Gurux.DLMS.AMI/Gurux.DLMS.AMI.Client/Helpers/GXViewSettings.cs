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

using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Client.Internal;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Reads administrator defaults without saving personal column preferences.</summary>
public static class GXViewSettings
{
    /// <summary>Resolves column customization, allowing only administrators when no preference is configured.</summary>
    public static bool CanCustomizeColumns(GXView? view, bool isAdmin) => view?.AllowColumnCustomization ?? isAdmin;

    /// <summary>Reads all defaults for the named table.</summary>
    public static Task<GXView?> ReadAsync(HttpClient http, string name, IAmiViewStorage? storage = null) => new GXViewAdapter(http, storage).ReadNameAsync(name);

    /// <summary>Returns visible column names in the administrator's default order.</summary>
    public static async Task<string[]?> LoadAsync(HttpClient http, string name, string[]? defaults = null, IAmiViewStorage? storage = null)
    {
        var view = await ReadAsync(http, name, storage);
        if (view == null || view.Enabled == false)
        {
            return defaults;
        }
        var columns = defaults == null ? view.Columns ?? [] : GXViewColumnDefaults.Merge(
            [.. defaults.Select((name, position) => new GXViewColumn(name) { Position = position })], view.Columns);
        return [.. columns.Where(c => c.Visible && !string.IsNullOrWhiteSpace(c.Name))
            .OrderBy(c => c.Position).Select(c => c.Name!)];
    }
}
