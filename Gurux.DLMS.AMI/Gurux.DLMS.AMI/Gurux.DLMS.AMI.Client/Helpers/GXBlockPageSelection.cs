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

using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

internal static class GXBlockPageSelection
{
    internal static bool Matches(GXBlock block, string relativePath)
    {
        var pages = (block.Pages ?? "").Split(['\r', '\n', ',', ';'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pages.Length == 0)
        {
            return true;
        }

        var path = Normalize(relativePath);
        var listed = pages.Any(page => string.Equals(Normalize(page), path, StringComparison.OrdinalIgnoreCase));
        return block.PageVisibility switch
        {
            BlockPageVisibility.ShowOnListedPages => listed,
            BlockPageVisibility.HideOnListedPages => !listed,
            _ => false
        };
    }

    private static string Normalize(string path) => path.Split('?', '#')[0].Trim().Trim('/');
}
