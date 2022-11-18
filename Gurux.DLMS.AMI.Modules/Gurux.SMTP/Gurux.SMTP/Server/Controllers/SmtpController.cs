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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Gurux.SMTP.Shared;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Gurux.DLMS.AMI.Shared;

namespace Gurux.SMTP.Server
{
    [Authorize(Roles = "Admin")]
    [Route("api/Module/[controller]")]
    [ApiController]
    public class SmtpController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IModuleLogRepository _moduleLogRepository;
        private readonly IOptionsSnapshot<SmtpOptions> _smtpOptions;
        public SmtpController(
            IEmailSender emailSender,
            IOptionsSnapshot<SmtpOptions> smtpOptions,
            IModuleLogRepository moduleLogRepository)
        {
            _emailSender = emailSender;
            _moduleLogRepository = moduleLogRepository;
            _smtpOptions = smtpOptions;
        }

        /// <summary>
        /// Send test email using SMTP.
        /// </summary>
        [HttpPost("Test")]
        public async Task<ActionResult> Post(SmtpTestMessage message)
        {
            SmtpOptions options = _smtpOptions.Value;
            try
            {
                MailAddress? address;
                if (string.IsNullOrEmpty(message.Address) ||
                    !MailAddress.TryCreate(message.Address, out address))
                {
                    return BadRequest(Properties.Resources.InvalidEmailAddress);
                }
                string msg = Properties.Resources.IfYouReceiveThisMessageItMeansYourSiteIsCapableOfUsingSMTPToSendEMail;
                string subject = Properties.Resources.GuruxDLMSAMISMTPTestEMail;
                if (message.Options != null)
                {
                    options.Host = message.Options.Host;
                    options.Port = message.Options.Port;
                    options.Email = message.Options.Email;
                    if (!string.IsNullOrEmpty(message.Options.Password))
                    {
                        options.Password = message.Options.Password;
                    }
                }
                var log = new GXModuleLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Message = message + " send to " + message.Address +
                    ", Host: " + options.Host +
                    ", Port: " + options.Port +
                    ", Sender: " + options.Email,
                    Module = new GXModule() { Id = "Smtp" }
                };
                await _moduleLogRepository.AddAsync(User, new GXModuleLog[] { log });
                await _emailSender.SendEmailAsync(address.Address, subject, msg);
                return Ok();
            }
            catch (Exception ex)
            {
                var log = new GXModuleLog()
                {
                    CreationTime = DateTime.Now,
                    Message = ex.Message,
                    Module = new GXModule() { Id = "Smtp" }
                };
                await _moduleLogRepository.AddAsync(User, new GXModuleLog[] { log });
                throw new GXAmiException(ex.Message);
            }
            //There is no reason to return original values because this is a snapshot.           
        }
    }
}
