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

using Gurux.DLMS.AMI.Shared;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Resolves module targets after checking user access.</summary>
public interface IAmiTargetProvider
{
    /// <summary>Resolves an accessible target for the specified user, or returns null.</summary>
    /// <param name="user">User whose access to the target must be checked.</param>
    /// <param name="target">Module-defined target category.</param>
    /// <param name="targetId">Identifier of the requested target.</param>
    /// <param name="cancellationToken">Token used to cancel target resolution.</param>
    /// <returns>The target's display name and navigation path, or <see langword="null"/> when unavailable to the user.</returns>
    Task<GXAmiTarget?> ResolveAsync(ClaimsPrincipal user, string target, string targetId, CancellationToken cancellationToken);
}
