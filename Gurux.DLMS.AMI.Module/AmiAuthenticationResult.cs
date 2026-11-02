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
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Result of module authentication, without credentials or sensitive failure details.</summary>
public sealed class AmiAuthenticationResult
{
    private AmiAuthenticationResult(AmiAuthenticationStatus status, ClaimsPrincipal? principal = null)
    {
        Status = status;
        Principal = principal;
    }

    /// <summary>Whether authentication succeeded, was rejected, or should use host authentication.</summary>
    public AmiAuthenticationStatus Status { get; }

    /// <summary>Validated identity and token grants when <see cref="Status"/> is authenticated.</summary>
    /// <remarks>The host copies the claims before releasing the module scope and validates the mapped host user.</remarks>
    public ClaimsPrincipal? Principal { get; }

    /// <summary>No applicable credentials were supplied; the host may use its normal authentication.</summary>
    public static AmiAuthenticationResult NotHandled { get; } = new(AmiAuthenticationStatus.NotHandled);

    /// <summary>Applicable credentials are invalid; the host must reject without authentication fallback.</summary>
    public static AmiAuthenticationResult Rejected { get; } = new(AmiAuthenticationStatus.Rejected);

    /// <summary>Creates a successful result for a principal whose credentials have been validated.</summary>
    /// <param name="principal">Authenticated principal containing the host user identifier and restricted grants.</param>
    /// <returns>A successful module authentication result.</returns>
    /// <exception cref="ArgumentNullException">The principal is null.</exception>
    /// <exception cref="ArgumentException">The principal has no authenticated identity.</exception>
    public static AmiAuthenticationResult Authenticated(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (!principal.Identities.Any(identity => identity.IsAuthenticated))
        {
            throw new ArgumentException("The principal must contain an authenticated identity.", nameof(principal));
        }
        return new(AmiAuthenticationStatus.Authenticated, principal);
    }
}
