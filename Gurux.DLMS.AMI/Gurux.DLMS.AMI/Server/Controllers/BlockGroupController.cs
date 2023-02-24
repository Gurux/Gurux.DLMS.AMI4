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
    /// This controller is used to handle the block groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BlockGroupController : ControllerBase
    {
        private readonly IBlockGroupRepository _BlockGroupRepository;
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockGroupController(
            IBlockGroupRepository BlockGroupRepository)
        {
            _BlockGroupRepository = BlockGroupRepository;
        }

        /// <summary>
        /// Get block group information.
        /// </summary>
        /// <param name="id">Block group id.</param>
        /// <returns>Block group.</returns>
        [HttpGet]
        [Authorize(Policy = GXBlockGroupPolicies.View)]
        public async Task<ActionResult<GetBlockGroupResponse>> Get(Guid id)
        {
            return new GetBlockGroupResponse()
            {
                Item = await _BlockGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update block group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXBlockGroupPolicies.Add)]
        public async Task<ActionResult<AddBlockGroupResponse>> Post(AddBlockGroup request)
        {
            await _BlockGroupRepository.UpdateAsync(User, request.BlockGroups);
            return new AddBlockGroupResponse() { Ids = request.BlockGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List block groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXBlockGroupPolicies.View)]
        public async Task<ActionResult<ListBlockGroupsResponse>> Post(
            ListBlockGroups request, CancellationToken cancellationToken)
        {
            ListBlockGroupsResponse ret = new ListBlockGroupsResponse();
            await _BlockGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXBlockGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveBlockGroupResponse>> Post(RemoveBlockGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _BlockGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveBlockGroupResponse();
        }
    }
}
