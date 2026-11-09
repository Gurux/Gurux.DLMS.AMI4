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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the roles.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController(IRoleRepository roleRepository) : ControllerBase
    {
        private readonly IRoleRepository _roleRepository = roleRepository;

        /// <summary>Gets agent token scopes without requiring membership in the Agent role.</summary>
        /// <param name="database">Database used to read current role and user permissions.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>Configured agent scopes limited to the requesting user's grants.</returns>
        [HttpGet("AgentTokenScopes")]
        [Authorize(Policy = GXAgentPolicies.Add)]
        public async Task<ActionResult<string[]>> GetAgentTokenScopes([FromServices] Gurux.DLMS.AMI.Db.IDbConnectionFactory database, CancellationToken cancellationToken = default)
        {
            return await Gurux.DLMS.AMI.Server.Internal.GXAgentTokenScopes.ReadAsync(database, User, cancellationToken);
        }

        /// <summary>
        /// Update role.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateRoleResponse>> Post(UpdateRole request, CancellationToken cancellationToken = default)
        {
            if (request.Roles?.Any() != true)
            {
                return BadRequest("No roles to save.");
            }
            UpdateRoleResponse ret = new()
            {
                Ids = await _roleRepository.UpsertAsync(request.Roles, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Get available roles.
        /// </summary>
        /// <returns>Available roles.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXRolePolicies.View)]
        public async Task<ActionResult<ListRolesResponse>> Post(
            ListRoles? request,
            CancellationToken cancellationToken)
        {
            ListRolesResponse ret = new();
            await _roleRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get role information.
        /// </summary>
        /// <param name="id">Role id.</param>
        /// <returns>Role information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXRolePolicies.View)]
        public async Task<ActionResult<GetRoleResponse>> Get(string id, CancellationToken cancellationToken = default)
        {
            var ret = new GetRoleResponse
            {
                Item = await _roleRepository.ReadAsync(id, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Remove selected role
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXRolePolicies.Delete)]
        public async Task<ActionResult<RoleDeleteResponse>> Post(RoleDelete request, CancellationToken cancellationToken = default)
        {
            await _roleRepository.DeleteAsync(request.Ids, cancellationToken);
            return new RoleDeleteResponse();
        }

        /// <summary>
        /// Regenerates role items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the role items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXRolePolicies.Edit)]
        public async Task<ActionResult<RegenerateRoleResponse>> Post(RegenerateRole request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _roleRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                await _roleRepository.EnsureDefaultsAsync(cancellationToken);
            }

            return new RegenerateRoleResponse();
        }
    }
}
