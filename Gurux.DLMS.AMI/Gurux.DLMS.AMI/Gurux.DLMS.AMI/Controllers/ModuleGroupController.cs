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
    /// This controller is used to handle the module groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleGroupController(
        IModuleGroupRepository ModuleGroupRepository) : ControllerBase
    {
        private readonly IModuleGroupRepository _ModuleGroupRepository = ModuleGroupRepository;

        /// <summary>
        /// Get module group information.
        /// </summary>
        /// <param name="id">Module group id.</param>
        /// <returns>Module group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXModuleGroupPolicies.View)]
        public async Task<ActionResult<GetModuleGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetModuleGroupResponse()
            {
                Item = await _ModuleGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update module group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXModuleGroupPolicies.Edit)]
        public async Task<ActionResult<AddModuleGroupResponse>> Post(AddModuleGroup request, CancellationToken cancellationToken = default)
        {
            await _ModuleGroupRepository.UpsertAsync(request.ModuleGroups, null, cancellationToken);
            return new AddModuleGroupResponse()
            {
                Ids = [.. request.ModuleGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List module groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXModuleGroupPolicies.View)]
        public async Task<ActionResult<ListModuleGroupsResponse>> Post(
            ListModuleGroups request,
            CancellationToken cancellationToken)
        {
            ListModuleGroupsResponse ret = new();
            await _ModuleGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes module groups based on the specified IDs and delete option.
        /// </summary>
        /// <param name="request">Contains the IDs of the module groups to delete and a flag indicating whether to perform a soft delete.</param>
        /// <returns>A response indicating the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXModuleGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveModuleGroupResponse>> Post(RemoveModuleGroup request, CancellationToken cancellationToken = default)
        {
            await _ModuleGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveModuleGroupResponse();
        }
    }
}
