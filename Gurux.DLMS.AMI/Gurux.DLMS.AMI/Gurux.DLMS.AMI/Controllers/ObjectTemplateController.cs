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

namespace Gurux.DLMS.AMI.Server
{
    /// <summary>
    /// This controller is used to handle the object templates.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectTemplateController(IObjectTemplateRepository objectRepository) : ControllerBase
    {
        private readonly IObjectTemplateRepository _objectRepository = objectRepository;

        /// <summary>
        /// Add COSEM object template.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXObjectTemplatePolicies.Add)]

        public async Task<ActionResult<UpdateObjectTemplateResponse>> Post(UpdateObjectTemplate request, CancellationToken cancellationToken = default)
        {
            if (request.ObjectTemplates == null || !request.ObjectTemplates.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateObjectTemplateResponse ret = new()
            {
                Ids = (await _objectRepository.UpsertAsync(request.ObjectTemplates, null, cancellationToken))
            };
            return ret;
        }

        /// <summary>
        /// Get object template information.
        /// </summary>
        /// <param name="id">Object template id.</param>
        /// <returns>Object template information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXObjectTemplatePolicies.View)]
        public async Task<ActionResult<GetObjectTemplateResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetObjectTemplateResponse()
            {
                Item = await _objectRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List COSEM object templates.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXObjectTemplatePolicies.View)]
        public async Task<ActionResult<ListObjectTemplatesResponse>> Post(
            ListObjectTemplates request,
            CancellationToken cancellationToken)
        {
            ListObjectTemplatesResponse ret = new();
            await _objectRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected object templates.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXObjectTemplatePolicies.Delete)]
        public async Task<ActionResult<RemoveObjectTemplateResponse>> Post(RemoveObjectTemplate request, CancellationToken cancellationToken = default)
        {
            await _objectRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveObjectTemplateResponse();
        }
    }
}
