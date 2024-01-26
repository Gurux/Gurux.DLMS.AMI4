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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the roles.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        /// <summary>
        /// Update role.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXRolePolicies.Add)]
        public async Task<ActionResult<UpdateRoleResponse>> Post(UpdateRole request)
        {
            if (request.Roles == null || request.Roles.Length == 0)
            {
                return BadRequest("Invalid roles");
            }
            UpdateRoleResponse ret = new UpdateRoleResponse();
            ret.Ids = await _roleRepository.AddAsync(User, request.Roles);
            return ret;
        }

        /// <summary>
        /// Get available roles.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXRolePolicies.View)]
        public async Task<ActionResult<ListRolesResponse>> Post(
            ListRoles request, 
            CancellationToken cancellationToken)
        {
            ListRolesResponse ret = new ListRolesResponse();
            await _roleRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get role information.
        /// </summary>
        /// <param name="id">Role id.</param>
        /// <returns>Role information.</returns>
        [HttpGet]
        [Authorize(Policy = GXRolePolicies.View)]
        public async Task<ActionResult<GXRole>> Get(string id)
        {
            return await _roleRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Remove selected role
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXRolePolicies.Delete)]
        public async Task<ActionResult<RoleDeleteResponse>> Post(RoleDelete request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _roleRepository.DeleteAsync(User, request.Ids);
            return new RoleDeleteResponse();
        }
    }
}
