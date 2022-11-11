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
    /// This interface is used to handle device errors.
    /// </summary>
    public interface IDeviceErrorRepository
    {
        /// <summary>
        /// List device errors.
        /// </summary>
        /// <returns>List of device errors.</returns>
        Task<GXDeviceError[]> ListAsync(
            ClaimsPrincipal User, 
            ListDeviceErrors? request, 
            ListDeviceErrorsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read device error information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Device error id.</param>
        /// <returns>Device error information.</returns>
        Task<GXDeviceError> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear device errors.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, IEnumerable<Guid>? devices);

        /// <summary>
        /// Add device errors.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">New errors.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXDeviceError> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="device">Device.</param>
        /// <param name="ex">Exception.</param>
        Task<GXDeviceError> AddAsync(ClaimsPrincipal User, GXDevice device, Exception ex);

        /// <summary>
        /// Close device error(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">Errors to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors);
    }
}
