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

using System.Net;
using System.Text.Json;
using Duende.IdentityServer.Extensions;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class handles the maintenance mode.
    /// </summary>
    internal sealed class GXMaintenanceMidleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfigurationRepository _configurationRepository;
        private MaintenanceSettings? settings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMaintenanceMidleware(
            RequestDelegate next,
            IConfigurationRepository configurationRepository)
        {
            _next = next;
            _configurationRepository = configurationRepository;
            ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Maintenance } };
            var ret = _configurationRepository.ListAsync(null, req, null, CancellationToken.None).Result;
            if (ret != null && ret.Length == 1 && !string.IsNullOrEmpty(ret[0].Settings))
            {
                settings = JsonSerializer.Deserialize<MaintenanceSettings>(ret[0].Settings);
            }
            _configurationRepository.Updated += async (configurations) =>
                {
                    //If maintenance configuration is updated.
                    foreach (GXConfiguration it in configurations)
                    {
                        if (it.Name == GXConfigurations.Maintenance && it.Settings != null)
                        {
                            settings = JsonSerializer.Deserialize<MaintenanceSettings>(it.Settings);
                            break;
                        }
                    }
                };
        }

        public async Task Invoke(HttpContext context)
        {
            if (settings != null && settings.MaintenanceMode &&
                context.User.IsAuthenticated() && !context.User.IsInRole(GXRoles.Admin))
            {
                //Server is in maintenance mode. Retry after given time.
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                int delay = 0;
                if (settings.EndTime != null && settings.EndTime > DateTime.Now)
                {
                    //If maintenance end time is given.
                    delay = (int)(settings.EndTime.Value - DateTime.Now).TotalSeconds;
                }
                context.Response.Headers.RetryAfter = delay.ToString();
                ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Maintenance } };
                var ret = await _configurationRepository.ListAsync(context.User, req, null, CancellationToken.None);
                if (ret.Length == 1)
                {
                    if (!string.IsNullOrEmpty(settings.Message))
                    {
                        await context.Response.WriteAsync(settings.Message);
                    }
                }
                return;
            }
            await _next(context);
        }
    }
}
