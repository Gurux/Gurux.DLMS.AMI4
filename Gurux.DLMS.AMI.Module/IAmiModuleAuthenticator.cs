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
public interface IAmiModuleAuthenticator
{
    /// <summary>Validates credentials for the module-owned endpoint selected for this request.</summary>
    /// <param name="request">HTTP request and scoped module services.</param>
    /// <param name="cancellationToken">Cancellation for the request and module lifetime.</param>
    /// <returns>The validated principal, an explicit rejection, or a request to use host authentication.</returns>
    Task<AmiAuthenticationResult> AuthenticateAsync(AmiModuleRequest request, CancellationToken cancellationToken);
}
