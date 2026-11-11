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


using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Anonymous module access requires an explicit declaration and host-owned method/route allowlist.</summary>
internal static class GXModuleAnonymousAccess
{
    public static bool IsAllowed(IConfiguration configuration, string moduleId, AmiEndpointDefinition endpoint, string method)
    {
        if (!endpoint.IsAnonymous || endpoint.Policies.Count != 0
            || !endpoint.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        var key = method.ToUpperInvariant() + " /api/modules/" + moduleId + "/" + endpoint.Pattern.Trim('/');
        return configuration.GetSection("Modules:AnonymousEndpoints").GetChildren()
            .Any(item => string.Equals(item.Value, key, StringComparison.Ordinal));
    }
}
