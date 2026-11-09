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
    /// This controller is used to handle the Menu groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuGroupController(
        IMenuGroupRepository MenuGroupRepository) : ControllerBase
    {
        private readonly IMenuGroupRepository _MenuGroupRepository = MenuGroupRepository;

        /// <summary>
        /// Get Menu group information.
        /// </summary>
        /// <param name="id">Menu group id.</param>
        /// <returns>Menu group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXMenuGroupPolicies.View)]
        public async Task<ActionResult<GetMenuGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetMenuGroupResponse()
            {
                Item = await _MenuGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update Menu group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXMenuGroupPolicies.Add)]
        public async Task<ActionResult<AddMenuGroupResponse>> Post(AddMenuGroup request, CancellationToken cancellationToken = default)
        {
            await _MenuGroupRepository.UpsertAsync(request.MenuGroups, null, cancellationToken);
            return new AddMenuGroupResponse() { Ids = [.. request.MenuGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List Menu groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXMenuGroupPolicies.View)]
        public async Task<ActionResult<ListMenuGroupsResponse>> Post(
            ListMenuGroups request, CancellationToken cancellationToken)
        {
            ListMenuGroupsResponse ret = new();
            await _MenuGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes menu groups based on the specified request.
        /// </summary>
        /// <param name="request">The request containing the IDs of the menu groups to delete and a flag indicating whether to perform a soft
        /// delete.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXMenuGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveMenuGroupResponse>> Post(RemoveMenuGroup request, CancellationToken cancellationToken = default)
        {
            await _MenuGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveMenuGroupResponse();
        }
    }
}
