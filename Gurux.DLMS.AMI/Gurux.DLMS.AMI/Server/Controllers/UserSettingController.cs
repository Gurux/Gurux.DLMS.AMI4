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
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the user settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserSettingController : ControllerBase
    {
        private readonly IUserSettingRepository _userSettingRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserSettingController(IUserSettingRepository userSettingRepository)
        {
            _userSettingRepository = userSettingRepository;
        }

        /// <summary>
        /// Get user settings.
        /// </summary>
        /// <param name="name">User settings name.</param>
        /// <returns>User settings.</returns>
        [HttpGet]
        [Authorize(Policy = GXUserSettingPolicies.View)]
        public async Task<ActionResult<GetUserSettings>> Get(string name)
        {
            return new GetUserSettings()
            {
                Item = await _userSettingRepository.ReadAsync(User, name)
            };
        }

        /// <summary>
        /// Get user settings.
        /// </summary>
        /// <param name="request">List user settings parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of user settings.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXUserSettingPolicies.View)]
        public async Task<ActionResult<ListUserSettingsResponse>> Post(
            ListUserSettings request,
            CancellationToken cancellationToken)
        {
            ListUserSettingsResponse ret = new ListUserSettingsResponse();
            await _userSettingRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update user settings.
        /// </summary>
        /// <param name="settings">Updated user settings.</param>
        [HttpPost("Update")]
        [HttpPost("Add")]
        [Authorize(Policy = GXUserSettingPolicies.Edit)]
        public async Task<ActionResult> Post(IEnumerable<GXUserSetting> settings)
        {
            await _userSettingRepository.UpdateAsync(User, settings);
            return Ok();
        }

        /// <summary>
        /// Delete user settings.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserSettingPolicies.Delete)]
        public async Task<ActionResult> Post(IEnumerable<string> settings)
        {
            if (settings == null || !settings.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _userSettingRepository.DeleteAsync(User, settings);
            return Ok();
        }
    }
}
