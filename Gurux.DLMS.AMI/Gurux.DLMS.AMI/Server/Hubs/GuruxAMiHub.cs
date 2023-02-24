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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.SignalR;

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>
    /// GuruxAMI Hub sends notification events.
    /// </summary>
    public class GuruxAMiHub : Hub<IGXHubEvents>
    {
        private readonly IGXHost _host;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GuruxAMiHub(IGXHost host)
        {
            _host = host;
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
            await base.OnDisconnectedAsync(exception);
        }
    }
}
