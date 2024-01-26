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
    /// This controller is used to handle the subtotal groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SubtotalGroupController : ControllerBase
    {
        private readonly ISubtotalGroupRepository _SubtotalGroupRepository;
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalGroupController(
            ISubtotalGroupRepository SubtotalGroupRepository)
        {
            _SubtotalGroupRepository = SubtotalGroupRepository;
        }

        /// <summary>
        /// Get subtotal group information.
        /// </summary>
        /// <param name="id">Subtotal group id.</param>
        /// <returns>Subtotal group.</returns>
        [HttpGet]
        [Authorize(Policy = GXSubtotalGroupPolicies.View)]
        public async Task<ActionResult<GetSubtotalGroupResponse>> Get(Guid id)
        {
            return new GetSubtotalGroupResponse()
            {
                Item = await _SubtotalGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update subtotal group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXSubtotalGroupPolicies.Add)]
        public async Task<ActionResult<AddSubtotalGroupResponse>> Post(AddSubtotalGroup request)
        {
            await _SubtotalGroupRepository.UpdateAsync(User, request.SubtotalGroups);
            return new AddSubtotalGroupResponse() { Ids = request.SubtotalGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List subtotal groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXSubtotalGroupPolicies.View)]
        public async Task<ActionResult<ListSubtotalGroupsResponse>> Post(
            ListSubtotalGroups request, CancellationToken cancellationToken)
        {
            ListSubtotalGroupsResponse ret = new ListSubtotalGroupsResponse();
            await _SubtotalGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXSubtotalGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveSubtotalGroupResponse>> Post(RemoveSubtotalGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _SubtotalGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveSubtotalGroupResponse();
        }
    }
}
