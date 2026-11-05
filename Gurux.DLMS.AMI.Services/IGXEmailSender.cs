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

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// Defines a contract for sending email messages asynchronously.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for delivering email messages to the
    /// specified recipient. The method is asynchronous and returns a <see cref="Task"/> that completes when the email
    /// has been sent or the operation has failed.</remarks>
    public interface IGXAmiEmailSender
    {
        /// <summary>
        /// Sends an email message asynchronously to the specified recipient with the given subject and HTML content.
        /// </summary>
        /// <param name="email">The email address of the recipient. Cannot be null or empty.</param>
        /// <param name="subject">The subject line of the email. Cannot be null or empty.</param>
        /// <param name="htmlMessage">The HTML content of the email body. Cannot be null or empty.</param>
        /// <param name="cancellationToken">Cancels the active send when the caller or module stops.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendEmailAsync(string email, string subject, string htmlMessage, CancellationToken cancellationToken = default);
    }
}
