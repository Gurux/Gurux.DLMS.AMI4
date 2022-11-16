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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.SMTP.Shared;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Gurux.SMTP.Server
{
    /// <summary>
    /// This class sends emails using SMTP protocol.
    /// </summary>
    public class GXSmtpSender : IEmailSender
    {
        private readonly IOptionsSnapshot<SmtpOptions> _smtpOptions;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSmtpSender(IOptionsSnapshot<SmtpOptions> smtpOptions)
        {
            _smtpOptions = smtpOptions;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                //If Smtp is not configured.
                if (_smtpOptions == null || string.IsNullOrEmpty(_smtpOptions.Value.Host))
                {
                    throw new ArgumentException("SMTP server is not configured.");
                }
                NetworkCredential? credentials = null;
                if (!string.IsNullOrEmpty(_smtpOptions.Value.Password))
                {
                    credentials = new NetworkCredential(_smtpOptions.Value.Email, _smtpOptions.Value.Password);
                }
                using (var client = new SmtpClient(_smtpOptions.Value.Host, _smtpOptions.Value.Port))
                {
                    client.UseDefaultCredentials = false;
                    if (credentials != null)
                    {
                        client.Credentials = credentials;
                    }
                    MailMessage msg = new MailMessage(_smtpOptions.Value.Email, email, subject, message);
                    msg.IsBodyHtml = true;
                    await client.SendMailAsync(msg);
                }
            }
            catch (Exception ex)
            {
                throw new GXAmiException("Failed to send SMTP message. " + ex.Message);
            }
        }
    }
}
