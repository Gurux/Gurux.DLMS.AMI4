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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the localized resources.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class LocalizedResourceController(ILocalizedResourceRepository localizedResourceRepository) : ControllerBase
    {
        private readonly ILocalizedResourceRepository _localizedResourceRepository = localizedResourceRepository;

        /// <summary>
        /// Update localized resource.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXLocalizedResourcePolicies.Add)]
        public async Task<ActionResult<UpdateLocalizedResourceResponse>> Post(UpdateLocalizedResource request, CancellationToken cancellationToken = default)
        {
            UpdateLocalizedResourceResponse ret = new()
            {
                Ids = await _localizedResourceRepository.UpsertAsync(request.LocalizedResources, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Get localized resources.
        /// </summary>
        /// <returns>Available localized resources.</returns>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListLocalizedResourcesResponse>> Post(
            ListLocalizedResources request,
            CancellationToken cancellationToken)
        {
            ListLocalizedResourcesResponse ret = new();
            await _localizedResourceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get localized resource information.
        /// </summary>
        /// <param name="hash">Hash of localized resource.</param>
        /// <param name="lang">Used language identifier.</param>
        /// <param name="text">Resource text.</param>
        /// <returns>LocalizedResource information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetLocalizedResourceResponse>> Get(string lang, string hash, string? text, CancellationToken cancellationToken = default)
        {
            var ret = new GetLocalizedResourceResponse
            {
                Item = await _localizedResourceRepository.ReadAsync(lang, hash, text, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Remove selected localized resources.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXLocalizedResourcePolicies.Delete)]
        public async Task<ActionResult<LocalizedResourceDeleteResponse>> Post(LocalizedResourceDelete request, CancellationToken cancellationToken = default)
        {
            await _localizedResourceRepository.DeleteAsync(request.Ids, cancellationToken);
            return new LocalizedResourceDeleteResponse();
        }

        /// <summary>
        /// When the localized resources was last changed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("LastChanged")]
        [AllowAnonymous]
        public async Task<ActionResult<DateTimeOffset?>> Post(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            var ret = await _localizedResourceRepository.LastChanged() ?? DateTimeOffset.MinValue;
            return Ok(ret);
        }
    }
}
