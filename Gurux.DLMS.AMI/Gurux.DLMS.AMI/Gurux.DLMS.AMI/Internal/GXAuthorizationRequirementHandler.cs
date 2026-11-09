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

namespace Gurux.DLMS.AMI.Server.Internal
{
    using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
    using Gurux.DLMS.AMI.Shared.Enums;
    using Microsoft.AspNetCore.Authorization;
    using System.Security.Claims;

    /// <summary>
    /// Handles GX authorization requirements.
    /// </summary>
    public sealed class GXAuthorizationRequirementHandler
        : AuthorizationHandler<GXAuthorizationRequirement>
    {
        /// <inheritdoc />
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GXAuthorizationRequirement requirement)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(requirement);

            if (requirement.Requirements.Count == 0)
            {
                return Task.CompletedTask;
            }

            // Authentication is mandatory, never an alternative to a permission in an OR policy.
            bool authenticationRequired = requirement.Requirements.Any(x => x.Type == PolicyRequirementType.AuthenticatedUser);
            if (authenticationRequired && context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            var permissions = requirement.Requirements.Where(x => x.Type != PolicyRequirementType.AuthenticatedUser).ToArray();
            bool succeeded = permissions.Length == 0 ? authenticationRequired : requirement.Operator switch
            {
                PolicyRequirementOperator.And =>
                    permissions.All(
                        x => IsRequirementSatisfied(context.User, x)),
                PolicyRequirementOperator.Or =>
                    permissions.Any(
                        x => IsRequirementSatisfied(context.User, x)),

                _ => false
            };

            if (succeeded)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks whether the user satisfies the given policy requirement.
        /// </summary>
        private static bool IsRequirementSatisfied(
            ClaimsPrincipal user,
            GXPolicyRequirement requirement)
        {
            return requirement.Type switch
            {
                PolicyRequirementType.AnyUser => true,
                PolicyRequirementType.Permission =>
                    HasPermission(user, requirement.Value),
                PolicyRequirementType.Role =>
                    HasRole(user, requirement.Value),
                PolicyRequirementType.Claim =>
                    HasClaim(user, requirement.ClaimType, requirement.Value),
                PolicyRequirementType.AuthenticatedUser =>
                    user.Identity?.IsAuthenticated == true,
                _ => false
            };
        }

        /// <summary>
        /// Checks whether the user has the specified permission.
        /// </summary>
        private static bool HasPermission(
            ClaimsPrincipal user,
            string? permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }
            return user.HasClaim(
                PolicyRequirementType.Permission.ToString(),
                permission);
        }

        /// <summary>
        /// Checks whether the user belongs to the specified role.
        /// </summary>
        private static bool HasRole(
            ClaimsPrincipal user,
            string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            return user.IsInRole(role);
        }

        /// <summary>
        /// Checks whether the user has the specified claim.
        /// </summary>
        private static bool HasClaim(
            ClaimsPrincipal user,
            string? name,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return user.HasClaim(x =>
                    string.Equals(
                        x.Type,
                        name,
                        StringComparison.Ordinal));
            }

            return user.HasClaim(
                name,
                value);
        }
    }
}
