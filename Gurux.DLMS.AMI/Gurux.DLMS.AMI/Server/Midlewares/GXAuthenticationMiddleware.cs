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

using System.Security.Claims;
using Duende.IdentityServer.Validation;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using static System.Net.Mime.MediaTypeNames;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        private IAuthenticationSchemeProvider _schemes { get; set; }

        /// <summary>
        /// Get scheme and credential from Authorization header.
        /// </summary>
        /// <param name="context">Http content.</param>
        /// <param name="scheme">Found scheme.</param>
        /// <param name="token">Found token.</param>
        public static void GetSchemeAndCredential(HttpContext context, out string scheme, out string token)
        {
            string header = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                string[] tmp = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length == 2)
                {
                    scheme = tmp[0];
                    token = tmp[1];
                    return;
                }
            }
            scheme = "";
            token = "";
        }

        /// <summary>
        /// Is client sending the reference token. 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsReferenceToken(HttpContext context, out string token)
        {
            string scheme;
            GetSchemeAndCredential(context, out scheme, out token);
            if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) &&
                !token.Contains('.'))
            {
                return true;
            }
            return false;
        }

        public GXAuthenticationMiddleware(
            RequestDelegate next,
            IAuthenticationSchemeProvider schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (schemes == null)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            _next = next;
            _schemes = schemes;
        }

        public async Task Invoke(HttpContext context,
            ITokenValidator tokenValidator,
            IAuthenticationHandlerProvider authenticationHandlerProvider)
        {
            //If user is not authenticated yet.
            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                {
                    OriginalPath = context.Request.Path,
                    OriginalPathBase = context.Request.PathBase
                });

                // Give any IAuthenticationRequestHandler schemes a chance to handle the request
                foreach (var scheme in await _schemes.GetRequestHandlerSchemesAsync())
                {
                    var handler = await authenticationHandlerProvider.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;
                    if (handler != null && await handler.HandleRequestAsync())
                    {
                        return;
                    }
                }

                var defaultAuthenticate = await _schemes.GetDefaultAuthenticateSchemeAsync();
                if (defaultAuthenticate != null)
                {
                    string token;
                    if (IsReferenceToken(context, out token))
                    {
                        TokenValidationResult ret = await tokenValidator.ValidateAccessTokenAsync(token, "Gurux.DLMS.AMI.ServerAPI");
                        if (!ret.IsError)
                        {
                            // Get user's identity.
                            string? sub = ret.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                            if (string.IsNullOrEmpty(sub))
                            {
                                throw new ArgumentNullException("sub");
                            }
                            // Get user's roles.
                            string[] roles = ret.Claims.Where(c => c.Type == "role").Select(s => s.Value).ToArray();
                            // Get scopes.
                            string[] scopes = ret.Claims.Where(c => c.Type == "scope").Select(s => s.Value).ToArray();
                            ClaimsIdentity claimsIdentity = new(ret.Claims);
                            ClaimsPrincipal claimsPrincipal = new(claimsIdentity);
                            var identity = new ClaimsIdentity(ret.Claims, "token");
                            context.User = new ClaimsPrincipal(identity);
                            var ci = (ClaimsIdentity?)context.User.Identity;
                            if (ci != null)
                            {
                                ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub));
                                foreach (var role in roles)
                                {
                                    ci.AddClaim(new Claim(identity.RoleClaimType, role));
                                }
                                foreach (var scope in scopes)
                                {
                                    ci.AddClaim(new Claim(JwtClaimTypes.Scope, scope));
                                }
                            }
                        }
                    }
                    else
                    {
                        var result = await context.AuthenticateAsync(defaultAuthenticate.Name);
                        if (result?.Principal != null)
                        {
                            context.User = result.Principal;
                        }
                    }
                }
            }
            await _next(context);
        }
    }

}
