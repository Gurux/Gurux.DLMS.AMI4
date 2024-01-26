//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Client.Pages.Config;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the shat user has seen.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserStampController : ControllerBase
    {
        private readonly IUserStampRepository _userSettingRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserStampController(IUserStampRepository userSettingRepository)
        {
            _userSettingRepository = userSettingRepository;
        }

        /// <summary>
        /// Get user stamps.
        /// </summary>
        /// <param name="request">List user stamps parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of user stamps.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<ListUserStampsResponse>> Post(
            ListUserStamps request,
            CancellationToken cancellationToken)
        {
            ListUserStampsResponse ret = new ListUserStampsResponse();
            await _userSettingRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update user stamps.
        /// </summary>
        /// <param name="request">Updated user stamps.</param>
        [HttpPost("Update")]
        [Authorize(Policy = GXUserPolicies.Edit)]
        public async Task<ActionResult<UpdateUserStampResponse>> Post(UpdateUserStamp request)
        {
            if (request.Stamps == null || !request.Stamps.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userSettingRepository.UpdateAsync(User, request.Stamps);
            return new UpdateUserStampResponse();
        }

        /// <summary>
        /// Delete user stamps.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserPolicies.Delete)]
        public async Task<ActionResult> Post(IEnumerable<Guid> stamps)
        {
            if (stamps == null || !stamps.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userSettingRepository.DeleteAsync(User, stamps);
            return Ok();
        }
    }
}
