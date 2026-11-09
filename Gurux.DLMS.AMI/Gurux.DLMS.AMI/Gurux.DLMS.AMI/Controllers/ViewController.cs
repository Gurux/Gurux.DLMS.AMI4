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
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the views.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ViewController(IViewRepository viewRepository) : ControllerBase
    {
        private readonly IViewRepository _viewRepository = viewRepository;

        /// <summary>
        /// Get view information.
        /// </summary>
        /// <param name="id">View id.</param>
        /// <returns>View information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetViewResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return GXViewHttpCache.Response(this, await _viewRepository.ReadAsync(id, null, cancellationToken));
        }

        /// <summary>
        /// Get view information.
        /// </summary>
        /// <param name="name">View name.</param>
        /// <returns>View information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("name")]
        [AllowAnonymous]
        public async Task<ActionResult<GetViewResponse>> Get(string name, CancellationToken cancellationToken = default)
        {
            return GXViewHttpCache.Response(this, await _viewRepository.ReadAsync(name, null, cancellationToken));
        }

        /// <summary>
        /// List Views.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListViewsResponse>> Post(
            ListViews request,
            CancellationToken cancellationToken)
        {
            ListViewsResponse ret = new();
            await _viewRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update View.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateViewResponse>> Post(UpdateView request, CancellationToken cancellationToken = default)
        {
            if (request.Views == null || request.Views.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateViewResponse ret = new()
            {
                Ids = await _viewRepository.UpsertAsync(request.Views, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes view items specified in the request.
        /// </summary>
        /// <param name="request">The request containing view item IDs and a flag indicating whether to perform a soft delete.</param>
        /// <returns>A response indicating the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXViewPolicies.Delete)]
        public async Task<ActionResult<RemoveViewResponse>> Post(RemoveView request, CancellationToken cancellationToken = default)
        {
            await _viewRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveViewResponse();
        }

        /// <summary>
        /// Regenerates view items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the view items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXViewPolicies.Edit)]
        public async Task<ActionResult<RegenerateViewResponse>> Post(RegenerateView request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _viewRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                await _viewRepository.EnsureDefaultsAsync(cancellationToken);
            }
            return new RegenerateViewResponse();
        }
    }
}
