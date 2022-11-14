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
    /// This class implements object repository that can be called from the agent script.
    /// </summary>
    class GXObjectRepository : IObjectRepository
    {

        /// <inheritdoc/>
        public async Task DeleteAsync(ClaimsPrincipal? user, IEnumerable<Guid> devices)
        {
            ObjectDelete req = new ObjectDelete() { Ids = devices.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<ObjectDeleteResponse>("/api/Object/Delete", req);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? objectId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? objectIds)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<GXObject[]> ListAsync(
            ClaimsPrincipal User, 
            ListObjects? request, 
            ListObjectsResponse? response,
            CancellationToken cancellationToken)
        {
            ListObjectsResponse? ret = await GXAgentWorker.client.PostAsJson<ListObjectsResponse>("/api/Object/List", 
                request, cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Objects = ret.Objects;
            }
            return ret.Objects;
        }

        /// <inheritdoc/>
        public async Task<GXObject> ReadAsync(ClaimsPrincipal? User, Guid id)
        {
            return await Helpers.GetAsync<GXObject>(string.Format("/api/Object/?Id={0}", id));
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(ClaimsPrincipal? user, IEnumerable<GXObject> devices)
        {
            UpdateObject req = new UpdateObject() { Objects = devices.ToArray() };
            return (await GXAgentWorker.client.PostAsJson<UpdateObjectResponse>("/api/Object/Update", req)).Ids;
        }

        /// <inheritdoc/>
        public async Task UpdateDatatypeAsync(ClaimsPrincipal user, IEnumerable<GXAttribute> attributes)
        {
            UpdateDatatype req = new UpdateDatatype() { Items = attributes.ToArray() };
            _ = await GXAgentWorker.client.PostAsJson<UpdateDatatypeResponse>("/api/Attribute/UpdateDatatype", req);
        }
    }
}