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
using Microsoft.AspNetCore.Http;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Dispatches authentication only to the owner of the selected endpoint.</summary>
internal interface IGXModuleAuthenticationRuntime
{
    bool CanAuthenticate(HttpContext context);
    Task<AmiAuthenticationResult> AuthenticateAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>Host-owned metadata keeps an endpoint bound to its original module generation.</summary>
internal sealed record GXModuleEndpointGeneration(string Generation, bool HasAuthenticator);
