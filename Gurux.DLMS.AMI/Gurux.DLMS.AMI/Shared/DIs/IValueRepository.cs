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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle values.
    /// </summary>
    public interface IValueRepository
    {
        /// <summary>
        /// List values.
        /// </summary>
        /// <returns>List of values.</returns>
        Task<GXValue[]> ListAsync(
            ListValues? request,
            ListValuesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read value information.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <returns>Value information.</returns>
        Task<GXValue> ReadAsync(Guid id);

        /// <summary>
        /// Add values(s).
        /// </summary>
        /// <param name="values">Added values.</param>
        Task<Guid[]> AddAsync(IEnumerable<GXValue> values);

        /// <summary>
        /// Delete values(s).
        /// </summary>
        /// <param name="values">Values(s) to delete.</param>
        Task DeleteAsync(IEnumerable<Guid> values);

        /// <summary>
        /// Clear device values(s).
        /// </summary>
        /// <param name="devices">Cleared devices.</param>
        Task ClearDeviceAsync(IEnumerable<GXDevice> devices);

        /// <summary>
        /// Clear object values(s).
        /// </summary>
        /// <param name="objects">Cleared objects.</param>
        Task ClearObjectAsync(IEnumerable<GXObject> objects);

        /// <summary>
        /// Clear attribute values(s).
        /// </summary>
        /// <param name="attributes">Values(s) to clear.</param>
        Task ClearAttributeAsync(IEnumerable<GXAttribute> attributes);
    }
}
