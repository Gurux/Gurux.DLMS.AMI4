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

using Gurux.DLMS.AMI.Shared.DTOs.Device;
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
            ListDeviceErrors? request,
            ListDeviceErrorsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read device error information.
        /// </summary>
        /// <param name="id">Device error id.</param>
        /// <returns>Device error information.</returns>
        Task<GXDeviceError> ReadAsync(Guid id);

        /// <summary>
        /// Clear device errors.
        /// </summary>
        Task ClearAsync(IEnumerable<Guid>? devices);

        /// <summary>
        /// Add device errors.
        /// </summary>
        /// <param name="type">Device error type.</param>
        /// <param name="errors">New errors.</param>
        Task AddAsync(string type, IEnumerable<GXDeviceError> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Device error type.</param>
        /// <param name="device">Device.</param>
        /// <param name="ex">Exception.</param>
        Task<GXDeviceError> AddAsync(string type, GXDevice device, Exception ex);

        /// <summary>
        /// Close device error(s).
        /// </summary>
        /// <param name="errors">Errors to close.</param>
        Task CloseAsync(IEnumerable<Guid> errors);
    }
}
