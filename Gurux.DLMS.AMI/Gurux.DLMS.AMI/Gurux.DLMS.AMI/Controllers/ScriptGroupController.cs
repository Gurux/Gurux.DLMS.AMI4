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
    /// This controller is used to handle the script groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptGroupController(
        IScriptGroupRepository ScriptGroupRepository) : ControllerBase
    {
        private readonly IScriptGroupRepository _ScriptGroupRepository = ScriptGroupRepository;

        /// <summary>
        /// Get script group information.
        /// </summary>
        /// <param name="id">Script group id.</param>
        /// <returns>Script group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXScriptGroupPolicies.View)]
        public async Task<ActionResult<GetScriptGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetScriptGroupResponse()
            {
                Item = await _ScriptGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update script group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScriptGroupPolicies.Add)]
        public async Task<ActionResult<AddScriptGroupResponse>> Post(AddScriptGroup request, CancellationToken cancellationToken = default)
        {
            await _ScriptGroupRepository.UpsertAsync(request.ScriptGroups, null, cancellationToken);
            return new AddScriptGroupResponse()
            {
                Ids = [.. request.ScriptGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List script groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScriptGroupPolicies.View)]
        public async Task<ActionResult<ListScriptGroupsResponse>> Post(
            ListScriptGroups request,
            CancellationToken cancellationToken)
        {
            ListScriptGroupsResponse ret = new();
            await _ScriptGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the remove script group request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXScriptGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveScriptGroupResponse>> Post(RemoveScriptGroup request, CancellationToken cancellationToken = default)
        {
            await _ScriptGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveScriptGroupResponse();
        }
    }
}
