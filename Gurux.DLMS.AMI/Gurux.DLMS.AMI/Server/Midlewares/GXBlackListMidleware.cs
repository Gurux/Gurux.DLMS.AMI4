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

using Gurux.DLMS.AMI.Shared.DIs;

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
        private readonly IBlackListRepository _blackListRepository;

        public GXBlackListMidleware(
            ILogger<GXBlackListMidleware> logger, 
            RequestDelegate next, 
            IBlackListRepository blackListRepository)
        {
            _logger = logger;
            _next = next;
            _blackListRepository = blackListRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            byte[] bytes = context.Connection.RemoteIpAddress.GetAddressBytes();
            UInt64 ipAddress;
            if (bytes.Length == 4)
            {
                ipAddress = BitConverter.ToUInt32(bytes);
            }
            else
            {
                ipAddress = BitConverter.ToUInt64(bytes);
            }
            if (_blackListRepository.IsBlocked(ipAddress))
            {
                _logger.LogInformation("Logging rejected from address:{0}.", ipAddress);
                return;
            }
            await _next(context);
        }      
    }
}
