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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the manufacturers.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturerController(
        IManufacturerRepository manufacturerRepository,
        IGXBackgroundService cron) : ControllerBase
    {
        private readonly IManufacturerRepository _manufacturerRepository = manufacturerRepository;
        private readonly IGXBackgroundService _cron = cron;

        /// <summary>
        /// Get manufacturer information.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>Manufacturer information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetManufacturerResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetManufacturerResponse()
            {
                Item = await _manufacturerRepository.ReadAsync(id, null, cancellationToken)
            };
        }
        /// <summary>
        /// Update manufacturer.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateManufacturerResponse>> Post(UpdateManufacturer request, CancellationToken cancellationToken = default)
        {
            if (request.Manufacturers == null || !request.Manufacturers.Any())
            {
                return BadRequest("Invalid manufacturers");
            }
            UpdateManufacturerResponse ret = new()
            {
                Ids = await _manufacturerRepository.UpsertAsync(request.Manufacturers, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Get available manufacturers.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<ListManufacturersResponse>> Post(
            ListManufacturers request,
            CancellationToken cancellationToken)
        {
            ListManufacturersResponse ret = new();
            await _manufacturerRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected manufacturer
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXManufacturerPolicies.Delete)]
        public async Task<ActionResult<RemoveManufacturerResponse>> Post(RemoveManufacturer request, CancellationToken cancellationToken = default)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _manufacturerRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveManufacturerResponse();
        }

        /// <summary>
        /// Get device model information.
        /// </summary>
        [HttpPost("Model")]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetModelResponse>> Post(
            GetModel request, CancellationToken cancellationToken = default)
        {
            if (request?.Id == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return new GetModelResponse()
            {
                Item = await _manufacturerRepository.ReadModelAsync(request.Id.Value, cancellationToken)
            };
        }

        /// <summary>
        /// Get device version information.
        /// </summary>
        [HttpPost("Version")]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetVersionResponse>> Post(
            GetVersion request, CancellationToken cancellationToken = default)
        {
            if (request?.Id == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return new GetVersionResponse()
            {
                Item = await _manufacturerRepository.ReadVersionAsync(request.Id.Value, cancellationToken)
            };
        }
        /// <summary>
        /// Install device templates from the device settings.
        /// </summary>
        [HttpPost("Install")]
        [Authorize(Policy = GXManufacturerPolicies.Edit)]
        public async Task<ActionResult<InstallManufacturersResponse>> Post(
            InstallManufacturers request, CancellationToken cancellationToken = default)
        {
            if ((request?.Manufacturers == null || !request.Manufacturers.Any()) &&
                (request?.Models == null || !request.Models.Any()) &&
                (request?.Versions == null || !request.Versions.Any()) &&
                (request?.Settings == null || !request.Settings.Any())
                )
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _manufacturerRepository.InstallAsync(request.Manufacturers,
                request?.Models, request?.Versions, request?.Settings, cancellationToken);
            return new InstallManufacturersResponse();
        }

        /// <summary>
        /// Check are there new device templates available.
        /// </summary>
        [HttpPost("Check")]
        [Authorize(Policy = GXManufacturerPolicies.Edit)]
        public async Task<ActionResult<CheckManufacturerResponse>> Post(
            CheckManufacturer request, CancellationToken cancellationToken = default)
        {
            _ = request;
            var runId = await _cron.StartAsync(GXBackgroundTaskIds.ManufacturerUpdate, User, cancellationToken);
            return new CheckManufacturerResponse { RunId = runId };
        }
    }
}
