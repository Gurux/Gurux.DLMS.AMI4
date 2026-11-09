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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle attribute values.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ValueController(IValueRepository valueRepository) : ControllerBase
    {
        private readonly IValueRepository _valueRepository = valueRepository;

        /// <summary>
        /// List values.
        /// </summary>
        /// <param name="request">Request parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXValuePolicies.View)]
        public async Task<ActionResult<ListValuesResponse>> Post(
            ListValues request,
            CancellationToken cancellationToken)
        {
            ListValuesResponse ret = new();
            await _valueRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update value.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXValuePolicies.Add)]
        public async Task<ActionResult<AddValueResponse>> Post(AddValue request, CancellationToken cancellationToken = default)
        {
            AddValueResponse ret = new();
            await _valueRepository.AddAsync(request.Values, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Clear device, object or attribute values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Clear")]
        [Authorize(Policy = GXValuePolicies.Clear)]
        public async Task<ActionResult<ClearValueResponse>> Post(ClearValue request, CancellationToken cancellationToken = default)
        {
            if (request.Devices != null && request.Devices.Any())
            {
                await _valueRepository.ClearDeviceAsync(request.Devices, cancellationToken);
            }
            if (request.Objects != null && request.Objects.Any())
            {
                await _valueRepository.ClearObjectAsync(request.Objects, cancellationToken);
            }
            if (request.Attributes != null && request.Attributes.Any())
            {
                await _valueRepository.ClearAttributeAsync(request.Attributes, cancellationToken);
            }
            ClearValueResponse ret = new();
            return ret;
        }

    }
}
