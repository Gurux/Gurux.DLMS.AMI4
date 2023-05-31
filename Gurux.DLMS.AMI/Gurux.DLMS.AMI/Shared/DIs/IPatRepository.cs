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
    /// This interface is used to handle personal access tokens.
    /// </summary>
    public interface IPatRepository
    {
        /// <summary>
        /// Get personal token by ID that belows for the user.
        /// </summary>
        /// <param name="User">User</param>
        /// <param name="id">Token ID.</param>
        /// <returns>Personal token by ID.</returns>
        Task<GXPersonalToken> GetPersonalTokenByIdAsync(ClaimsPrincipal User, string id);

        /// <summary>
        /// Get personal tokens that belows for the user.
        /// </summary>
        /// <param name="User">User</param>
        /// <param name="request">Request parameters.</param>
        /// <returns>Collection of personal tokens.</returns>
        Task<GXPersonalToken[]> GetPersonalTokensAsync(ClaimsPrincipal User, ListTokens? request);

        /// <summary>
        /// Add new personal access token.
        /// </summary>
        /// <param name="User">User</param>
        /// <param name="token"></param>
        Task<string> AddPersonalTokenAsync(ClaimsPrincipal User, GXPersonalToken token);

        /// <summary>
        /// Remove personal token.
        /// </summary>
        /// <param name="User">User</param>
        /// <param name="id">Removed token ID</param>
        Task<GXPersonalToken> RemovePersonalTokenAsync(ClaimsPrincipal User, string id);
    }
}
