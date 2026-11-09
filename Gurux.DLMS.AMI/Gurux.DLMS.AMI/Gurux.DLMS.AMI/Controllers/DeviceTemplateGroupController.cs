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
    /// This controller is used to handle the device template groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTemplateGroupController(
        IDeviceTemplateGroupRepository DeviceTemplateGroupRepository) : ControllerBase
    {
        private readonly IDeviceTemplateGroupRepository _deviceTemplateGroupRepository = DeviceTemplateGroupRepository;

        /// <summary>
        /// Get device template group information.
        /// </summary>
        /// <param name="id">Device template group id.</param>
        /// <returns>Device template group information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.View)]
        public async Task<ActionResult<GetDeviceTemplateGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetDeviceTemplateGroupResponse()
            {
                Item = await _deviceTemplateGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update device template group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.Add)]
        public async Task<ActionResult<AddDeviceTemplateGroupResponse>> Post(AddDeviceTemplateGroup request, CancellationToken cancellationToken = default)
        {
            return new AddDeviceTemplateGroupResponse()
            {
                Ids = await _deviceTemplateGroupRepository.UpsertAsync(request.DeviceTemplateGroups, null, cancellationToken)
            };
        }

        /// <summary>
        /// List device template groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.View)]
        public async Task<ActionResult<ListDeviceTemplateGroupsResponse>> Post(
            ListDeviceTemplateGroups request, CancellationToken cancellationToken)
        {
            ListDeviceTemplateGroupsResponse ret = new();
            await _deviceTemplateGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove device template groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceTemplateGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceTemplateGroupResponse>> Post(RemoveDeviceTemplateGroup request, CancellationToken cancellationToken = default)
        {
            await _deviceTemplateGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveDeviceTemplateGroupResponse();
        }
    }
}
