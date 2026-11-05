// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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

        /// <summary>Add values.</summary>
        /// <param name="values">Added values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the added records.</returns>
        [System.ComponentModel.Description("Add values.")]
        Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXValue> values, CancellationToken cancellationToken = default);

        /// <summary>Delete values.</summary>
        /// <param name="values">Values(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete values.</returns>
        [System.ComponentModel.Description("Delete values.")]
        Task DeleteAsync(IEnumerable<Guid> values, CancellationToken cancellationToken = default);

        /// <summary>Clear device values.</summary>
        /// <param name="devices">Cleared devices.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear device values.</returns>
        [System.ComponentModel.Description("Clear device values.")]
        Task ClearDeviceAsync(IEnumerable<GXDevice> devices, CancellationToken cancellationToken = default);

        /// <summary>Clear object values.</summary>
        /// <param name="objects">Cleared objects.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear object values.</returns>
        [System.ComponentModel.Description("Clear object values.")]
        Task ClearObjectAsync(IEnumerable<GXObject> objects, CancellationToken cancellationToken = default);

        /// <summary>Clear attribute values.</summary>
        /// <param name="attributes">Values to clear.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear attribute values.</returns>
        [System.ComponentModel.Description("Clear attribute values.")]
        Task ClearAttributeAsync(IEnumerable<GXAttribute> attributes, CancellationToken cancellationToken = default);
    }
}
