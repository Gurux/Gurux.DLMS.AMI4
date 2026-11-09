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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the appearances.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class AppearanceController(IAppearanceRepository appearanceRepository) : ControllerBase
    {
        private readonly IAppearanceRepository _appearanceRepository = appearanceRepository;

        /// <summary>
        /// Get appearance information.
        /// </summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="id">Appearance id.</param>
        /// <returns>Appearance information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetAppearanceResponse>> Get(int type, Guid id, CancellationToken cancellationToken = default)
        {
            return new GetAppearanceResponse()
            {
                Item = await _appearanceRepository.ReadAsync(type, id, null, cancellationToken)
            };
        }

        /// <summary>Get an appearance by its symbolic resource name.</summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="name">Unique resource name.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("ByName")]
        [AllowAnonymous]
        public async Task<ActionResult<GetAppearanceResponse>> GetByName(int type, string name, CancellationToken cancellationToken = default)
        {
            return new GetAppearanceResponse
            {
                Item = await _appearanceRepository.ReadByNameAsync(type, name, null, cancellationToken)
            };
        }

        /// <summary>
        /// List Appearances.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListAppearancesResponse>> Post(
            ListAppearances request,
            CancellationToken cancellationToken)
        {
            ListAppearancesResponse ret = new();
            await _appearanceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update appearance.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Update appearance response.</returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateAppearanceResponse>> Post(UpdateAppearance request,
            CancellationToken cancellationToken)
        {
            if (request.Appearances == null || request.Appearances.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateAppearanceResponse ret = new()
            {
                Ids = await _appearanceRepository.UpsertAsync(request.Appearances, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Delete appearance.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Remove appearance response.</returns>
        [HttpPost("Delete")]
        [Authorize]
        public async Task<ActionResult<RemoveAppearanceResponse>> Post(RemoveAppearance request,
            CancellationToken cancellationToken = default)
        {
            await _appearanceRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveAppearanceResponse();
        }

        /// <summary>
        /// When the appearance was last changed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("LastChanged")]
        [AllowAnonymous]
        public async Task<ActionResult<DateTimeOffset?>> Post([FromBody] byte type,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            var ret = await _appearanceRepository.LastChanged(type, cancellationToken) ?? DateTimeOffset.MinValue;
            return Ok(ret);
        }

        /// <summary>
        /// Regenerate appearances.
        /// </summary>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXAppearancePolicies.Regenerate)]
        public async Task<ActionResult<RegenerateAppearanceResponse>> Post(RegenerateAppearance request,
            CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _appearanceRepository.ResetToDefaultsAsync(request.Type, request.Ids, cancellationToken);
            }
            else
            {
                await _appearanceRepository.EnsureDefaultsAsync(request.Type, cancellationToken);
            }
            return new RegenerateAppearanceResponse();
        }
    }
}
