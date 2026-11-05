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

/// <summary>Optional QRCode module capabilities. Disabling enrollment never removes existing Identity protection.</summary>
/// <param name="TotpEnrollmentEnabled">Whether enrollment of time-based one-time passwords is enabled.</param>
/// <param name="PhoneApprovalEnabled">Whether phone approval for login is enabled.</param>
public sealed record AmiQrLoginFeatures(bool TotpEnrollmentEnabled, bool PhoneApprovalEnabled);

/// <summary>A new login request. BrowserKey must remain in an HttpOnly browser cookie.</summary>
/// <param name="Id">The identifier of the login request.</param>
/// <param name="BrowserKey">The initiating browser secret, which must remain in an HttpOnly cookie.</param>
/// <param name="VerificationNumber">The number displayed in the initiating browser for the phone user to enter.</param>
/// <param name="ExpiresAt">The time at which the request expires.</param>
public sealed record AmiQrLoginChallenge(string Id, string BrowserKey, string VerificationNumber, DateTimeOffset ExpiresAt);

/// <summary>Information visible only to the matching signed-in phone user.</summary>
/// <param name="RemoteAddress">The remote address associated with the initiating login.</param>
/// <param name="CreatedAt">The time at which the request was created.</param>
/// <param name="ExpiresAt">The time at which the request expires.</param>
public sealed record AmiQrLoginInfo(string RemoteAddress, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

/// <summary>Consumed login authorization, independent of the module lifetime.</summary>
/// <param name="UserId">The account authorized by the login request.</param>
/// <param name="SecurityStamp">The account security stamp captured for the authorization.</param>
/// <param name="RememberMe">Whether the resulting login should persist beyond the browser session.</param>
/// <param name="ReturnUrl">The optional navigation destination, subject to host validation.</param>
public sealed record AmiQrLoginGrant(string UserId, string SecurityStamp, bool RememberMe, string? ReturnUrl);

/// <summary>Short-lived phone approvals, accessed only through the module dispatcher.</summary>
public interface IGXAmiQrLogin
{
    /// <summary>Gets the administrator-enabled features.</summary>
    /// <returns>The current feature switches configured by the administrator.</returns>
    AmiQrLoginFeatures GetFeatures();
    /// <summary>Creates a request after the host has verified the password.</summary>
    /// <param name="grant">The login authorization created after host password verification.</param>
    /// <param name="remoteAddress">The remote address of the initiating browser.</param>
    /// <returns>The new challenge, or <see langword="null"/> when no challenge can be created.</returns>
    AmiQrLoginChallenge? Create(AmiQrLoginGrant grant, string remoteAddress);
    /// <summary>Reads a pending request using the initiating browser's secret.</summary>
    /// <param name="id">The login request identifier.</param>
    /// <param name="browserKey">The secret held by the initiating browser.</param>
    /// <returns>The matching pending challenge, or <see langword="null"/> when unavailable.</returns>
    AmiQrLoginChallenge? GetForBrowser(string id, string browserKey);
    /// <summary>Reads a request only for its matching authenticated account.</summary>
    /// <param name="id">The login request identifier.</param>
    /// <param name="userId">The authenticated phone user account identifier.</param>
    /// <returns>Request details for the matching account, or <see langword="null"/> when unavailable.</returns>
    AmiQrLoginInfo? GetForApproval(string id, string userId);
    /// <summary>Approves once after the phone user enters the number displayed on the initiating browser.</summary>
    /// <param name="id">The login request identifier.</param>
    /// <param name="userId">The authenticated phone user account identifier.</param>
    /// <param name="verificationNumber">The number entered by the phone user from the initiating browser.</param>
    /// <returns><see langword="true"/> if approval succeeds; otherwise, <see langword="false"/>.</returns>
    bool Approve(string id, string userId, string verificationNumber);
    /// <summary>Atomically consumes an approved request once, for its initiating browser.</summary>
    /// <param name="id">The login request identifier.</param>
    /// <param name="browserKey">The secret held by the initiating browser.</param>
    /// <returns>The approved authorization, or <see langword="null"/> when it cannot be consumed. A consumed authorization cannot be retrieved again.</returns>
    AmiQrLoginGrant? Consume(string id, string browserKey);
    /// <summary>Invalidates a pending request when the initiating browser cancels.</summary>
    /// <param name="id">The login request identifier.</param>
    /// <param name="browserKey">The secret held by the initiating browser.</param>
    void Cancel(string id, string browserKey);
}
