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
    /// This controller is used to handle the content groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContentGroupController : ControllerBase
    {
        private readonly IContentGroupRepository _ContentGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentGroupController(
            IContentGroupRepository ContentGroupRepository)
        {
            _ContentGroupRepository = ContentGroupRepository;
        }

        /// <summary>
        /// Get content group information.
        /// </summary>
        /// <param name="id">Content group id.</param>
        /// <returns>Content group.</returns>
        [HttpGet]
        [Authorize(Policy = GXContentGroupPolicies.View)]
        public async Task<ActionResult<GetContentGroupResponse>> Get(Guid id)
        {
            return new GetContentGroupResponse()
            {
                Item = await _ContentGroupRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Update content group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXContentGroupPolicies.Add)]
        public async Task<ActionResult<AddContentGroupResponse>> Post(AddContentGroup request)
        {
            await _ContentGroupRepository.UpdateAsync(request.ContentGroups);
            return new AddContentGroupResponse() { Ids = request.ContentGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List content groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXContentGroupPolicies.View)]
        public async Task<ActionResult<ListContentGroupsResponse>> Post(
            ListContentGroups request, CancellationToken cancellationToken)
        {
            ListContentGroupsResponse ret = new ListContentGroupsResponse();
            await _ContentGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXContentGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveContentGroupResponse>> Post(RemoveContentGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ContentGroupRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveContentGroupResponse();
        }
    }
}
