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
    /// This controller is used to handle the manufacturer groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturerGroupController(
        IManufacturerGroupRepository ManufacturerGroupRepository) : ControllerBase
    {
        private readonly IManufacturerGroupRepository _ManufacturerGroupRepository = ManufacturerGroupRepository;

        /// <summary>
        /// Get manufacturer group information.
        /// </summary>
        /// <param name="id">Manufacturer group id.</param>
        /// <returns>Manufacturer group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXManufacturerGroupPolicies.View)]
        public async Task<ActionResult<GetManufacturerGroupResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetManufacturerGroupResponse()
            {
                Item = await _ManufacturerGroupRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update manufacturer group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXManufacturerGroupPolicies.Add)]
        public async Task<ActionResult<AddManufacturerGroupResponse>> Post(AddManufacturerGroup request, CancellationToken cancellationToken = default)
        {
            await _ManufacturerGroupRepository.UpsertAsync(request.ManufacturerGroups, null, cancellationToken);
            return new AddManufacturerGroupResponse() { Ids = [.. request.ManufacturerGroups.Select(s => s.Id)] };
        }

        /// <summary>
        /// List manufacturer groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXManufacturerGroupPolicies.View)]
        public async Task<ActionResult<ListManufacturerGroupsResponse>> Post(
            ListManufacturerGroups request, CancellationToken cancellationToken)
        {
            ListManufacturerGroupsResponse ret = new();
            await _ManufacturerGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes the specified manufacturer groups.
        /// </summary>
        /// <param name="request">Contains the identifiers of the manufacturer groups to delete and a flag indicating whether to perform a
        /// soft delete.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXManufacturerGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveManufacturerGroupResponse>> Post(RemoveManufacturerGroup request, CancellationToken cancellationToken = default)
        {
            await _ManufacturerGroupRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveManufacturerGroupResponse();
        }
    }
}
