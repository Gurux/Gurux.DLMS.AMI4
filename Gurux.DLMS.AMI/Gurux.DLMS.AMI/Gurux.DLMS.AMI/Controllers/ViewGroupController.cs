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
    /// This controller is used to handle the View groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ViewGroupController(
        IViewGroupRepository ViewGroupRepository) : ControllerBase
    {
        private readonly IViewGroupRepository _ViewGroupRepository = ViewGroupRepository;

        /// <summary>
        /// Get View group information.
        /// </summary>
        /// <param name="id">View group id.</param>
        /// <returns>View group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXViewGroupPolicies.View)]
        public async Task<ActionResult<GetViewGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetViewGroupResponse()
            {
                Item = await _ViewGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update View group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXViewGroupPolicies.Add)]
        public async Task<ActionResult<AddViewGroupResponse>> Post(AddViewGroup request, CancellationToken cancellationToken = default)
        {
            await _ViewGroupRepository.UpsertAsync(request.ViewGroups, null, cancellationToken);
            return new AddViewGroupResponse() { Ids = [.. request.ViewGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List View groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXViewGroupPolicies.View)]
        public async Task<ActionResult<ListViewGroupsResponse>> Post(
            ListViewGroups request, CancellationToken cancellationToken)
        {
            ListViewGroupsResponse ret = new();
            await _ViewGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes view groups based on the specified request.
        /// </summary>
        /// <param name="request">The request containing the IDs of the view groups to delete and a flag indicating whether to perform a soft
        /// delete.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXViewGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveViewGroupResponse>> Post(RemoveViewGroup request, CancellationToken cancellationToken = default)
        {
            await _ViewGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveViewGroupResponse();
        }
    }
}
