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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user settings.
    /// </summary>
    public interface IUserSettingRepository
    {
        /// <summary>
        /// List user settings.
        /// </summary>
        /// <returns>List of user settings.</returns>
        Task<GXUserSetting[]> ListAsync(
            ClaimsPrincipal User,
            ListUserSettings? request,
            ListUserSettingsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user settings.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Object id.</param>
        /// <returns>User setting.</returns>
        Task<GXUserSetting> ReadAsync(ClaimsPrincipal User, string id);

        /// <summary>
        /// Update user settings.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="settings">User settings to add.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXUserSetting> settings,
            Expression<Func<GXUserSetting, object?>>? columns = null);

        /// <summary>
        /// Delete user settings.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="settings">User settings to delete.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<string> settings);
    }
}
