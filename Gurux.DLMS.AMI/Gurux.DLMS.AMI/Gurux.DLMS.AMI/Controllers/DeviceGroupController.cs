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
    /// This controller is used to handle the device groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceGroupController(
        IDeviceGroupRepository DeviceGroupRepository) : ControllerBase
    {
        private readonly IDeviceGroupRepository _deviceGroupRepository = DeviceGroupRepository;

        /// <summary>
        /// Get device group information.
        /// </summary>
        /// <param name="id">Device group id.</param>
        /// <returns>Device group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXDeviceGroupPolicies.View)]
        public async Task<ActionResult<GetDeviceGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetDeviceGroupResponse()
            {
                Item = await _deviceGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update device group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceGroupPolicies.Add)]
        public async Task<ActionResult<AddDeviceGroupResponse>> Post(AddDeviceGroup request, CancellationToken cancellationToken = default)
        {
            await _deviceGroupRepository.UpsertAsync(request.DeviceGroups, null, cancellationToken);
            return new AddDeviceGroupResponse() { Ids = [.. request.DeviceGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List user groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceGroupPolicies.View)]
        public async Task<ActionResult<ListDeviceGroupsResponse>> Post(
            ListDeviceGroups request,
            CancellationToken cancellationToken)
        {
            ListDeviceGroupsResponse ret = new();
            await _deviceGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete device groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceGroupResponse>> Post(RemoveDeviceGroup request, CancellationToken cancellationToken = default)
        {
            await _deviceGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveDeviceGroupResponse();
        }
    }
}
