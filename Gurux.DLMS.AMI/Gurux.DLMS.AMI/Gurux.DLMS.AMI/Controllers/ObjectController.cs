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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server
{
    /// <summary>
    /// This controller is used to handle the object settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectController(IObjectRepository objectRepository) : ControllerBase
    {
        private readonly IObjectRepository _objectRepository = objectRepository;

        /// <summary>
        /// Add COSEM objects
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXObjectPolicies.Add)]

        public async Task<ActionResult<UpdateObjectResponse>> Post(UpdateObject request, CancellationToken cancellationToken = default)
        {
            if (request.Objects == null || !request.Objects.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateObjectResponse ret = new()
            {
                Ids = (await _objectRepository.UpsertAsync(request.Objects, null, cancellationToken))
            };
            return ret;
        }

        /// <summary>
        /// Get object information.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <returns>Object information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXObjectPolicies.View)]
        public async Task<ActionResult<GetObjectResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetObjectResponse()
            {
                Item = await _objectRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List COSEM objects
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXObjectPolicies.View)]
        public async Task<ActionResult<ListObjectsResponse>> Post(
            ListObjects request,
            CancellationToken cancellationToken)
        {
            ListObjectsResponse ret = new();
            await _objectRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected objects
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXObjectPolicies.Delete)]
        public async Task<ActionResult<RemoveObjectResponse>> Post(RemoveObject request, CancellationToken cancellationToken = default)
        {
            await _objectRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveObjectResponse();
        }
    }
}
