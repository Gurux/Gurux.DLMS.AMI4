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

using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// GXBlackListMidleware handles black listed IP addresses.
    /// </summary>
    /// <remarks>
    /// Connection are disallowed, if IP address is added for the black list.
    /// </remarks>
    internal sealed class GXBlackListMidleware
    {
        private readonly ILogger<GXBlackListMidleware> _logger;

        private readonly RequestDelegate _next;
        private readonly BlockService _blockService;

        public GXBlackListMidleware(
            ILogger<GXBlackListMidleware> logger,
            RequestDelegate next,
            BlockService blockService)
        {
            _logger = logger;
            _next = next;
            _blockService = blockService;
        }

        public async Task Invoke(HttpContext context)
        {
            if (await _blockService.IsBlockedAsync(context.Connection?.RemoteIpAddress, ServerHelpers.GetUserId(context.User, false)))
            {
                _logger.LogInformation("Logging rejected from address:{0}.", context.Connection?.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }
            await _next(context);
        }
    }
}
