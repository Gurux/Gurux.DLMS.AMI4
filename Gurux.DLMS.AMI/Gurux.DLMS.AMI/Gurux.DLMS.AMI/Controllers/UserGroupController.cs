// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle user groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserGroupController(
        IUserGroupRepository userGroupRepository) : ControllerBase
    {
        private readonly IUserGroupRepository _userGroupRepository = userGroupRepository;

        /// <summary>
        /// Get user group information.
        /// </summary>
        /// <param name="id">User group id.</param>
        /// <returns>User group information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXUserGroupPolicies.View)]
        public async Task<ActionResult<GetUserGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetUserGroupResponse()
            {
                Item = await _userGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update user group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXUserGroupPolicies.Add)]
        public async Task<ActionResult<AddUserGroupResponse>> Post(AddUserGroup request, CancellationToken cancellationToken = default)
        {
            return new AddUserGroupResponse()
            {
                Ids = await _userGroupRepository.UpsertAsync(request.UserGroups, null, cancellationToken)
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
            ListUserGroupsResponse ret = new();
            await _userGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete user groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveUserGroupResponse>> Post(RemoveUserGroup request, CancellationToken cancellationToken = default)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveUserGroupResponse();
        }
    }
}
