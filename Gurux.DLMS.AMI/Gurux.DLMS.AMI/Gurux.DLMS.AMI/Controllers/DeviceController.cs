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

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the devices.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController(IDeviceRepository deviceRepository, Services.KeyManagement.GXDeviceSettingsService settingsService) : ControllerBase
    {
        private readonly IDeviceRepository _deviceRepository = deviceRepository;

        /// <summary>
        /// Add devices.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXDevicePolicies.Add)]
        public Task<ActionResult<UpdateDeviceResponse>> Add(UpdateDevice request,
            CancellationToken cancellationToken) => UpsertAsync(request, cancellationToken);

        /// <summary>Update existing devices.</summary>
        [HttpPost("Update")]
        [Authorize(Policy = GXDevicePolicies.Edit)]
        public Task<ActionResult<UpdateDeviceResponse>> Post(UpdateDevice request,
            CancellationToken cancellationToken) => UpsertAsync(request, cancellationToken);

        private async Task<ActionResult<UpdateDeviceResponse>> UpsertAsync(UpdateDevice request,
            CancellationToken cancellationToken)
        {
            if (request.Devices == null || !request.Devices.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateDeviceResponse ret = new()
            {
                Ids = (await _deviceRepository.UpsertAsync(request.Devices,
                    cancellationToken,
                    null,
                    request.LateBinding,
                    request.Groups))
            };
            return ret;
        }

        /// <summary>
        /// Get available devices.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("List")]
        [Authorize(Policy = GXDevicePolicies.View)]
        public async Task<ActionResult<ListDevicesResponse>> Post(
            ListDevices request,
            CancellationToken cancellationToken)
        {
            ListDevicesResponse ret = new();
            await _deviceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get device information.
        /// </summary>
        /// <param name="id">Device id.</param>
        /// <param name="includeKeys">Include operational DLMS secrets after key-use authorization; ordinary reads remain redacted.</param>
        /// <returns>Device information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Authorize(Policy = GXDevicePolicies.View)]
        public async Task<ActionResult<GetDeviceResponse>> Get(Guid id, CancellationToken cancellationToken = default, bool includeKeys = false)
        {
            return new GetDeviceResponse()
            {
                Item = includeKeys ? await settingsService.ReadAsync(User, id, false, cancellationToken)
                    : await _deviceRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>Returns fresh notification DLMS settings for an exact system title.</summary>
        [HttpGet("BySystemTitle")]
        [Authorize(Policy = GXDevicePolicies.View)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<GetDeviceResponse>> BySystemTitle(string systemTitle, CancellationToken cancellationToken = default)
            => new GetDeviceResponse { Item = await settingsService.ReadBySystemTitleAsync(User, systemTitle, cancellationToken) };

        /// <summary>
        /// Remove selected device
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDevicePolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceResponse>> Post(RemoveDevice request, CancellationToken cancellationToken = default)
        {
            await _deviceRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveDeviceResponse();
        }

        /// <summary>
        /// Update device status.
        /// </summary>
        [HttpPost("UpdateStatus")]
        [Authorize(Policy = GXDevicePolicies.Edit)]
        public async Task<ActionResult> Post(UpdateDeviceStatus request, CancellationToken cancellationToken = default)
        {
            if (request.Id == Guid.Empty)
            {
                return BadRequest(Properties.Resources.InvalidId);
            }
            await _deviceRepository.UpdateStatusAsync(request.Id, request.Status, cancellationToken);
            return Ok();
        }
    }
}
