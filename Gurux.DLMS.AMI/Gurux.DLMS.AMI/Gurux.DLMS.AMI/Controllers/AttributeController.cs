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
    /// This controller is used to handle the attribute settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeController(IAttributeRepository attributeRepository) : ControllerBase
    {
        private readonly IAttributeRepository _attributeRepository = attributeRepository;

        /// <summary>
        /// Add COSEM attributes
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAttributePolicies.Add)]
        public async Task<ActionResult<UpdateAttributeResponse>> Post(UpdateAttribute request, CancellationToken cancellationToken = default)
        {
            if (request.Attributes == null || !request.Attributes.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateAttributeResponse ret = new()
            {
                Ids = (await _attributeRepository.UpsertAsync(request.Attributes, null, cancellationToken))
            };
            return ret;
        }

        /// <summary>
        /// Get attribute information.
        /// </summary>
        /// <param name="id">Attribute id.</param>
        /// <returns>Attribute information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXAttributePolicies.View)]
        public async Task<ActionResult<GetAttributeResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetAttributeResponse()
            {
                Item = await _attributeRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List COSEM attributes
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXAttributePolicies.View)]
        public async Task<ActionResult<ListAttributesResponse>> Post(
            ListAttributes request,
            CancellationToken cancellationToken)
        {
            ListAttributesResponse ret = new();
            await _attributeRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected attributes
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAttributePolicies.Delete)]
        public async Task<ActionResult<RemoveAttributeResponse>> Post(RemoveAttribute request,
            CancellationToken cancellationToken = default)
        {
            await _attributeRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveAttributeResponse();
        }

        /// <summary>
        /// Update data type of the attribute.
        /// </summary>
        [HttpPost("UpdateDataType")]
        [Authorize(Policy = GXAttributePolicies.Edit)]
        public async Task<ActionResult<UpdateDatatypeResponse>> Post(UpdateDatatype request, CancellationToken cancellationToken = default)
        {
            await _attributeRepository.UpdateDatatypeAsync(request.Items, cancellationToken);
            return new UpdateDatatypeResponse();
        }
    }
}
