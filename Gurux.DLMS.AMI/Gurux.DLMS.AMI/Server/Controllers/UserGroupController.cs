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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle user groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserGroupController : ControllerBase
    {
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserGroupController(
            IUserGroupRepository userGroupRepository)
        {
            _userGroupRepository = userGroupRepository;
        }

        /// <summary>
        /// Get user group information.
        /// </summary>
        /// <param name="id">User group id.</param>
        /// <returns>User group information.</returns>
        [HttpGet]
        [Authorize(Policy = GXUserGroupPolicies.View)]
        public async Task<ActionResult<GetUserGroupResponse>> Get(Guid id)
        {
            return new GetUserGroupResponse()
            {
                Item = await _userGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update user group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXUserGroupPolicies.Add)]
        public async Task<ActionResult<AddUserGroupResponse>> Post(AddUserGroup request)
        {
            return new AddUserGroupResponse()
            {
                Ids = await _userGroupRepository.UpdateAsync(User, request.UserGroups)
            };
        }

        /// <summary>
        /// List user groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserGroupPolicies.View)]
        public async Task<ActionResult<ListUserGroupsResponse>> Post(
            ListUserGroups request,
            CancellationToken cancellationToken)
        {
            ListUserGroupsResponse ret = new ListUserGroupsResponse();
            await _userGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete user groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveUserGroupResponse>> Post(RemoveUserGroup request)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveUserGroupResponse();
        }
    }
}
