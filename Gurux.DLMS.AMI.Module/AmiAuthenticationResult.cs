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
