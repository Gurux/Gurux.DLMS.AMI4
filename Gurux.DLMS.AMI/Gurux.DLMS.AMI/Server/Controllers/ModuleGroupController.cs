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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the module groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleGroupController : ControllerBase
    {
        private readonly IModuleGroupRepository _ModuleGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleGroupController(
            IModuleGroupRepository ModuleGroupRepository)
        {
            _ModuleGroupRepository = ModuleGroupRepository;
        }

        /// <summary>
        /// Get module group information.
        /// </summary>
        /// <param name="id">Module group id.</param>
        /// <returns>Module group.</returns>
        [HttpGet]
        [Authorize(Policy = GXModuleGroupPolicies.View)]
        public async Task<ActionResult<GetModuleGroupResponse>> Get(Guid id)
        {
            return new GetModuleGroupResponse()
            {
                Item = await _ModuleGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update module group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXModuleGroupPolicies.Edit)]
        public async Task<ActionResult<AddModuleGroupResponse>> Post(AddModuleGroup request)
        {
            await _ModuleGroupRepository.UpdateAsync(User, request.ModuleGroups);
            return new AddModuleGroupResponse()
            {
                Ids = request.ModuleGroups.Select(s => s.Id).ToArray()
            };
        }

        /// <summary>
        /// List module groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXModuleGroupPolicies.View)]
        public async Task<ActionResult<ListModuleGroupsResponse>> Post(
            ListModuleGroups request,
            CancellationToken cancellationToken)
        {
            ListModuleGroupsResponse ret = new ListModuleGroupsResponse();
            await _ModuleGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXModuleGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveModuleGroupResponse>> Post(RemoveModuleGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ModuleGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveModuleGroupResponse();
        }
    }
}
