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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>
    /// GuruxAMI Hub sends notification events.
    /// </summary>
    public class GuruxAMiHub : Hub<IGXHubEvents>
    {
        private readonly IGXHost _host;
        private readonly IGXConnectionManager _connectionManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GuruxAMiHub(IGXHost host,
            IGXConnectionManager connectionManager)
        {
            _host = host;
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Save occurred exception to the database.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                GXSystemLog log = new GXSystemLog();
                log.Message = exception.Message;
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(log));
            }
            _connectionManager.RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client has establish a connection to the hub.
        /// </summary>
        public override Task OnConnectedAsync()
        {
            //If user is banned.
            var isBanned = Context.User?.HasClaim("banned", "true") == true;
            if (isBanned)
            {
                Context.Abort();
                return Task.CompletedTask;
            }
            var http = Context.GetHttpContext();
            var userId =
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                Context.User?.FindFirst("sub")?.Value;

            var email =
                Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            if (userId == null)
            {
                //Get user id from authorization header.
                //This is used with JWT.
                var httpContext = Context.GetHttpContext();
                var authHeader = httpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);
                    userId = jwt.Claims
                        .FirstOrDefault(c => c.Type == "sub"
                                          || c.Type == ClaimTypes.NameIdentifier)?.Value;
                    //Email address is not included to JWT token.
                }
            }

            var ip = http?.Connection.RemoteIpAddress?.ToString();
            var userAgent = http?.Request.Headers["User-Agent"].ToString();

            var info = new GXConnectionInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                Connected = DateTime.Now,
                Email = email
            };
            _connectionManager.AddConnection(info);
            return base.OnConnectedAsync();
        }
    }
}
