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
    /// This controller is used to handle the KeyManagement groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class KeyManagementGroupController(
        IKeyManagementGroupRepository KeyManagementGroupRepository) : ControllerBase
    {
        private readonly IKeyManagementGroupRepository _KeyManagementGroupRepository = KeyManagementGroupRepository;

        /// <summary>
        /// Get key management group information.
        /// </summary>
        /// <param name="id">KeyManagement group id.</param>
        /// <returns>KeyManagement group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXKeyManagementGroupPolicies.View)]
        public async Task<ActionResult<GetKeyManagementGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetKeyManagementGroupResponse()
            {
                Item = await _KeyManagementGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update key management group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.Add)]
        public async Task<ActionResult<AddKeyManagementGroupResponse>> Post(AddKeyManagementGroup request, CancellationToken cancellationToken = default)
        {
            await _KeyManagementGroupRepository.UpsertAsync(request.KeyManagementGroups, null, cancellationToken);
            return new AddKeyManagementGroupResponse()
            {
                Ids = [.. request.KeyManagementGroups.Select(s => s.Id)]
            };
        }

        /// <summary>
        /// List key management groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.View)]
        public async Task<ActionResult<ListKeyManagementGroupsResponse>> Post(
            ListKeyManagementGroups request,
            CancellationToken cancellationToken)
        {
            ListKeyManagementGroupsResponse ret = new();
            await _KeyManagementGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete key management groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveKeyManagementGroupResponse>> Post(RemoveKeyManagementGroup request, CancellationToken cancellationToken = default)
        {
            await _KeyManagementGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveKeyManagementGroupResponse();
        }
    }
}
