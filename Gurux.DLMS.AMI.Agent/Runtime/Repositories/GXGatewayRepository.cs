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

using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Gateway.Worker.Repositories
{
    /// <summary>
    /// This class implements gateway repository that can be called from the gateway script.
    /// </summary>
    class GXGatewayRepository(HttpClient http) : IGatewayRepository
    {
        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> devices, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            RemoveGateway req = new() { Ids = [.. devices], Delete = delete };
            _ = await http.PostAsJson<RemoveGatewayResponse>("/api/Gateway/Delete", req, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(Guid? deviceId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<string>> GetUsersAsync(IEnumerable<Guid>? gatewayIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXGateway>> ListAsync(
            ListGateways? request,
            ListGatewaysResponse? response,
            CancellationToken cancellationToken)
        {
            ListGatewaysResponse? ret = await http.PostAsJson<ListGatewaysResponse>("/api/Gateway/List",
                request ?? new ListGateways(), cancellationToken);
            if (response != null && ret != null)
            {
                response.Count = ret.Count;
                response.Gateways = ret.Gateways;
            }
            return ret?.Gateways ?? [];
        }

        /// <inheritdoc/>
        public async Task<GXGateway> ReadAsync(Guid id, Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            return await http.GetAsJsonAsync<GXGateway>(string.Format("/api/Gateway/?Id={0}", id), cancellationToken)
                ?? throw new KeyNotFoundException($"Item {id} was not returned.");
        }

        /// <inheritdoc/>
        public Task ResetAsync(IEnumerable<Guid> gateways, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXGateway> items,
            Expression<Func<GXGateway, object>>? columns = null,
            CancellationToken cancellationToken = default)
            => UpsertAsync(items, columns, cancellationToken);

        /// <inheritdoc/>
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            UpdateGateway req = new() { Gateways = [.. gateways] };
            return (await http.PostAsJson<UpdateGatewayResponse>("/api/Gateway/Update", req, cancellationToken))?.Ids
                ?? throw new HttpRequestException("Update response was not returned.");
        }

        /// <inheritdoc/>
        public Task UpdateStatusAsync(Guid gatewayId, GatewayStatus status, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
