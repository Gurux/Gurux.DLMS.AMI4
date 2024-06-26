﻿//
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
    /// This public interface is used with user activities.
    /// </summary>
    public interface IUserActionRepository
    {
        /// <summary>
        /// List user activities.
        /// </summary>
        /// <returns>UserActivitys.</returns>
        Task<GXUserAction[]> ListAsync(ClaimsPrincipal user, 
            ListUserAction? request, 
            ListUserActionResponse? response = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user activity.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">User activity id.</param>
        /// <returns></returns>
        Task<GXUserAction> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Add user activity.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="userActions">Added user actions.</param>
        Task AddAsync(ClaimsPrincipal user, IEnumerable<GXUserAction> userActions);

        /// <summary>
        /// Clear all user activitys.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="users">List of user Ids whoes activities are cleared.</param>
        Task ClearAsync(ClaimsPrincipal user, IEnumerable<string>? users);
    }
}
