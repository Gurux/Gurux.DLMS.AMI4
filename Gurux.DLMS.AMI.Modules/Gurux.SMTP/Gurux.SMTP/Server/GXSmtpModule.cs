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
using Gurux.SMTP.Shared;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;

namespace Gurux.SMTP.Server
{
    /// <summary>
    /// SMTP email module settings.
    /// </summary>
    public class GXSmtpModule : IAmiModule
    {
        /// <inheritdoc />
        public string Name => "Smtp";

        /// <inheritdoc />
        public string Icon => "oi oi-envelope-closed";

        /// <inheritdoc />
        public Type Configuration
        {
            get
            {
                return typeof(Client.Pages.Configuration);
            }
        }

        /// <inheritdoc />
        public string Description => Properties.Resources.SMTPSettings;

        /// <inheritdoc />
        public void ConfigureModuleServices(IServiceCollection services, 
            IConfiguration configuration)
        {
            services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        }

        /// <inheritdoc />
        public void ConfigureFrameworkServices(IServiceCollection services)
        {
            services.AddTransient<IEmailSender, GXSmtpSender>();
        }

        /// <inheritdoc />
        public void ConfigureMidlewares(IApplicationBuilder builder)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Install(ClaimsPrincipal user, IServiceProvider services, object module)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Update(ClaimsPrincipal user, IServiceProvider services, object current, object updated)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Uninstall(ClaimsPrincipal user, IServiceProvider services, object module)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Start(IServiceProvider services)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Execute(IServiceProvider services)
        {
            //Nothing to do for SMTP.
        }

        /// <inheritdoc />
        public void Stop(IServiceProvider services)
        {
            //Nothing to do for SMTP.
        }

    }
}
