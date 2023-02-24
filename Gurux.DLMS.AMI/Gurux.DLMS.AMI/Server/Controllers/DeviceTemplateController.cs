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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the device templates.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTemplateController : ControllerBase
    {
        private readonly IDeviceTemplateRepository _deviceTemplateRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateController(IDeviceTemplateRepository deviceTemplateRepository)
        {
            _deviceTemplateRepository = deviceTemplateRepository;
        }

        /// <summary>
        /// Update device template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXDeviceTemplatePolicies.Add)]
        public async Task<ActionResult<UpdateDeviceTemplateResponse>> Post(UpdateDeviceTemplate request)
        {
            if (request.Templates == null || request.Templates.Length == 0)
            {
                return BadRequest("Invalid device template.");
            }
            UpdateDeviceTemplateResponse ret = new UpdateDeviceTemplateResponse();
            ret.Ids = (await _deviceTemplateRepository.UpdateAsync(User, request.Templates));
            return ret;
        }

        /// <summary>
        /// Get device template information.
        /// </summary>
        /// <param name="id">device template id.</param>
        /// <returns>Device template information.</returns>
        [HttpGet]
        [Authorize(Policy = GXDeviceTemplatePolicies.View)]
        public async Task<ActionResult<GetDeviceTemplateResponse>> Get(Guid id)
        {
            return new GetDeviceTemplateResponse()
            {
                Item = await _deviceTemplateRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Get available device templates.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXDeviceTemplatePolicies.View)]
        public async Task<ActionResult<ListDeviceTemplatesResponse>> Post(
            ListDeviceTemplates request,
            CancellationToken cancellationToken)
        {
            ListDeviceTemplatesResponse ret = new ListDeviceTemplatesResponse();
            await _deviceTemplateRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected device template.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXDeviceTemplatePolicies.Delete)]
        public async Task<ActionResult<RemoveDeviceTemplateResponse>> Post(RemoveDeviceTemplate request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _deviceTemplateRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveDeviceTemplateResponse();
        }
    }
}
