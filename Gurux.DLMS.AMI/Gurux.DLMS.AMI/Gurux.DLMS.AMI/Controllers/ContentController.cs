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
    /// This controller is used to handle the contents.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentController(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        /// <summary>
        /// Get content information.
        /// </summary>
        /// <param name="id">Content id.</param>
        /// <returns>Content information.</returns>
        [HttpGet]
        [Authorize(Policy = GXContentPolicies.View)]
        public async Task<ActionResult<GetContentResponse>> Get(Guid id)
        {
            return new GetContentResponse()
            {
                Item = await _contentRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// List Contents.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXContentPolicies.View)]
        public async Task<ActionResult<ListContentsResponse>> Post(
            ListContents request,
            CancellationToken cancellationToken)
        {
            ListContentsResponse ret = new ListContentsResponse();
            await _contentRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Content.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXContentPolicies.Add)]
        public async Task<ActionResult<UpdateContentResponse>> Post(UpdateContent request)
        {
            if (request.Contents == null || request.Contents.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateContentResponse ret = new UpdateContentResponse();
            ret.Ids = await _contentRepository.UpdateAsync(request.Contents);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXContentPolicies.Delete)]
        public async Task<ActionResult<RemoveContentResponse>> Post(RemoveContent request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _contentRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveContentResponse();
        }

        /// <summary>
        /// Close the contents(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXContentPolicies.Close)]
        public async Task<ActionResult<CloseContentResponse>> Post(CloseContent request)
        {
            await _contentRepository.CloseAsync(request.Ids);
            return new CloseContentResponse();
        }
    }
}
