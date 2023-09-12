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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
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
        Task<GXGateway[]> ListAsync(
            ClaimsPrincipal User,
            ListGateways? request,
            ListGatewaysResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read gateway.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Gateway id.</param>
        /// <returns></returns>
        Task<GXGateway> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update gateway(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gateways">Updated gateway(s).</param>
        /// <param name="columns">Updated column(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object?>>? columns = null);

        /// <summary>
        /// Delete gateway(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gateways">Gateway(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> gateways, bool delete);

        /// <summary>
        /// Get all users that can access this gateway.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayId">Gateway id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? gatewayId);

        /// <summary>
        /// Get all users that can access gateways.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayIds">Gateway ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? gatewayIds);

        /// <summary>
        /// Gateway updates the status.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="status">Gateway status</param>
        Task UpdateStatusAsync(ClaimsPrincipal User, Guid gatewayId, GatewayStatus status);
    }
}
