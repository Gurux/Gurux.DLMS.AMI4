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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// REST statistic information.
    /// </summary>
    public interface IRestStatisticRepository
    {
        /// <summary>
        /// List from the REST statistics.
        /// </summary>
        /// <returns>Statistics.</returns>
        Task<GXRestStatistic[]> ListAsync(
            ClaimsPrincipal user,
            ListRestStatistics? request,
            ListRestStatisticsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Add new REST statistics.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="statistics">Added statistics.</param>
        Task<Guid[]> AddAsync(ClaimsPrincipal user, IEnumerable<GXRestStatistic> statistics);

        /// <summary>
        /// Clear statistics.
        /// </summary>
        /// <param name="user">Current user.</param>
        Task ClearAsync(ClaimsPrincipal user);
    }
}
