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

using System.Security.Claims;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Services;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle users.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly GXAccountRegistrationPolicy _registrationPolicy;

        /// <summary>Creates the user API controller.</summary>
        public UserController(IHostApplicationLifetime applicationLifetime,
            IUserRepository userRepository,
            GXAccountRegistrationPolicy registrationPolicy)
        {
            _ = applicationLifetime;
            _userRepository = userRepository;
            _registrationPolicy = registrationPolicy;
        }

        /// <summary>
        /// Get current user information.
        /// </summary>
        /// <returns>Current user information.</returns>
        [HttpGet]
        public async Task<ActionResult<GetUserResponse>> Get(CancellationToken cancellationToken = default)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                //Return empty user information when anonymous user.
                return new GetUserResponse();
            }
            return new GetUserResponse()
            {
                Item = await _userRepository.ReadAsync(null, null, cancellationToken)
            };
        }

        /// <summary>
        /// Get user information.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <returns>User information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("{id}")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<GetUserResponse>> Get(string id, CancellationToken cancellationToken = default)
        {
            return new GetUserResponse()
            {
                Item = await _userRepository.ReadAsync(id, null, cancellationToken)
            };
        }
        /// <summary>
        /// Add new user.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXUserPolicies.Add)]
        public async Task<ActionResult<AddUserResponse>> Post(AddUser request, CancellationToken cancellationToken = default)
        {
            if (request.Users?.Any() != true)
            {
                return BadRequest("No users to add.");
            }
            if (request.Users.Any(user => string.IsNullOrEmpty(user.Id)) && !(await _registrationPolicy.ReadAsync(cancellationToken)).CanCreate(User))
            {
                return Forbid();
            }
            await _userRepository.UpsertAsync(request.Users, null, cancellationToken);
            return new AddUserResponse() { Users = request.Users };
        }

        /// <summary>Get the signed-in user's profile picture without user-management permissions.</summary>
        [HttpGet("Picture/Current")]
        [Authorize]
        public async Task<ActionResult<Guid?>> GetCurrentPictureAsync(
            [FromServices] IDbConnectionFactory dbFactory, CancellationToken cancellationToken = default)
        {
            var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(owner))
            {
                return Unauthorized();
            }
            await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
            var query = GXSelectArgs.Select<GXUser>(item => new { item.Id, item.ProfilePicture },
                item => item.Id == owner && item.Removed == null);
            query.Joins.AddLeftJoin<GXUser, GXAppearance>(item => item.ProfilePicture!, image => image.Id);
            query.Columns.Add<GXAppearance>(image => image.Id);
            var profile = await connection.SingleOrDefaultAsync<GXUser>(query, cancellationToken);
            // Explicit JSON null lets the client use its default avatar without a deserialization error.
            return new JsonResult(profile?.ProfilePicture?.Id);
        }

        /// <summary>
        /// Get the profile picture appearance identifier of the requested accessible user.
        /// </summary>
        [HttpGet("Picture")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<Guid?>> Post(string userId, CancellationToken cancellationToken = default)
        {
            return (await _userRepository.ReadAsync(userId, null, cancellationToken)).ProfilePicture?.Id;
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
            ListUsersResponse ret = new();
            await _userRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Returns identifiers of non-removed users assigned to any requested accessible role.</summary>
        /// <param name="roles">Role names to look up. Administrators can query any role; other callers can query their own roles.</param>
        /// <returns>Distinct user identifiers, or a bad request when no valid role names are supplied.</returns>
        [HttpPost("RoleUsers")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<List<string>>> GetUserIdsInRoleAsync([FromBody] string[] roles)
        {
            if (roles == null || roles.Length == 0 || roles.Any(string.IsNullOrWhiteSpace))
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return await _userRepository.GetUserIdsInRoleAsync(roles);
        }

        /// <summary>Handles the remove user request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXUserPolicies.Delete)]
        public async Task<ActionResult<RemoveUserResponse>> Post(RemoveUser request, CancellationToken cancellationToken = default)
        {
            await _userRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveUserResponse();
        }
    }
}
