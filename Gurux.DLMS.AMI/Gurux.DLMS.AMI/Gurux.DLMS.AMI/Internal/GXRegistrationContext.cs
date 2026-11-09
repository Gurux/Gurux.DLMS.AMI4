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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Authorizes fixed registration defaults without changing the user's stored roles or sign-in principal.</summary>
internal sealed class GXRegistrationContext : IDisposable
{
    private readonly HttpContext context;
    private readonly ClaimsPrincipal original;
    private bool disposed;

    private GXRegistrationContext(HttpContext context, ClaimsPrincipal owner)
    {
        this.context = context;
        original = context.User;
        var provisioning = new ClaimsPrincipal(owner.Identities.Select(identity => new ClaimsIdentity(identity)));
        var identity = provisioning.Identities.FirstOrDefault(candidate => candidate.IsAuthenticated)
            ?? throw new InvalidOperationException("Default provisioning requires an authenticated owner identity.");
        if (!provisioning.IsInRole(GXRoles.Admin))
        {
            identity.AddClaim(new Claim(identity.RoleClaimType, GXRoles.Admin));
        }
        context.User = provisioning;
    }

    internal static IDisposable Begin(HttpContext context, ClaimsPrincipal owner) => new GXRegistrationContext(context, owner);

    public void Dispose()
    {
        if (!disposed)
        {
            context.User = original;
            disposed = true;
        }
    }
}
