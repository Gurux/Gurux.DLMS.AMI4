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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used with device actions.
    /// </summary>
    public interface IDeviceActionRepository
    {
        /// <summary>
        /// List device activities.
        /// </summary>
        /// <returns>Device actions.</returns>
        Task<GXDeviceAction[]> ListAsync(
            ClaimsPrincipal user,
            ListDeviceAction? request,
            ListDeviceActionResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read device activity.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Device activity id.</param>
        /// <returns></returns>
        Task<GXDeviceAction> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Add device activity.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="deviceActions">Added device actions.</param>
        Task AddAsync(ClaimsPrincipal user, IEnumerable<GXDeviceAction> deviceActions);

        /// <summary>
        /// Clear all device activitys.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="devices">List of device Ids whoes activities are cleared.</param>
        Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? devices);
    }
}
