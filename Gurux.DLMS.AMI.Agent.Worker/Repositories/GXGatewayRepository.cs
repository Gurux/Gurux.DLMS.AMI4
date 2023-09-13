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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Gateway.Worker.Repositories
{
    /// <summary>
    /// This class implements gateway repository that can be called from the gateway script.
    /// </summary>
    class GXGatewayRepository : IGatewayRepository
    {
        /// <inheritdoc/>
        public async Task DeleteAsync(ClaimsPrincipal? user, IEnumerable<Guid> devices, bool delete)
        {
            RemoveGateway req = new RemoveGateway() { Ids = devices.ToArray(), Delete = delete };
            _ = await GXAgentWorker.client.PostAsJson<RemoveGatewayResponse>("/api/Gateway/Delete", req);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal? user, Guid? deviceId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? gatewayIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXGateway[]> ListAsync(
            ClaimsPrincipal User,
            ListGateways? request,
            ListGatewaysResponse? response,
            CancellationToken cancellationToken)
        {
            ListGatewaysResponse? ret = await GXAgentWorker.client.PostAsJson<ListGatewaysResponse>("/api/Gateway/List",
                request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Gateways = ret.Gateways;
            }
            return ret.Gateways;
        }      

        /// <inheritdoc/>
        public async Task<GXGateway> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return await Helpers.GetAsync<GXGateway>(string.Format("/api/Gateway/?Id={0}", id));
        }

        /// <inheritdoc/>
        public Task ResetAsync(ClaimsPrincipal user, IEnumerable<Guid> gateways)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(ClaimsPrincipal User, IEnumerable<GXGateway> gateways, Expression<Func<GXGateway, object?>>? columns = null)
        {
            UpdateGateway req = new UpdateGateway() { Gateways = gateways.ToArray() };
            return (await GXAgentWorker.client.PostAsJson<UpdateGatewayResponse>("/api/Gateway/Update", req)).GatewayIds;
        }

        /// <inheritdoc/>
        public Task UpdateStatusAsync(ClaimsPrincipal User, Guid gatewayId, GatewayStatus status)
        {
            throw new NotImplementedException();
        }
    }
}