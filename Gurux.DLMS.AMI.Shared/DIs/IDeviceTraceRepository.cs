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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used with device hex traces.
    /// </summary>
    [System.ComponentModel.Description("This public interface is used with device hex traces.")]
    public interface IDeviceTraceRepository
    {
        /// <summary>
        /// Lists device communication traces.
        /// </summary>
        /// <returns>DeviceActivitys.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("Lists device communication traces.")]
        Task<IEnumerable<GXDeviceTrace>> ListAsync(
            ListDeviceTrace? request = null,
            ListDeviceTraceResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a device communication trace by identifier.
        /// </summary>
        /// <param name="id">Device activity id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested device trace.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Reads a device communication trace by identifier.")]
        Task<GXDeviceTrace> ReadAsync(Guid id, Expression<Func<GXDeviceTrace, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add device traces.
        /// </summary>
        /// <param name="type">Device trace type.</param>
        /// <param name="deviceTraces">Added device traces.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to add device traces.</returns>
        [System.ComponentModel.Description("Add device traces.")]
        Task AddAsync(string type, IEnumerable<GXDeviceTrace> deviceTraces, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all device activities.
        /// </summary>
        /// <param name="devices">List of device Ids whoes activities are cleared.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear all device activities.</returns>
        [System.ComponentModel.Description("Clear all device activities.")]
        Task ClearAsync(IEnumerable<Guid>? devices, CancellationToken cancellationToken);
    }
}
