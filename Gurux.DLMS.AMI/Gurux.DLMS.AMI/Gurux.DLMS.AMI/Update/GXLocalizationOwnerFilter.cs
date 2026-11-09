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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;

namespace Gurux.DLMS.AMI.Update;

internal static class GXLocalizationOwnerFilter
{
    public static IEnumerable<GXLocalization> Filter(IEnumerable<GXLocalization> rows, IEnumerable<GXModule> modules)
    {
        var installed = modules
            .Where(module => ((module.Status ?? 0) & (ModuleStatus.Installed | ModuleStatus.CustomBuild)) != 0)
            .Select(module => module.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return rows.Where(row => !string.Equals(row.OwnerType, "module", StringComparison.OrdinalIgnoreCase)
            || installed.Contains(row.Owner));
    }
}
