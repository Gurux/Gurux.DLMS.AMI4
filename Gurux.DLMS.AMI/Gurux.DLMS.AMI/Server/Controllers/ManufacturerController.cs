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

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Cron;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the manufacturers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturerController : ControllerBase
    {
        private readonly IManufacturerRepository _manufacturerRepository;
        private readonly IGXCronTask _cron;
        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerController(
            IManufacturerRepository manufacturerRepository,
            IGXCronTask cron)
        {
            _manufacturerRepository = manufacturerRepository;
            _cron = cron;
        }

        /// <summary>
        /// Get manufacturer information.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>Manufacturer information.</returns>
        [HttpGet]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetManufacturerResponse>> Get(Guid id)
        {
            return new GetManufacturerResponse()
            {
                Item = await _manufacturerRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update manufacturer.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXManufacturerPolicies.Add)]
        public async Task<ActionResult<UpdateManufacturerResponse>> Post(UpdateManufacturer request)
        {
            if (request.Manufacturers == null || !request.Manufacturers.Any())
            {
                return BadRequest("Invalid manufacturers");
            }
            UpdateManufacturerResponse ret = new UpdateManufacturerResponse();
            ret.Ids = await _manufacturerRepository.UpdateAsync(User, request.Manufacturers);
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
            ListManufacturersResponse ret = new ListManufacturersResponse();
            await _manufacturerRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected manufacturer
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXManufacturerPolicies.Delete)]
        public async Task<ActionResult<RemoveManufacturerResponse>> Post(RemoveManufacturer request)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _manufacturerRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveManufacturerResponse();
        }

        /// <summary>
        /// Get model information.
        /// </summary>
        [HttpPost("Model")]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetModelResponse>> Post(
            GetModel request)
        {
            if (request?.Id == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return new GetModelResponse()
            {
                Item = await _manufacturerRepository.ReadModelAsync(User, request.Id.Value)
            };
        }

        /// <summary>
        /// Get Version information.
        /// </summary>
        [HttpPost("Version")]
        [Authorize(Policy = GXManufacturerPolicies.View)]
        public async Task<ActionResult<GetVersionResponse>> Post(
            GetVersion request)
        {
            if (request?.Id == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return new GetVersionResponse()
            {
                Item = await _manufacturerRepository.ReadVersionAsync(User, request.Id.Value)
            };
        }
        /// <summary>
        /// Install device templates from the device settings.
        /// </summary>
        [HttpPost("Install")]
        [Authorize(Policy = GXManufacturerPolicies.Edit)]
        public async Task<ActionResult<InstallManufacturersResponse>> Post(
            InstallManufacturers request)
        {
            if ((request?.Manufacturers == null || !request.Manufacturers.Any()) &&
                (request?.Models == null || !request.Models.Any()) &&
                (request?.Versions == null || !request.Versions.Any()) &&
                (request?.Settings == null || !request.Settings.Any())
                )
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _manufacturerRepository.InstallAsync(User, request.Manufacturers,
                request?.Models, request?.Versions, request?.Settings);
            return new InstallManufacturersResponse();
        }

        /// <summary>
        /// Check are there new device templates available.
        /// </summary>
        [HttpPost("Check")]
        [Authorize(Policy = GXManufacturerPolicies.Edit)]
        public async Task<ActionResult<CheckManufacturerResponse>> Post(
            CheckManufacturer request)
        {
            await _cron.CheckManufacturersAsync(User);
            return new CheckManufacturerResponse();
        }
    }
}
