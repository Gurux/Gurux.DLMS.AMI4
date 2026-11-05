// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements object repository that can be called from the agent script.
    /// </summary>
    class GXObjectRepository(HttpClient http) : IObjectRepository
    {
        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> devices, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            RemoveObject req = new() { Ids = [.. devices], Delete = delete };
            _ = await http.PostAsJson<RemoveObjectResponse>("/api/Object/Delete", req, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(Guid? objectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(IEnumerable<Guid>? objectIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXObject>> ListAsync(
            ListObjects? request,
            ListObjectsResponse? response,
            CancellationToken cancellationToken)
        {
            ListObjectsResponse? ret = await http.PostAsJson<ListObjectsResponse>("/api/Object/List",
                request ?? new ListObjects(), cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Objects = ret.Objects;
            }
            return ret?.Objects ?? [];
        }

        /// <inheritdoc/>
        public async Task<GXObject> ReadAsync(Guid id, Expression<Func<GXObject, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            return await http.GetAsJsonAsync<GXObject>(string.Format("/api/Object/?Id={0}", id), cancellationToken)
                ?? throw new KeyNotFoundException($"Item {id} was not returned.");
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXObject> items,
            Expression<Func<GXObject, object>>? columns = null,
            CancellationToken cancellationToken = default)
            => UpsertAsync(items, columns, cancellationToken);

        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXObject> objects,
             Expression<Func<GXObject, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            UpdateObject req = new() { Objects = [.. objects] };
            return (await http.PostAsJson<UpdateObjectResponse>("/api/Object/Update", req, cancellationToken))?.Ids
                ?? throw new HttpRequestException("Update response was not returned.");
        }

        /// <inheritdoc/>
        public async Task UpdateDatatypeAsync(IEnumerable<GXAttribute> attributes)
        {
            // Preserve the public instance method used by repository consumers.
            _ = this;
            UpdateDatatype req = new() { Items = [.. attributes] };
            _ = await http.PostAsJson<UpdateDatatypeResponse>("/api/Attribute/UpdateDatatype", req);
        }
    }
}
