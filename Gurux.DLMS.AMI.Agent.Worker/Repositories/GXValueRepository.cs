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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements value repository that can be called from the agent script.
    /// </summary>
    class GXValueRepository : IValueRepository
    {
        /// <inheritdoc/>
        public async Task<Guid[]> AddAsync(ClaimsPrincipal? User, IEnumerable<GXValue> values)
        {
            AddValue req = new AddValue() { Values = values.ToArray() };
            return (await GXAgentWorker.client.PostAsJson<AddValueResponse>("/api/Value/Update", req)).Ids;
        }

        /// <inheritdoc/>
        public Task ClearAttributeAsync(ClaimsPrincipal User, IEnumerable<GXAttribute> attributes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ClearDeviceAsync(ClaimsPrincipal User, IEnumerable<GXDevice> objects)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ClearObjectAsync(ClaimsPrincipal User, IEnumerable<GXObject> objects)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteAsync(ClaimsPrincipal? user, IEnumerable<Guid> devices)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXValue[]> ListAsync(
            ClaimsPrincipal User, 
            ListValues? request, 
            ListValuesResponse? response,
            CancellationToken cancellationToken)
        {
            ListValuesResponse? ret = await GXAgentWorker.client.PostAsJson<ListValuesResponse>("/api/Value/List", request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Values = ret.Values;
            }
            return ret.Values;
        }

        /// <inheritdoc/>
        public async Task<GXValue> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return await Helpers.GetAsync<GXValue>(string.Format("/api/Value/?Id={0}", id));
        }
    }
}