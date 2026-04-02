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
namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Defines a contract for sending email messages asynchronously.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for delivering email messages to the
    /// specified recipient. The method is asynchronous and returns a <see cref="Task"/> that completes when the email
    /// has been sent or the operation has failed.</remarks>
    public interface IAmiEmailSender
    {
        /// <summary>
        /// Sends an email message asynchronously to the specified recipient with the given subject and HTML content.
        /// </summary>
        /// <param name="email">The email address of the recipient. Cannot be null or empty.</param>
        /// <param name="subject">The subject line of the email. Cannot be null or empty.</param>
        /// <param name="htmlMessage">The HTML content of the email body. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
