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
    /// This controller is used to handle the block groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class BlockGroupController(
        IBlockGroupRepository BlockGroupRepository) : ControllerBase
    {
        private readonly IBlockGroupRepository _BlockGroupRepository = BlockGroupRepository;

        /// <summary>
        /// Get block group information.
        /// </summary>
        /// <param name="id">Block group id.</param>
        /// <returns>Block group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXBlockGroupPolicies.View)]
        public async Task<ActionResult<GetBlockGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetBlockGroupResponse()
            {
                Item = await _BlockGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update block group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXBlockGroupPolicies.Add)]
        public async Task<ActionResult<AddBlockGroupResponse>> Post(AddBlockGroup request, CancellationToken cancellationToken = default)
        {
            if (request.BlockGroups?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _BlockGroupRepository.UpsertAsync(request.BlockGroups, null, cancellationToken);
            return new AddBlockGroupResponse() { Ids = [.. request.BlockGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List block groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXBlockGroupPolicies.View)]
        public async Task<ActionResult<ListBlockGroupsResponse>> Post(
            ListBlockGroups request, CancellationToken cancellationToken)
        {
            ListBlockGroupsResponse ret = new();
            await _BlockGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the remove block group request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXBlockGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveBlockGroupResponse>> Post(RemoveBlockGroup request, CancellationToken cancellationToken = default)
        {
            await _BlockGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveBlockGroupResponse();
        }
    }
}
