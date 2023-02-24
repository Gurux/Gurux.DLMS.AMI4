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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server
{
    /// <summary>
    /// This controller is used to handle the attribute settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeTemplateController : ControllerBase
    {
        private readonly IAttributeTemplateRepository _attributeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttributeTemplateController(IAttributeTemplateRepository attributeRepository)
        {
            _attributeRepository = attributeRepository;
        }

        /// <summary>
        /// Add COSEM attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAttributeTemplatePolicies.Add)]
        public async Task<ActionResult<UpdateAttributeTemplateResponse>> Post(UpdateAttributeTemplate request)
        {
            if (request.AttributeTemplates == null || !request.AttributeTemplates.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateAttributeTemplateResponse ret = new UpdateAttributeTemplateResponse();
            ret.Ids = (await _attributeRepository.UpdateAsync(User, request.AttributeTemplates, null));
            return ret;
        }

        /// <summary>
        /// Get attribute information.
        /// </summary>
        /// <param name="id">AttributeTemplate id.</param>
        /// <returns>AttributeTemplate information.</returns>
        [HttpGet]
        [Authorize(Policy = GXAttributeTemplatePolicies.View)]
        public async Task<ActionResult<GetAttributeTemplateResponse>> Get(Guid id)
        {
            return new GetAttributeTemplateResponse()
            {
                Item = await _attributeRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List COSEM attributes
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXAttributeTemplatePolicies.View)]
        public async Task<ActionResult<ListAttributeTemplatesResponse>> Post(
            ListAttributeTemplates request,
            CancellationToken cancellationToken)
        {
            ListAttributeTemplatesResponse ret = new ListAttributeTemplatesResponse();
            await _attributeRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected attributes
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAttributeTemplatePolicies.Delete)]
        public async Task<ActionResult<RemoveAttributeTemplateResponse>> Post(RemoveAttributeTemplate request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _attributeRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveAttributeTemplateResponse();
        }

        /// <summary>
        /// Update data type of the attribute.
        /// </summary>
        [HttpPost("UpdateDataType")]
        [Authorize(Policy = GXAttributeTemplatePolicies.Edit)]
        public async Task<ActionResult<UpdateAttributeTemplateDataTypeResponse>> Post(UpdateAttributeTemplateDataType request)
        {
            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _attributeRepository.UpdateAsync(User, request.Items, w => new { w.DataType, w.UIDataType });
            return new UpdateAttributeTemplateDataTypeResponse();
        }
    }
}
