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
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the content types.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContentTypeController : ControllerBase
    {
        private readonly IContentTypeRepository _contentTypeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentTypeController(IContentTypeRepository contentTypeRepository)
        {
            _contentTypeRepository = contentTypeRepository;
        }

        /// <summary>
        /// Get content type information.
        /// </summary>
        /// <param name="id">ContentType id.</param>
        /// <returns>ContentType information.</returns>
        [HttpGet]
        [Authorize(Policy = GXContentTypePolicies.View)]
        public async Task<ActionResult<GetContentTypeResponse>> Get(Guid id)
        {
            return new GetContentTypeResponse()
            {
                Item = await _contentTypeRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// List ContentTypes.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXContentTypePolicies.View)]
        public async Task<ActionResult<ListContentTypesResponse>> Post(
            ListContentTypes request,
            CancellationToken cancellationToken)
        {
            ListContentTypesResponse ret = new ListContentTypesResponse();
            await _contentTypeRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update ContentType.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXContentTypePolicies.Add)]
        public async Task<ActionResult<UpdateContentTypeResponse>> Post(UpdateContentType request)
        {
            if (request.ContentTypes == null || request.ContentTypes.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateContentTypeResponse ret = new UpdateContentTypeResponse();
            ret.Ids = await _contentTypeRepository.UpdateAsync(request.ContentTypes);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXContentTypePolicies.Delete)]
        public async Task<ActionResult<RemoveContentTypeResponse>> Post(RemoveContentType request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _contentTypeRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveContentTypeResponse();
        }
    }
}
