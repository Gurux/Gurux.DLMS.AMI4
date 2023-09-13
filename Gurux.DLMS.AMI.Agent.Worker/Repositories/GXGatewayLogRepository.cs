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

using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Gateway.Worker.Repositories
{
    /// <summary>
    /// This class implements device log repository that can be called from the gateway script.
    /// </summary>
    class GXGatewayLogRepository : IGatewayLogRepository
    {
        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal? User, IEnumerable<GXGatewayLog> logs)
        {
            AddGatewayLog req = new AddGatewayLog() { Logs = logs.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddGatewayLogResponse>("/api/GatewayLog/Add", req);
        }

        /// <inheritdoc/>
        public Task<GXGatewayLog> AddAsync(ClaimsPrincipal? User, GXGateway device, Exception ex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task ClearAsync(ClaimsPrincipal User, Guid[] gateways)
        {
            ClearGatewayLogs req = new ClearGatewayLogs()
            {
                Gateways = gateways != null ? gateways.ToArray() : null
            };
            _ = await GXAgentWorker.client.PostAsJson<ClearGatewayLogsResponse>("/api/GatewayLog/Clear", req);
        }

        /// <inheritdoc/>
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> logs)
        {
            CloseGatewayLog req = new CloseGatewayLog() { Logs = logs.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<AddGatewayLogResponse>("/api/GatewayLog/Close", req);
        }

        /// <inheritdoc/>
        public async Task<GXGatewayLog[]> ListAsync(
            ClaimsPrincipal User,
            ListGatewayLogs? request,
            ListGatewayLogsResponse? response,
            CancellationToken cancellationToken)
        {
            ListGatewayLogsResponse? ret = await GXAgentWorker.client.PostAsJson<ListGatewayLogsResponse>("/api/GatewayLog/List",
                request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Logs = ret.Logs;
            }
            return ret.Logs;
        }

        /// <inheritdoc/>
        public Task<GXGatewayLog?> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return Helpers.GetAsync<GXGatewayLog>(string.Format("/api/GatewayLog/?Id={0}", id));
            /*TODO:
            GetGatewayLogResponse ret = Helpers.GetAsync<GetGatewayLogResponse>(string.Format("/api/GatewayLog/?Id={0}", id));
            if (ret == null)
            {
                //Read using old way.
                return Helpers.GetAsync<GXGatewayLog>(string.Format("/api/GatewayLog/?Id={0}", id));
            }
            else
            {
                return ret.Item;
            }
            */
        }
    }
}