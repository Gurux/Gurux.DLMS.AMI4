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
    /// This controller is used to handle the schedule groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleGroupController : ControllerBase
    {
        private readonly IScheduleGroupRepository _ScheduleGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleGroupController(
            IScheduleGroupRepository ScheduleGroupRepository)
        {
            _ScheduleGroupRepository = ScheduleGroupRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>


        /// <summary>
        /// Get schedule group information.
        /// </summary>
        /// <param name="id">Schedule group id.</param>
        /// <returns>Schedule group.</returns>
        [HttpGet]
        [Authorize(Policy = GXScheduleGroupPolicies.View)]
        public async Task<ActionResult<GXScheduleGroup>> Get(Guid id)
        {
            return await _ScheduleGroupRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Update schedule group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScheduleGroupPolicies.Add)]
        public async Task<ActionResult<AddScheduleGroupResponse>> Post(AddScheduleGroup request)
        {
            await _ScheduleGroupRepository.UpdateAsync(User, request.ScheduleGroups);
            return new AddScheduleGroupResponse() { ScheduleGroups = request.ScheduleGroups };
        }

        /// <summary>
        /// List schedule groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScheduleGroupPolicies.View)]
        public async Task<ActionResult<ListScheduleGroupsResponse>> Post(
            ListScheduleGroups request, 
            CancellationToken cancellationToken)
        {
            ListScheduleGroupsResponse ret = new ListScheduleGroupsResponse();
            await _ScheduleGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXScheduleGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveScheduleGroupResponse>> Post(RemoveScheduleGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ScheduleGroupRepository.DeleteAsync(User, request.Ids);
            return new RemoveScheduleGroupResponse();
        }
    }
}
