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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user stamps.
    /// </summary>
    public interface IUserStampRepository
    {
        /// <summary>
        /// List user stamps.
        /// </summary>
        /// <returns>List of user stamps.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXUserStamp>> ListAsync(
            ListUserStamps? request = null,
            ListUserStampsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user stamps.
        /// </summary>
        /// <param name="userStamps">User stamps.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> userStamps, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add user stamps.
        /// </summary>
        /// <param name="userStamps">New user stamps.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateAsync(IEnumerable<GXUserStamp> userStamps, CancellationToken cancellationToken = default);
    }
}
