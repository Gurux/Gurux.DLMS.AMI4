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
    /// This controller is used to handle the device templates.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTemplateController(IDeviceTemplateRepository deviceTemplateRepository) : ControllerBase
    {
        private readonly IDeviceTemplateRepository _deviceTemplateRepository = deviceTemplateRepository;

        /// <summary>
        /// Update device template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceTemplatePolicies.Add)]
        public async Task<ActionResult<UpdateDeviceTemplateResponse>> Post(UpdateDeviceTemplate request,
            CancellationToken cancellationToken = default)
        {
            if (request.Templates == null || !request.Templates.Any())
            {
                return BadRequest("Invalid device template.");
            }
            UpdateDeviceTemplateResponse ret = new()
            {
                Ids = (await _deviceTemplateRepository.UpsertAsync(request.Templates, null, cancellationToken))
            };
            return ret;
        }

        /// <summary>
        /// Get device template information.
        /// </summary>
        /// <param name="id">device template id.</param>
        /// <returns>Device template information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXDeviceTemplatePolicies.View)]
        public async Task<ActionResult<GetDeviceTemplateResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetDeviceTemplateResponse()
            {
                Item = await _deviceTemplateRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Get available device templates.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTemplatePolicies.View)]
        public async Task<ActionResult<ListDeviceTemplatesResponse>> Post(
            ListDeviceTemplates request,
            CancellationToken cancellationToken)
        {
            ListDeviceTemplatesResponse ret = new();
            await _deviceTemplateRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected device template.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceTemplatePolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceTemplateResponse>> Post(RemoveDeviceTemplate request, CancellationToken cancellationToken = default)
        {
            await _deviceTemplateRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveDeviceTemplateResponse();
        }
    }
}
