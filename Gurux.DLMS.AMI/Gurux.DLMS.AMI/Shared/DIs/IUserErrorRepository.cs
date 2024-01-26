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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user errors.
    /// </summary>
    public interface IUserErrorRepository
    {
        /// <summary>
        /// List user errors.
        /// </summary>
        /// <returns>List of user errors.</returns>
        Task<GXUserError[]> ListAsync(
            ClaimsPrincipal user, 
            ListUserErrors? request, 
            ListUserErrorsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read user error information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">User error id.</param>
        /// <returns>User error information.</returns>
        Task<GXUserError> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Clear user errors.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal user, IEnumerable<string>? users);

        /// <summary>
        /// Add user errors.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="errors">New errors.</param>
        Task AddAsync(ClaimsPrincipal user, IEnumerable<GXUserError> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="user">User.</param>
        /// <param name="ex">Exception.</param>
        Task<GXUserError> AddAsync(ClaimsPrincipal User, GXUser user, Exception ex);

        /// <summary>
        /// Close user error(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="errors">Errors to close.</param>
        Task CloseAsync(ClaimsPrincipal user, IEnumerable<Guid> errors);
    }
}
