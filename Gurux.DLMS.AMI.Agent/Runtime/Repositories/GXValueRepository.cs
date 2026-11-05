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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories
{
    /// <summary>
    /// This class implements value repository that can be called from the agent script.
    /// </summary>
    class GXValueRepository(HttpClient http) : IValueRepository
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXValue> values, CancellationToken cancellationToken = default)
        {
            AddValue req = new() { Values = [.. values] };
            return (await http.PostAsJson<AddValueResponse>("/api/Value/Add", req, cancellationToken))?.Ids
                ?? throw new HttpRequestException("Add response was not returned.");
        }

        /// <inheritdoc/>
        public Task ClearAttributeAsync(IEnumerable<GXAttribute> attributes, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ClearDeviceAsync(IEnumerable<GXDevice> objects, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ClearObjectAsync(IEnumerable<GXObject> objects, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteAsync(IEnumerable<Guid> devices, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXValue>> ListAsync(
            ListValues? request,
            ListValuesResponse? response,
            CancellationToken cancellationToken)
        {
            ListValuesResponse? ret = await http.PostAsJson<ListValuesResponse>("/api/Value/List", request ?? new ListValues(), cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Values = ret.Values;
            }
            return ret?.Values ?? [];
        }

        /// <inheritdoc/>
        public async Task<GXValue> ReadAsync(Guid id, Expression<Func<GXValue, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            return await http.GetAsJsonAsync<GXValue>(string.Format("/api/Value/?Id={0}", id), cancellationToken)
                ?? throw new KeyNotFoundException($"Item {id} was not returned.");
        }
    }
}
