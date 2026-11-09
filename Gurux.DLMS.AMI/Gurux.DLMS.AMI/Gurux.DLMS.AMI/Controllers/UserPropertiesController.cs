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
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the user settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserPropertiesController(IUserPropertiesRepository userSettingRepository) : ControllerBase
    {
        private readonly IUserPropertiesRepository _userSettingRepository = userSettingRepository;

        /// <summary>
        /// Get user settings.
        /// </summary>
        /// <param name="name">User settings name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="userId">Optional settings owner; another user requires administrator access.</param>
        /// <returns>User settings.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
        [HttpGet]
        [Authorize(Policy = GXUserPropertiesPolicies.View)]
        public async Task<ActionResult<GetUserProperties>> Get(string name, CancellationToken cancellationToken = default, string? userId = null)
        {
            return new GetUserProperties()
            {
                Item = await _userSettingRepository.ReadAsync(name, null, cancellationToken, userId)
            };
        }

        /// <summary>
        /// Get user settings.
        /// </summary>
        /// <param name="request">List user settings parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of user settings.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXUserPropertiesPolicies.View)]
        public async Task<ActionResult<ListUserPropertiesResponse>> Post(
            ListUserProperties request,
            CancellationToken cancellationToken)
        {
            ListUserPropertiesResponse ret = new();
            await _userSettingRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update user settings.
        /// </summary>
        /// <param name="settings">Updated user settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPost("Update")]
        [HttpPost("Add")]
        [Authorize(Policy = GXUserPropertiesPolicies.Edit)]
        public async Task<ActionResult> Post(UpdateUserProperties settings, CancellationToken cancellationToken = default)
        {
            await _userSettingRepository.UpsertAsync(settings.Properties, null, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Delete user settings.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserPropertiesPolicies.Delete)]
        public async Task<ActionResult> Post(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userSettingRepository.DeleteAsync(ids, cancellationToken);
            return Ok();
        }
    }
}
