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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle users.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IGXHost host;
        private readonly CancellationToken _cancellationToken;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserController(IGXHost value, UserManager<ApplicationUser> userManager,
            IHostApplicationLifetime applicationLifetime,
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            host = value;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }
        /// <summary>
        /// Get current user information.
        /// </summary>
        /// <returns>Current user information.</returns>
        [HttpGet]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<GetUserResponse>> Get()
        {
            return new GetUserResponse()
            {
                Item = await _userRepository.ReadAsync(User, null)
            };
        }

        /// <summary>
        /// Get user information.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <returns>User information.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<GetUserResponse>> Get(string id)
        {
            return new GetUserResponse()
            {
                Item = await _userRepository.ReadAsync(User, id)
            };
        }
        /// <summary>
        /// Add new user.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXUserPolicies.Add)]
        public async Task<ActionResult<AddUserResponse>> Post(AddUser request)
        {
            if (request.Users.Length == 0)
            {
                return BadRequest("No users to add.");
            }
            await _userRepository.UpdateAsync(User, request.Users);
            return new AddUserResponse() { Users = request.Users };
        }

        /// <summary>
        /// Get user profile picture.
        /// </summary>
        [HttpGet("Picture")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<string>> Post(string userId)
        {
            return (await _userManager.GetUserAsync(User)).ProfilePicture;
        }

        /// <summary>
        /// List users
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<ListUsersResponse>> Post(
            ListUsers request,
            CancellationToken cancellationToken)
        {
            ListUsersResponse ret = new ListUsersResponse();
            await _userRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXUserPolicies.Delete)]
        public async Task<ActionResult<RemoveUserResponse>> Post(RemoveUser request)
        {
            await _userRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveUserResponse();
        }
    }
}
