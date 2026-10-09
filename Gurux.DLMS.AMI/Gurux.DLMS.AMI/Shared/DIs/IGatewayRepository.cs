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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle gateways.
    /// </summary>
    public interface IGatewayRepository
    {
        /// <summary>
        /// List gateways.
        /// </summary>
        /// <returns>Gateways.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXGateway>> ListAsync(
            ListGateways? request = null,
            ListGatewaysResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read gateway.
        /// </summary>
        /// <param name="id">Gateway id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXGateway> ReadAsync(Guid id, Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update gateway(s).
        /// </summary>
        /// <param name="gateways">Updated gateway(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete gateway(s).
        /// </summary>
        /// <param name="gateways">Gateway(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> gateways, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this gateway.
        /// </summary>
        /// <param name="gatewayId">Gateway id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? gatewayId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access gateways.
        /// </summary>
        /// <param name="Ids">Gateway ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gateway updates the status.
        /// </summary>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="status">Gateway status</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateStatusAsync(Guid gatewayId, GatewayStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset gateways to offline.
        /// </summary>
        /// <param name="gateways">Resetted gateway(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ResetAsync(IEnumerable<Guid> gateways, CancellationToken cancellationToken = default);
    }
}
