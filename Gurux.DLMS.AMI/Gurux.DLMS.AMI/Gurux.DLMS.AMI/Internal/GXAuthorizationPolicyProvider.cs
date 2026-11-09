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

using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>Resolves authorization policies from the AMI policy registry.</summary>
    /// <inheritdoc/>
    public class GXAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IServiceScopeFactory scopeFactory) : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        /// <inheritdoc/>
        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IGXPolicyService service = scope.ServiceProvider.GetRequiredService<IGXPolicyService>();
            GXPolicy? policy = await service.FindAsync(policyName);
            if (policy == null)
            {
                return await _fallback.GetPolicyAsync(policyName)
                    ?? new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();
            }
            AuthorizationPolicyBuilder builder = new();
            if (policy.RequireAuthenticatedUser)
            {
                builder.RequireAuthenticatedUser();
            }

            builder.AddRequirements(
                            new GXAuthorizationRequirement(
                                policy.RequirementOperator,
                                policy.Requirements ?? []));
            return builder.Build();
        }


        /// <inheritdoc/>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        /// <inheritdoc/>
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();
    }
}
