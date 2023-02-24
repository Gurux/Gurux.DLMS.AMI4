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
    /// This controller is used to handle the component views.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentViewGroupController : ControllerBase
    {
        private readonly IComponentViewGroupRepository _componentViewGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentViewGroupController(
            IComponentViewGroupRepository ComponentViewGroupRepository)
        {
            _componentViewGroupRepository = ComponentViewGroupRepository;
        }

        /// <summary>
        /// Get component view group information.
        /// </summary>
        /// <param name="id">ComponentView group id.</param>
        /// <returns>ComponentView group.</returns>
        [HttpGet]
        [Authorize(Policy = GXComponentViewGroupPolicies.View)]
        public async Task<ActionResult<GetComponentViewGroupResponse>> Get(Guid id)
        {
            return new GetComponentViewGroupResponse()
            {
                Item = await _componentViewGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update component view group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXComponentViewGroupPolicies.Add)]
        public async Task<ActionResult<AddComponentViewGroupResponse>> Post(AddComponentViewGroup request)
        {
            await _componentViewGroupRepository.UpdateAsync(User, request.ComponentViewGroups);
            return new AddComponentViewGroupResponse()
            {
                Ids = request.ComponentViewGroups.Select(s => s.Id).ToArray()
            };
        }

        /// <summary>
        /// List component view groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXComponentViewGroupPolicies.View)]
        public async Task<ActionResult<ListComponentViewGroupsResponse>> Post(
            ListComponentViewGroups request,
            CancellationToken cancellationToken)
        {
            ListComponentViewGroupsResponse ret = new ListComponentViewGroupsResponse();
            await _componentViewGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXComponentViewGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveComponentViewGroupResponse>> Post(RemoveComponentViewGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _componentViewGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveComponentViewGroupResponse();
        }
    }
}
