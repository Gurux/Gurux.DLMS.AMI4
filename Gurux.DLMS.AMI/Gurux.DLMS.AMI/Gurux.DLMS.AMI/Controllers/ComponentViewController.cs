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
    /// This controller is used to handle the component views.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentViewController(IComponentViewRepository componentViewrRepository) : ControllerBase
    {
        private readonly IComponentViewRepository _componentviewRepository = componentViewrRepository;

        /// <summary>
        /// Get component view information.
        /// </summary>
        /// <param name="id">Block id.</param>
        /// <returns>Block information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXComponentViewPolicies.View)]
        public async Task<ActionResult<GetComponentViewResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetComponentViewResponse()
            {
                Item = await _componentviewRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List component views.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListComponentViewsResponse>> Post(
            ListComponentViews request,
            CancellationToken cancellationToken)
        {
            ListComponentViewsResponse ret = new();
            await _componentviewRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update component view.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXComponentViewPolicies.Add)]
        public async Task<ActionResult<UpdateComponentViewResponse>> Post(UpdateComponentView request, CancellationToken cancellationToken = default)
        {
            UpdateComponentViewResponse ret = new()
            {
                Ids = await _componentviewRepository.UpsertAsync(request.ComponentViews, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Delete the component view.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXComponentViewPolicies.Delete)]
        public async Task<ActionResult<RemoveComponentViewResponse>> Post(RemoveComponentView request,
            CancellationToken cancellationToken = default)
        {
            await _componentviewRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveComponentViewResponse();
        }

        /// <summary>
        /// Refresh component views.
        /// </summary>
        [HttpPost("Refresh")]
        [Authorize(Policy = GXComponentViewPolicies.Refresh)]
        public async Task<ActionResult<RefreshComponentViewResponse>> Post(RefreshComponentView request, CancellationToken cancellationToken = default)
        {
            _ = request;
            return new RefreshComponentViewResponse() { NewItems = await _componentviewRepository.RefrestAsync(cancellationToken) };
        }
    }
}
