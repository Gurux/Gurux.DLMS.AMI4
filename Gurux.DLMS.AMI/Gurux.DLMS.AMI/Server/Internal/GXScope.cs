//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Scope Requirement.
    /// </summary>
    public class ScopeRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Issuer.
        /// </summary>
        public string Issuer { get; }
        /// <summary>
        /// Scope.
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// Allowed roles.
        /// </summary>
        public string[]? Roles
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="scope">Allowed scope.</param>
        /// <param name="issuer">Allowed role.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScopeRequirement(string scope, string issuer)
        {
            if (string.IsNullOrEmpty(scope))
            {
                throw new ArgumentNullException(nameof(scope));
            }
            Scopes = new string[] { scope };
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="scopes">Allowed scopes.</param>
        /// <param name="issuer">Allowed role.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScopeRequirement(string[] scopes, string issuer)
        {
            Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
    }

    internal class ScopedAccessHandler : AuthorizationHandler<ScopeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
        {
            if (requirement.Roles != null)
            {
                foreach (string role in requirement.Roles)
                {
                    if (context.User.IsInRole(role))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }
            //Verify issuer.
            if (!context.User.HasClaim(c => c.Type == "iss" && c.Value == requirement.Issuer))
            {
                return Task.CompletedTask;
            }

            // Split the scopes string into an array.
            foreach (var scope in requirement.Scopes)
            {
                var tmp = context.User.FindFirst(c => c.Type == "scope" && c.Value == scope);
                if (tmp != null)
                {
                    context.Succeed(requirement);
                    break;
                }
            }
            return Task.CompletedTask;
        }
    }
}
