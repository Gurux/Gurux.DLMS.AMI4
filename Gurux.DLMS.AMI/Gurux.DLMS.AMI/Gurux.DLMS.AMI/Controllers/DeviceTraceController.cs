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

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device traces.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTraceController(
        IDeviceTraceRepository deviceTraceRepository) : ControllerBase
    {
        private readonly IDeviceTraceRepository _deviceTraceRepository = deviceTraceRepository;

        /// <summary>
        /// Get device trace information.
        /// </summary>
        /// <param name="id">Device trace id.</param>
        /// <returns>Device trace.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXDeviceTracePolicies.View)]
        public async Task<ActionResult<GetDeviceTrace>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetDeviceTrace()
            {
                Item = await _deviceTraceRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List device traces.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTracePolicies.View)]
        public async Task<ActionResult<ListDeviceTraceResponse>> Post(
            ListDeviceTrace request,
            CancellationToken cancellationToken)
        {
            ListDeviceTraceResponse ret = new();
            await _deviceTraceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New device trace is added.
        /// </summary>
        /// <param name="request">Device trace item.</param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXDeviceTracePolicies.Add)]
        public async Task<ActionResult<AddDeviceTraceResponse>> Post(AddDeviceTrace request, CancellationToken cancellationToken = default)
        {
            AddDeviceTraceResponse ret = new();
            await _deviceTraceRepository.AddAsync(request.Type, request.Traces, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New Device trace is added..
        /// </summary>
        /// <param name="request">Device trace item.</param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Clear")]
        [Authorize(Policy = GXDeviceTracePolicies.Clear)]
        public async Task<ActionResult<ClearDeviceTraceResponse>> Post(ClearDeviceTrace request, CancellationToken cancellationToken = default)
        {
            ClearDeviceTraceResponse ret = new();
            await _deviceTraceRepository.ClearAsync(request.Ids, cancellationToken);
            return ret;
        }
    }
}