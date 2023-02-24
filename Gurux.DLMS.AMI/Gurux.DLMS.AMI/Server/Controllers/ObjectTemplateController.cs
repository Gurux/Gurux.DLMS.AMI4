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
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server
{
    /// <summary>
    /// This controller is used to handle the object templates.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectTemplateController : ControllerBase
    {
        private readonly IObjectTemplateRepository _objectRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectTemplateController(IObjectTemplateRepository objectRepository)
        {
            _objectRepository = objectRepository;
        }

        /// <summary>
        /// Add COSEM object template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXObjectTemplatePolicies.Add)]

        public async Task<ActionResult<UpdateObjectTemplateResponse>> Post(UpdateObjectTemplate request)
        {
            if (request.ObjectTemplates == null || !request.ObjectTemplates.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateObjectTemplateResponse ret = new UpdateObjectTemplateResponse()
            {
                Ids = (await _objectRepository.UpdateAsync(User, request.ObjectTemplates))
            };
            return ret;
        }

        /// <summary>
        /// Get object template information.
        /// </summary>
        /// <param name="id">Object template id.</param>
        /// <returns>Object template information.</returns>
        [HttpGet]
        [Authorize(Policy = GXObjectTemplatePolicies.View)]
        public async Task<ActionResult<GetObjectTemplateResponse>> Get(Guid id)
        {
            return new GetObjectTemplateResponse()
            {
                Item = await _objectRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List COSEM object templates.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXObjectTemplatePolicies.View)]
        public async Task<ActionResult<ListObjectTemplatesResponse>> Post(
            ListObjectTemplates request,
            CancellationToken cancellationToken)
        {
            ListObjectTemplatesResponse ret = new ListObjectTemplatesResponse();
            await _objectRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected object templates.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXObjectTemplatePolicies.Delete)]
        public async Task<ActionResult<RemoveObjectTemplateResponse>> Post(RemoveObjectTemplate request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _objectRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveObjectTemplateResponse();
        }
    }
}
