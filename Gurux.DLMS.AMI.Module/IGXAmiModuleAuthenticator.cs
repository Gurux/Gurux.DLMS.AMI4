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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Authenticates requests to HTTP endpoints owned by the registering module.</summary>
/// <remarks>
/// Register using <see cref="AmiModuleBuilder.AddAuthenticator{T}"/>. The host creates the
/// authenticator in the module's request scope and cancels it when the request ends or the
/// module stops. Validate credentials, expiry, issuer, audience and revocation as applicable
/// on every request. Return <see cref="AmiAuthenticationResult.NotHandled"/> only when
/// credentials are absent or do not apply to this authenticator; invalid applicable credentials
/// must return <see cref="AmiAuthenticationResult.Rejected"/>. The host additionally validates
/// the mapped host user and restricts permissions and roles to that user's current grants.
/// Do not write a response body or retain the request after this method completes.
/// </remarks>
public interface IGXAmiModuleAuthenticator
{
    /// <summary>Validates credentials for the module-owned endpoint selected for this request.</summary>
    /// <param name="request">HTTP request and scoped module services.</param>
    /// <param name="cancellationToken">Cancellation for the request and module lifetime.</param>
    /// <returns>The validated principal, an explicit rejection, or a request to use host authentication.</returns>
    Task<AmiAuthenticationResult> AuthenticateAsync(AmiModuleRequest request, CancellationToken cancellationToken);
}
