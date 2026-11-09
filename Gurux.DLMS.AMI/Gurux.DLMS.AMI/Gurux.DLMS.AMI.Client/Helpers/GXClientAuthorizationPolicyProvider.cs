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

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Resolves local UI policies and otherwise delegates named authorization to the server.</summary>
public sealed class GXClientAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var local = await base.GetPolicyAsync(policyName);
        return local ?? new AuthorizationPolicyBuilder()
            .AddRequirements(new GXClientAuthorizationRequirement(policyName)).Build();
    }
}

/// <summary>A server-evaluated named policy; it does not require authentication by itself.</summary>
public sealed record GXClientAuthorizationRequirement(string PolicyName) : IAuthorizationRequirement;
