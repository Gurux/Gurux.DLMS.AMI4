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
using Gurux.DLMS.AMI.Module.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Module;

public sealed partial class GXModuleRuntime
{
    bool IGXModuleAuthenticationRuntime.CanAuthenticate(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        // Keep selecting the module scheme for stale endpoints. Authentication must fail
        // closed if the module was stopped/replaced after routing selected the endpoint.
        return endpoint?.Metadata.GetMetadata<AmiModuleRouteOwner>() != null &&
            endpoint.Metadata.GetMetadata<GXModuleEndpointGeneration>()?.HasAuthenticator == true;
    }

    async Task<AmiAuthenticationResult> IGXModuleAuthenticationRuntime.AuthenticateAsync(
        HttpContext http, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var endpoint = http.GetEndpoint();
        var owner = endpoint?.Metadata.GetMetadata<AmiModuleRouteOwner>();
        var generation = endpoint?.Metadata.GetMetadata<GXModuleEndpointGeneration>();
        if (owner == null || generation?.HasAuthenticator != true)
        {
            return AmiAuthenticationResult.NotHandled;
        }

        try
        {
            return await CallAsync(owner.ModuleId, async (entry, context, token) =>
            {
                var previousUser = http.User;
                try
                {
                    // Isolate any principal mutations made by the provider. Only its
                    // returned result may establish the authenticated host identity.
                    http.User = new ClaimsPrincipal(previousUser.Identities.Select(identity => new ClaimsIdentity(identity)));
                    if (entry.AuthenticatorType == null)
                    {
                        return AmiAuthenticationResult.Rejected;
                    }

                    var authenticator = (IGXAmiModuleAuthenticator)context.Services.GetRequiredService(entry.AuthenticatorType);
                    var result = await authenticator.AuthenticateAsync(new AmiModuleRequest(http, context), token);
                    token.ThrowIfCancellationRequested();
                    if (result?.Status == AmiAuthenticationStatus.NotHandled)
                    {
                        return AmiAuthenticationResult.NotHandled;
                    }
                    if (result?.Status != AmiAuthenticationStatus.Authenticated ||
                        result.Principal == null || !result.Principal.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        return AmiAuthenticationResult.Rejected;
                    }

                    // Do not retain identities, claims, actors or bootstrap objects supplied by
                    // an unloadable module after its lease has ended.
                    var identities = result.Principal.Identities.Select(identity =>
                    {
                        var copy = new ClaimsIdentity(identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType)
                        {
                            Label = identity.Label
                        };
                        foreach (var claim in identity.Claims)
                        {
                            var detached = new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer);
                            foreach (var property in claim.Properties)
                            {
                                detached.Properties.Add(property.Key, property.Value);
                            }
                            copy.AddClaim(detached);
                        }
                        return copy;
                    });
                    return AmiAuthenticationResult.Authenticated(new ClaimsPrincipal(identities));
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // The generic module fault path logs exceptions. Provider exceptions
                    // may include credentials, so never attach them as an inner exception.
                    throw new InvalidOperationException("Module authentication failed.");
                }
                finally
                {
                    http.User = previousUser;
                }
            }, cancellationToken, generation.Generation, sanitizeFault: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // CallAsync already faults modules for unexpected provider failures. Neither
            // their exceptions nor invalid credentials may enable host authentication.
            cancellationToken.ThrowIfCancellationRequested();
            return AmiAuthenticationResult.Rejected;
        }
    }
}
