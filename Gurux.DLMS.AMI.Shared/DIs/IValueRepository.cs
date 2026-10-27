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
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle values.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle values.")]
    public interface IValueRepository
    {
        /// <summary>
        /// List values.
        /// </summary>
        /// <returns>List of values.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List values.")]
        Task<IEnumerable<GXValue>> ListAsync(
            ListValues? request = null,
            ListValuesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read value details.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Value information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read value details.")]
        Task<GXValue> ReadAsync(Guid id, Expression<Func<GXValue, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Add valuess.</summary>
        /// <param name="values">Added values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Add valuess.")]
        Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXValue> values, CancellationToken cancellationToken = default);

        /// <summary>Delete values.</summary>
        /// <param name="values">Values(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete values.")]
        Task DeleteAsync(IEnumerable<Guid> values, CancellationToken cancellationToken = default);

        /// <summary>Clear device valuess.</summary>
        /// <param name="devices">Cleared devices.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Clear device valuess.")]
        Task ClearDeviceAsync(IEnumerable<GXDevice> devices, CancellationToken cancellationToken = default);

        /// <summary>Clear object valuess.</summary>
        /// <param name="objects">Cleared objects.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Clear object valuess.")]
        Task ClearObjectAsync(IEnumerable<GXObject> objects, CancellationToken cancellationToken = default);

        /// <summary>Clear attribute valuess.</summary>
        /// <param name="attributes">Values to clear.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Clear attribute valuess.")]
        Task ClearAttributeAsync(IEnumerable<GXAttribute> attributes, CancellationToken cancellationToken = default);
    }
}
