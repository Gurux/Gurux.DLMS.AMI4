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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle gateways.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle gateways.")]
    public interface IGatewayRepository
    {
        /// <summary>
        /// List gateways.
        /// </summary>
        /// <returns>Gateways.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List gateways.")]
        Task<IEnumerable<GXGateway>> ListAsync(
            ListGateways? request = null,
            ListGatewaysResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read gateway.
        /// </summary>
        /// <param name="id">Gateway id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested gateway.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read gateway.")]
        Task<GXGateway> ReadAsync(Guid id, Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new gateways or updates existing ones.
        /// </summary>
        /// <param name="gateways">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new gateways or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete gateways.</summary>
        /// <param name="gateways">Gateway(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete gateways.</returns>
        [System.ComponentModel.Description("Delete gateways.")]
        Task DeleteAsync(IEnumerable<Guid> gateways, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this gateway.
        /// </summary>
        /// <param name="gatewayId">Gateway id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this gateway.")]
        Task<List<string>> GetUsersAsync(Guid? gatewayId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access gateways.
        /// </summary>
        /// <param name="Ids">Gateway ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access gateways.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gateway updates the status.
        /// </summary>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="status">Gateway status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to update the gateway status.</returns>
        [System.ComponentModel.Description("Gateway updates the status.")]
        Task UpdateStatusAsync(Guid gatewayId, GatewayStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset gateways to offline.
        /// </summary>
        /// <param name="gateways">Resetted gateway(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to reset gateways to offline.</returns>
        [System.ComponentModel.Description("Reset gateways to offline.")]
        Task ResetAsync(IEnumerable<Guid> gateways, CancellationToken cancellationToken = default);
    }
}
