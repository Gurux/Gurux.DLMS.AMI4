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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle trigger groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TriggerGroupController : ControllerBase
    {
        private readonly ITriggerGroupRepository _TriggerGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerGroupController(
            ITriggerGroupRepository TriggerGroupRepository)
        {
            _TriggerGroupRepository = TriggerGroupRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>


        /// <summary>
        /// Get trigger group information.
        /// </summary>
        /// <param name="id">Trigger group id.</param>
        /// <returns>Trigger group.</returns>
        [HttpGet]
        [Authorize(Policy = GXTriggerGroupPolicies.View)]
        public async Task<ActionResult<GXTriggerGroup>> Get(Guid id)
        {
            return await _TriggerGroupRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Update trigger group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXTriggerGroupPolicies.Add)]
        public async Task<ActionResult<AddTriggerGroupResponse>> Post(AddTriggerGroup request)
        {
            await _TriggerGroupRepository.UpdateAsync(User, request.TriggerGroups);
            return new AddTriggerGroupResponse() { TriggerGroups = request.TriggerGroups };
        }

        /// <summary>
        /// List trigger groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTriggerGroupPolicies.View)]
        public async Task<ActionResult<ListTriggerGroupsResponse>> Post(
            ListTriggerGroups request, 
            CancellationToken cancellationToken)
        {
            ListTriggerGroupsResponse ret = new ListTriggerGroupsResponse();
            await _TriggerGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXTriggerGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveTriggerGroupResponse>> Post(RemoveTriggerGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _TriggerGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveTriggerGroupResponse();
        }
    }
}
