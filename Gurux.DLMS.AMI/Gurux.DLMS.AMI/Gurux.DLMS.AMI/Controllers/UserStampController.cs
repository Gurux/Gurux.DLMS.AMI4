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

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the shat user has seen.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserStampController(IUserStampRepository userStampRepository, IAuthorizationService authorization) : ControllerBase
    {
        private readonly IUserStampRepository _userStampRepository = userStampRepository;

        /// <summary>
        /// Get user stamps.
        /// </summary>
        /// <param name="request">List user stamps parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of user stamps.</returns>
        [HttpPost("List")]
        public async Task<ActionResult<ListUserStampsResponse>> Post(
            ListUserStamps request,
            CancellationToken cancellationToken)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                //Return empty list when anonymous user.
                return new ListUserStampsResponse();
            }
            await authorization.EnsureAnyPolicyAsync(User, GXUserStampPolicies.View);
            ListUserStampsResponse ret = new();
            await _userStampRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update user stamps.
        /// </summary>
        /// <param name="request">Updated user stamps.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Update")]
        [Authorize(Policy = GXUserPolicies.Edit)]
        public async Task<ActionResult<UpdateUserStampResponse>> Post(UpdateUserStamp request, CancellationToken cancellationToken = default)
        {
            if (request.Stamps == null || !request.Stamps.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userStampRepository.UpsertAsync(request.Stamps, cancellationToken);
            return new UpdateUserStampResponse()
            {
                Stamps = request.Stamps
            };
        }

        /// <summary>
        /// Delete user stamps.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserPolicies.Delete)]
        public async Task<ActionResult> Post(IEnumerable<Guid> stamps, CancellationToken cancellationToken = default)
        {
            if (stamps == null || !stamps.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userStampRepository.DeleteAsync(stamps, cancellationToken);
            return Ok();
        }
    }
}
